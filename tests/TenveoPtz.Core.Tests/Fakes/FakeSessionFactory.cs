using System;
using TenveoPtz.Core.Session;
using TenveoPtz.Core.Settings;

namespace TenveoPtz.Core.Tests.Fakes;

internal sealed class FakeSessionFactory : ICameraSessionFactory
{
    public RecordingPtzController Ptz { get; } = new();

    public Exception? FailWith { get; set; }

    public ConnectionOptions? LastOptions { get; private set; }

    public CameraSession Open(ConnectionOptions options)
    {
        LastOptions = options;
        if (FailWith is not null)
        {
            throw FailWith;
        }

        return new CameraSession(null, Ptz);
    }
}
