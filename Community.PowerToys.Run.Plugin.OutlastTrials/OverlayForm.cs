using System.Drawing;
using System.Windows.Forms;

namespace Community.PowerToys.Run.Plugin.OutlastTrials;

public class OverlayForm : Form
{
    // Prevent window from being activated (no focus stealing)
    protected override bool ShowWithoutActivation => true;

    protected override CreateParams CreateParams
    {
        get
        {
            CreateParams @params = base.CreateParams;
            @params.ExStyle |= 0x00000020; // WS_EX_TRANSPARENT
            @params.ExStyle |= 0x00080000; // WS_EX_LAYERED
            return @params;
        }
    }

    private readonly OutlinedLabel _label;

    public OverlayForm()
    {
        FormBorderStyle = FormBorderStyle.None;
        TopMost = true;
        TopLevel = true;
        BackColor = Color.Black;
        TransparencyKey = Color.Black; // Makes black fully transparent (acts as cutout)
        Opacity = 0.75; // Overall window opacity
        ShowInTaskbar = false;

        Size = new Size(300, 150);
        Location = new Point(20, 20);

        _label = new OutlinedLabel
        {
            Text = "Hello World",
            ForeColor = Color.LimeGreen,
            BackColor = Color.Transparent,
            Font = new Font("Consolas", 24, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(10, 10),
            OutlineColor = Color.White,
            OutlineWidth = 4f,
        };

        Controls.Add(_label);

        Shown += (_, _) =>
        {
            var myScreen = Screen.FromControl(this);
            var area = myScreen.WorkingArea;

            var middleOfScreen = new Point(area.Left + area.Width / 2, area.Top + area.Height / 2);
            Location = new Point(
                middleOfScreen.X + 60, // +60 for putting the text to the right
                middleOfScreen.Y - _label.Height / 2 + 8 // +8 is a manual adjustment
            );

            ForceTopLevel();
        };
    }

    public void ForceTopLevel()
    {
        Native.SetWindowPos(Handle, Native.HWND_TOPMOST, 100, 100, 300, 300, Native.TOPMOST_FLAGS);
    }

    public void SetText(string text, Color? color = null)
    {
        _label.Text = text;

        if (color.HasValue)
            _label.ForeColor = color.Value;
    }
}
