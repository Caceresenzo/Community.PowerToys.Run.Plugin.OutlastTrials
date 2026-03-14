using System;
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

    private readonly TimeSpan _playTime;
    private readonly TimeSpan _hideTime;

    private int _remainingSeconds;
    private ShockPhase _shockPhase;

    public OverlayManager(TimeSpan playTime, TimeSpan hideTime)
    {
        _playTime = playTime;
        _hideTime = hideTime;

        _remainingSeconds = (int)_playTime.TotalSeconds;
        _shockPhase = ShockPhase.Playing;

        _countDownThread = new Thread(() =>
        {
            int resolution = 4;
            int waitTime = 1000 / resolution;

            while (!_disposed)
            {
                lock (_lock)
                {
                    bool signaled = false;

                    for (int loop = 0; loop < resolution; loop++)
                    {
                        signaled = Monitor.Wait(_lock, waitTime);
                        if (signaled)
                            break;

                        if (_disposed)
                            return;
                    }

                    if (signaled)
                        continue;
                }

                _remainingSeconds--;

                if (_remainingSeconds <= 0)
                {
                    _shockPhase =
                        _shockPhase == ShockPhase.Playing ? ShockPhase.Hiding : ShockPhase.Playing;

                    _remainingSeconds = (int)(
                        _shockPhase == ShockPhase.Playing
                            ? _playTime.TotalSeconds
                            : _hideTime.TotalSeconds
                    );
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
            _remainingSeconds = (int)_playTime.TotalSeconds;
        else
            _remainingSeconds = (int)_hideTime.TotalSeconds;

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
