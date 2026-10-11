using AppKit;
using CoreAnimation;
using CoreGraphics;

namespace TenveoPtz.Mac.Views.Controls;

/// <summary>Black video area hosting the camera preview layer, with a hint while nothing is shown.</summary>
internal sealed class VideoView : NSView
{
    private readonly NSStackView placeholder;
    private CALayer? preview;

    public VideoView()
    {
        WantsLayer = true;
        Layer!.BackgroundColor = new CGColor(0, 0, 0);
        Layer.CornerRadius = 8;
        Layer.MasksToBounds = true;

        var title = NSTextField.CreateLabel("No video");
        title.Font = NSFont.SystemFontOfSize(17, NSFontWeight.Semibold)!;
        title.TextColor = NSColor.White;
        var text = NSTextField.CreateLabel("Choose the camera above, then press Connect.");
        text.TextColor = NSColor.FromWhite(1, 0.7f);
        placeholder = NSStackView.FromViews(new NSView[] { title, text });
        placeholder.Orientation = NSUserInterfaceLayoutOrientation.Vertical;
        placeholder.Spacing = 6;
        placeholder.TranslatesAutoresizingMaskIntoConstraints = false;
        AddSubview(placeholder);
        placeholder.CenterXAnchor.ConstraintEqualTo(CenterXAnchor).Active = true;
        placeholder.CenterYAnchor.ConstraintEqualTo(CenterYAnchor).Active = true;
    }

    public bool ShowPlaceholder
    {
        get => !placeholder.Hidden;
        set => placeholder.Hidden = !value;
    }

    /// <summary>Shows <paramref name="layer"/> as the video, or removes the current one when <c>null</c>.</summary>
    public void ShowPreview(CALayer? layer)
    {
        preview?.RemoveFromSuperLayer();
        preview = layer;
        if (layer is not null)
        {
            layer.Frame = Bounds;
            Layer!.InsertSublayer(layer, 0);
        }
    }

    public override void Layout()
    {
        base.Layout();
        if (preview is not null)
        {
            // No implicit animation: the picture follows the window edge while resizing.
            CATransaction.Begin();
            CATransaction.DisableActions = true;
            preview.Frame = Bounds;
            CATransaction.Commit();
        }
    }
}
