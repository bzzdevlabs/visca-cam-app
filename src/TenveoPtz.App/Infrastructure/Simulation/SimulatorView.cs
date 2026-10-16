using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using TenveoPtz.Core.Ptz;
using TenveoPtz.Core.Simulation;

namespace TenveoPtz.App.Infrastructure.Simulation;

/// <summary>
/// Live picture of the <see cref="SimulatedCamera"/>: a panorama with a degree grid and lettered
/// landmarks, seen through the camera's pan, tilt and zoom, redrawn as the motors move.
/// </summary>
internal sealed class SimulatorView : Control
{
    /// <summary>Horizontal field of view at the widest zoom, in degrees.</summary>
    private const float WideFieldOfView = 64F;

    /// <summary>Magnification at the longest zoom.</summary>
    private const float MaxMagnification = 10F;

    private static readonly (string Label, float Azimuth, float Elevation, Color Color)[] Landmarks =
    {
        ("A", -60, 5, Color.FromArgb(0xE5, 0x39, 0x35)),
        ("B", -25, 15, Color.FromArgb(0xFB, 0x8C, 0x00)),
        ("C", 0, 0, Color.FromArgb(0x43, 0xA0, 0x47)),
        ("D", 25, -12, Color.FromArgb(0x1E, 0x88, 0xE5)),
        ("E", 60, 10, Color.FromArgb(0x8E, 0x24, 0xAA)),
        ("F", 100, 0, Color.FromArgb(0x00, 0x89, 0x7B)),
        ("G", -100, -5, Color.FromArgb(0x6D, 0x4C, 0x41)),
        ("UP", 0, 45, Color.FromArgb(0x54, 0x6E, 0x7A)),
        ("DOWN", 0, -45, Color.FromArgb(0x54, 0x6E, 0x7A)),
    };

    private readonly SimulatedCamera camera;

    public SimulatorView(SimulatedCamera camera)
    {
        this.camera = camera ?? throw new ArgumentNullException(nameof(camera));
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
        BackColor = Color.Black;
        camera.Moved += OnMoved;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            camera.Moved -= OnMoved;
        }

        base.Dispose(disposing);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var frame = Letterbox(ClientRectangle);
        if (frame.Width <= 0 || frame.Height <= 0)
        {
            return;
        }

        var graphics = e.Graphics;
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.SetClip(frame);

        // Tenveo convention: the view turns right as pan decreases and looks up as tilt decreases.
        var azimuth = (float)-camera.PositionOf(CameraAxis.Pan);
        var elevation = (float)-camera.PositionOf(CameraAxis.Tilt);
        var magnification = 1 + ((MaxMagnification - 1) * (float)camera.PositionOf(CameraAxis.Zoom) / 1000F);
        var fieldWidth = WideFieldOfView / magnification;
        var fieldHeight = fieldWidth * frame.Height / frame.Width;
        PointF ToScreen(float az, float el) => new(
            frame.X + (frame.Width / 2F) + ((az - azimuth) / fieldWidth * frame.Width),
            frame.Y + (frame.Height / 2F) - ((el - elevation) / fieldHeight * frame.Height));

        var horizon = ToScreen(0, 0).Y;
        using (var sky = new SolidBrush(Color.FromArgb(0x9C, 0xC9, 0xEE)))
        using (var ground = new SolidBrush(Color.FromArgb(0x7C, 0x9A, 0x6A)))
        {
            graphics.FillRectangle(sky, frame.X, frame.Y, frame.Width, Math.Max(0, horizon - frame.Y));
            graphics.FillRectangle(ground, frame.X, horizon, frame.Width, Math.Max(0, frame.Bottom - horizon));
        }

        DrawGrid(graphics, frame, azimuth, elevation, fieldWidth, fieldHeight, ToScreen);
        var degree = frame.Width / fieldWidth;
        using (var font = new Font(FontFamily.GenericSansSerif, Math.Max(6F, 3F * degree), FontStyle.Bold, GraphicsUnit.Pixel))
        {
            foreach (var (label, az, el, color) in Landmarks)
            {
                var center = ToScreen(az, el);
                var size = Math.Max(label.Length, 2) * 2.5F * degree;
                var board = new RectangleF(center.X - (size / 2), center.Y - (2.5F * degree), size, 5F * degree);
                using var brush = new SolidBrush(color);
                graphics.FillRectangle(brush, board);
                TextRenderer.DrawText(graphics, label, font, Rectangle.Round(board), Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }
        }

        DrawOverlay(graphics, frame);
        graphics.ResetClip();
    }

    /// <summary>The 16:9 picture centred in <paramref name="area"/>, like a real camera's preview.</summary>
    private static Rectangle Letterbox(Rectangle area)
    {
        var width = area.Width;
        var height = width * 9 / 16;
        if (height > area.Height)
        {
            height = area.Height;
            width = height * 16 / 9;
        }

        return new Rectangle(area.X + ((area.Width - width) / 2), area.Y + ((area.Height - height) / 2), width, height);
    }

    private static void DrawGrid(Graphics graphics, Rectangle frame, float azimuth, float elevation, float fieldWidth, float fieldHeight, Func<float, float, PointF> toScreen)
    {
        using var line = new Pen(Color.FromArgb(70, Color.White));
        using var font = new Font(FontFamily.GenericSansSerif, 11F, GraphicsUnit.Pixel);
        var step = fieldWidth > 30 ? 10 : fieldWidth > 10 ? 5 : 1;
        for (var az = (int)Math.Floor((azimuth - fieldWidth) / step) * step; az <= azimuth + fieldWidth; az += step)
        {
            var x = toScreen(az, 0).X;
            graphics.DrawLine(line, x, frame.Top, x, frame.Bottom);
            TextRenderer.DrawText(graphics, $"{az}°", font, new Point((int)x + 3, (int)toScreen(0, 0).Y + 3), Color.White);
        }

        for (var el = (int)Math.Floor((elevation - fieldHeight) / step) * step; el <= elevation + fieldHeight; el += step)
        {
            var y = toScreen(0, el).Y;
            graphics.DrawLine(line, frame.Left, y, frame.Right, y);
        }
    }

    private void DrawOverlay(Graphics graphics, Rectangle frame)
    {
        var middle = new PointF(frame.X + (frame.Width / 2F), frame.Y + (frame.Height / 2F));
        using (var cross = new Pen(Color.FromArgb(200, Color.White), 2F))
        {
            graphics.DrawLine(cross, middle.X - 14, middle.Y, middle.X + 14, middle.Y);
            graphics.DrawLine(cross, middle.X, middle.Y - 14, middle.X, middle.Y + 14);
        }

        var status = $"PTZ SIMULATOR   pan {camera.GetValue(CameraAxis.Pan)}   tilt {camera.GetValue(CameraAxis.Tilt)}   zoom {camera.GetValue(CameraAxis.Zoom)}{(camera.IsMoving ? "   moving" : string.Empty)}";
        using var font = new Font(FontFamily.GenericSansSerif, 12F, FontStyle.Bold, GraphicsUnit.Pixel);
        var size = TextRenderer.MeasureText(status, font);
        using var shade = new SolidBrush(Color.FromArgb(150, Color.Black));
        graphics.FillRectangle(shade, frame.X + 8, frame.Y + 8, size.Width + 12, size.Height + 6);
        TextRenderer.DrawText(graphics, status, font, new Point(frame.X + 14, frame.Y + 11), Color.White);
    }

    private void OnMoved(object? sender, EventArgs e) => Invalidate();
}
