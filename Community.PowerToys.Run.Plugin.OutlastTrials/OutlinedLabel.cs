using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Community.PowerToys.Run.Plugin.OutlastTrials;

public class OutlinedLabel : Label
{
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public Color OutlineColor { get; set; } = Color.Black;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public float OutlineWidth { get; set; } = 2f;

    protected override void OnPaint(PaintEventArgs @event)
    {
        @event.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        @event.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

        using var path = new GraphicsPath();
        using var outlinePen = new Pen(OutlineColor, OutlineWidth * 2)
        {
            LineJoin = LineJoin.Round, // prevents spiky corners on sharp letters
        };
        using var fillBrush = new SolidBrush(ForeColor);
        using var format = new StringFormat();

        path.AddString(
            Text,
            Font.FontFamily,
            (int)Font.Style,
            @event.Graphics.DpiY * Font.Size / 72f, // convert points to pixels
            new Point(0, 0),
            format
        );

        // Draw outline first, then fill on top
        @event.Graphics.DrawPath(outlinePen, path);
        @event.Graphics.FillPath(fillBrush, path);
    }
}
