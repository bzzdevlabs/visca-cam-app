using TenveoPtz.Core.Session;
using TenveoPtz.Core.Settings;
using TenveoPtz.Core.Tests.Fakes;

namespace TenveoPtz.Core.Tests.Session;

public sealed class CameraSessionFactoryTests
{
    private readonly FakeDevices devices = new();
    private readonly CameraSessionFactory factory;

    public CameraSessionFactoryTests()
    {
        factory = new CameraSessionFactory(devices, devices, devices);
    }

    [Fact]
    public void Open_StartsPreviewByDefault()
    {
        using var session = factory.Open(new ConnectionOptions { VideoDeviceName = "Tenveo USB Camera" });

        Assert.Equal(new[] { true }, devices.Started);
    }

    [Fact]
    public void Open_UvcWithoutPreviewOpensDeviceForControlsOnly()
    {
        using var session = factory.Open(new ConnectionOptions { VideoDeviceName = "Tenveo USB Camera", ShowPreview = false });

        Assert.Equal(new[] { false }, devices.Started);
        Assert.Equal("UVC (USB)", session.Ptz.Description);
    }

    [Fact]
    public void Open_ViscaWithoutPreviewLeavesVideoDeviceAlone()
    {
        using var session = factory.Open(new ConnectionOptions
        {
            VideoDeviceName = "Tenveo USB Camera",
            Mode = ControlMode.Visca,
            PortName = "COM3",
            ShowPreview = false,
        });

        Assert.Empty(devices.Started);
        Assert.Null(session.Video);
    }
}
