using TenveoPtz.Core.Settings;

namespace TenveoPtz.Core.Session;

public interface ICameraSessionFactory
{
    /// <summary>Opens the video preview and the PTZ control channel described by <paramref name="options"/>.</summary>
    CameraSession Open(ConnectionOptions options);
}
