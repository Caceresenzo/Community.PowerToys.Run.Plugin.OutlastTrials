using System;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using ManagedCommon;

namespace Community.PowerToys.Run.Plugin.OutlastTrials;

public class OverlayManager : IDisposable
{
    private OverlayForm _form;
    private readonly Thread _countDownThread;
    private readonly Thread _uiThread;
    private bool _disposed = false;

    private readonly object _lock = new();

    private readonly int _playingTime;
    private readonly int _hidingTime;

    private int _remainingSeconds;
    private ShockPhase _shockPhase;

    public OverlayManager(ShockDefinition shock)
        : this(shock.PlayingTime, shock.HidingTime) { }

    public OverlayManager(int playingTime, int hidingTime)
    {
        _playingTime = playingTime;
        _hidingTime = hidingTime;

        _remainingSeconds = _playingTime;
        _shockPhase = ShockPhase.Playing;
        _countDownThread = new Thread(() =>
        {
            var nextTick = Stopwatch.GetTimestamp();
            var ticksPerSecond = Stopwatch.Frequency;

            while (!_disposed)
            {
                nextTick += ticksPerSecond;

                long now = Stopwatch.GetTimestamp();
                long waitTicks = nextTick - now;

                if (waitTicks > 0)
                {
                    int waitMilliseconds = (int)(waitTicks * 1000 / ticksPerSecond);

                    if (waitMilliseconds > 0)
                    {
                        lock (_lock)
                        {
                            if (Monitor.Wait(_lock, waitMilliseconds))
                                continue;
                        }
                    }
                }

                if (_disposed)
                    return;

                _remainingSeconds--;

                if (_remainingSeconds <= 0)
                {
                    _shockPhase =
                        _shockPhase == ShockPhase.Playing ? ShockPhase.Hiding : ShockPhase.Playing;

                    _remainingSeconds =
                        _shockPhase == ShockPhase.Playing ? _playingTime : _hidingTime;
                }

                Update();
            }
        });

        _uiThread = new Thread(() =>
        {
            Application.EnableVisualStyles();

            _form = new OverlayForm();
            _form.Shown += (_, _) => Update();

            Application.Run(_form);
        });

        _uiThread.SetApartmentState(ApartmentState.STA);
        _uiThread.IsBackground = true;

        _countDownThread.SetApartmentState(ApartmentState.STA);
        _countDownThread.IsBackground = true;
    }

    ~OverlayManager()
    {
        Dispose(false);
    }

    public void Show()
    {
        if (_uiThread.IsAlive)
        {
            Logger.LogInfo("Overlay is already running.");
            _form.ForceTopLevel();
            return;
        }

        _uiThread.Start();
        _countDownThread.Start();

        while (_form == null || !_form.IsHandleCreated)
            Thread.Sleep(10);
    }

    public void Update()
    {
        if (_disposed || _form == null)
            return;

        string text;
        Color color;

        if (_shockPhase == ShockPhase.Playing)
        {
            text = $"{_remainingSeconds}s";
            color = Color.LimeGreen;

            if (_remainingSeconds < 10)
                color = _remainingSeconds % 2 == 0 ? Color.OrangeRed : Color.Orange;
        }
        else
        {
            text = $"-{_remainingSeconds}s";
            color = Color.Red;
        }

        try
        {
            if (_form.IsHandleCreated && !_form.IsDisposed)
                _form.Invoke(() => _form.SetText(text, color));
        }
        catch (ObjectDisposedException) { }
        catch (InvalidOperationException) { }
    }

    public void Sync(ShockPhase phase)
    {
        NotifyStop();

        if (phase == ShockPhase.Playing)
            _remainingSeconds = _playingTime;
        else
            _remainingSeconds = _hidingTime;

        _shockPhase = phase;

        Update();
    }

    private void NotifyStop()
    {
        lock (_lock)
        {
            Monitor.PulseAll(_lock);
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;
        _disposed = true;

        NotifyStop();

        if (disposing)
        {
            if (_form != null)
            {
                try
                {
                    if (_form.IsHandleCreated && !_form.IsDisposed)
                    {
                        _form.Invoke(() =>
                        {
                            Application.ExitThread();
                            _form.Dispose();
                        });
                    }
                }
                catch (ObjectDisposedException) { }
                catch (InvalidOperationException) { }
            }

            if (_uiThread.IsAlive)
                _uiThread.Join(timeout: TimeSpan.FromSeconds(3));

            _form = null;
        }
    }

    public enum ShockPhase
    {
        Playing,
        Hiding,
    }
}
