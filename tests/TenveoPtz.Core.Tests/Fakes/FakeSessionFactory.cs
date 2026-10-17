using System;
using TenveoPtz.Core.Session;
using TenveoPtz.Core.Settings;

namespace TenveoPtz.Core.Tests.Fakes;

internal sealed class FakeSessionFactory : ICameraSessionFactory
{
    /// <summary>Controller of the opened sessions; a <see cref="RawRecordingPtzController"/> acts like VISCA.</summary>
    public RecordingPtzController Ptz { get; set; } = new();

    public Exception? FailWith { get; set; }

    public ConnectionOptions? LastOptions { get; private set; }

    public int OpenCount { get; private set; }

    public CameraSession Open(ConnectionOptions options)
    {
        LastOptions = options;
        OpenCount++;
        if (FailWith is not null)
        {
            throw FailWith;
        }

        return new CameraSession(null, Ptz);
    }
}
