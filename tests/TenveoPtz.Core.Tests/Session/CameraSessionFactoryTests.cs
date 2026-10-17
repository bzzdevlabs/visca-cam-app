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

    [Fact]
    public void Open_RequiresOptions()
    {
        Assert.Throws<System.ArgumentNullException>(() => factory.Open(null!));
    }

    [Fact]
    public void Open_UvcWithoutACameraExplainsWhatIsMissing()
    {
        var error = Assert.Throws<System.InvalidOperationException>(() => factory.Open(new ConnectionOptions { VideoDeviceName = "" }));

        Assert.Contains("select it in the Camera list", error.Message);
    }

    [Fact]
    public void Open_UnknownCameraSaysItIsNotConnected()
    {
        var error = Assert.Throws<System.InvalidOperationException>(() => factory.Open(new ConnectionOptions { VideoDeviceName = "Other camera" }));

        Assert.Equal("Video device \"Other camera\" is not connected.", error.Message);
    }

    [Fact]
    public void Open_FailureReleasesTheVideoAlreadyStarted()
    {
        Assert.Throws<System.InvalidOperationException>(() => factory.Open(new ConnectionOptions
        {
            VideoDeviceName = "Tenveo USB Camera",
            Mode = ControlMode.Visca,
            PortName = "",
        }));

        Assert.True(devices.LastVideo!.Disposed);
    }

    [Fact]
    public void Open_ViscaFailureReleasesTheSerialPort()
    {
        Assert.Throws<System.ArgumentOutOfRangeException>(() => factory.Open(new ConnectionOptions
        {
            Mode = ControlMode.Visca,
            PortName = "COM3",
            Address = 9,
            ShowPreview = false,
        }));

        Assert.True(devices.LastTransport!.Disposed);
    }
}
