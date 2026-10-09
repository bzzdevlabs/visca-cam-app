using System;
using System.IO;
using TenveoPtz.Core.Presets;

namespace TenveoPtz.Core.Ptz;

/// <summary>
/// Strategy for driving a PTZ camera. Implementations translate high-level intents
/// (move, zoom, presets) into a concrete control protocol.
/// </summary>
public interface IPtzController : IDisposable
{
    /// <summary>Raised when an asynchronous command fails. May be raised on any thread.</summary>
    event EventHandler<ErrorEventArgs>? Faulted;

    /// <summary>Human readable description of the control channel, e.g. "VISCA on COM3".</summary>
    string Description { get; }

    /// <summary>Starts a continuous pan/tilt movement. <see cref="Motion.None"/> on both axes stops.</summary>
    /// <param name="speed">Relative speed between 0 (slowest) and 1 (fastest).</param>
    void Move(Motion pan, Motion tilt, double speed);

    /// <summary>Starts or stops a continuous zoom.</summary>
    void Zoom(Motion direction, double speed);

    /// <summary>Starts or stops a continuous manual focus adjustment.</summary>
    void Focus(Motion direction);

    void SetAutoFocus(bool enabled);

    /// <summary>Returns the camera to its home (centre) position.</summary>
    void Home();

    /// <summary>Stores the current camera position into <paramref name="preset"/>.</summary>
    void StorePreset(Preset preset);

    /// <summary>Moves the camera to <paramref name="preset"/>.</summary>
    void RecallPreset(Preset preset);

    /// <summary>Releases any camera-side resources held by <paramref name="preset"/>.</summary>
    void ForgetPreset(Preset preset);
}
