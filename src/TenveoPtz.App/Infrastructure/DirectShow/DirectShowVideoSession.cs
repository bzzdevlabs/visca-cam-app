using System;
using System.Windows.Forms;
using TenveoPtz.App.Infrastructure.DirectShow.Interop;
using TenveoPtz.Core.Ptz;
using TenveoPtz.Core.Video;

namespace TenveoPtz.App.Infrastructure.DirectShow;

/// <summary>
/// Live preview built as a DirectShow graph (capture filter, then default video renderer) whose
/// renderer window is parented into a WinForms control. Rendering is done by Windows itself,
/// so the preview costs almost no CPU.
/// </summary>
internal sealed class DirectShowVideoSession : IVideoSession
{
    private const int WsChild = 0x40000000;
    private const int WsClipSiblings = 0x04000000;
    private const int WsClipChildren = 0x02000000;
    private const int OaTrue = -1;
    private const int OaFalse = 0;

    private readonly Control host;
    private IBaseFilter? source;
    private IGraphBuilder? graph;
    private ICaptureGraphBuilder2? builder;
    private IMediaControl? mediaControl;
    private IVideoWindow? videoWindow;

    public DirectShowVideoSession(VideoDevice device, Control host)
    {
        Device = device ?? throw new ArgumentNullException(nameof(device));
        this.host = host ?? throw new ArgumentNullException(nameof(host));
        try
        {
            BuildGraph();
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    public VideoDevice Device { get; }

    public ICameraAxes? Axes { get; private set; }

    public void Dispose()
    {
        host.Resize -= OnHostResize;
        mediaControl?.Stop();
        if (videoWindow is not null)
        {
            videoWindow.put_Visible(OaFalse);
            videoWindow.put_MessageDrain(IntPtr.Zero);
            videoWindow.put_Owner(IntPtr.Zero);
        }

        // The graph, media control and video window are the same COM object.
        mediaControl = null;
        videoWindow = null;
        Axes = null;
        ComHelper.Release(builder);
        ComHelper.Release(graph);
        ComHelper.Release(source);
        builder = null;
        graph = null;
        source = null;
        host.Invalidate();
    }

    private void BuildGraph()
    {
        source = DeviceEnumerator.CreateFilter(Device.Index);
        if (source is IAMCameraControl cameraControl)
        {
            Axes = new DirectShowCameraAxes(cameraControl);
        }

        graph = ComHelper.CreateInstance<IGraphBuilder>(DsGuids.FilterGraph);
        builder = ComHelper.CreateInstance<ICaptureGraphBuilder2>(DsGuids.CaptureGraphBuilder2);
        ComHelper.ThrowOnFailure(builder.SetFiltergraph(graph), "SetFiltergraph");
        ComHelper.ThrowOnFailure(graph.AddFilter(source, "Camera"), "AddFilter");
        RenderPreview();

        videoWindow = (IVideoWindow)graph;
        videoWindow.put_Owner(host.Handle);
        videoWindow.put_WindowStyle(WsChild | WsClipSiblings | WsClipChildren);
        videoWindow.put_MessageDrain(host.Handle);
        FitToHost();
        videoWindow.put_Visible(OaTrue);
        host.Resize += OnHostResize;

        mediaControl = (IMediaControl)graph;
        ComHelper.ThrowOnFailure(mediaControl.Run(), "Run");
    }

    private void RenderPreview()
    {
        var mediaType = DsGuids.MediaTypeVideo;
        var preview = DsGuids.PinCategoryPreview;
        if (builder!.RenderStream(ref preview, ref mediaType, source!, null, null) >= 0)
        {
            return;
        }

        // Some cameras have no preview pin: render the capture pin instead.
        var capture = DsGuids.PinCategoryCapture;
        ComHelper.ThrowOnFailure(builder.RenderStream(ref capture, ref mediaType, source!, null, null), "RenderStream");
    }

    private void OnHostResize(object? sender, EventArgs e) => FitToHost();

    /// <summary>Letterboxes the video into the host control with a 16:9 aspect ratio.</summary>
    private void FitToHost()
    {
        var area = host.ClientSize;
        if (videoWindow is null || area.Width <= 0 || area.Height <= 0)
        {
            return;
        }

        var width = area.Width;
        var height = width * 9 / 16;
        if (height > area.Height)
        {
            height = area.Height;
            width = height * 16 / 9;
        }

        videoWindow.SetWindowPosition((area.Width - width) / 2, (area.Height - height) / 2, width, height);
    }
}
