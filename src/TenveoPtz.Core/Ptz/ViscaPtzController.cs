using System;
using System.IO;
using TenveoPtz.Core.Execution;
using TenveoPtz.Core.Presets;
using TenveoPtz.Core.Visca;

namespace TenveoPtz.Core.Ptz;

/// <summary>
/// PTZ strategy using VISCA over a serial line. Presets live in the camera's own memory,
/// so they survive restarts and are recalled with the camera's built-in motion profile.
/// </summary>
public sealed class ViscaPtzController : IPtzController
{
    private const string PanTiltKey = "pan-tilt";
    private const string ZoomKey = "zoom";
    private const string FocusKey = "focus";

    private readonly ViscaClient client;
    private readonly ICommandExecutor executor;

    /// <param name="client">VISCA client. Ownership is transferred to the controller.</param>
    /// <param name="executor">Executor serialising I/O. Ownership is transferred to the controller.</param>
    public ViscaPtzController(ViscaClient client, ICommandExecutor executor, string description)
    {
        this.client = client ?? throw new ArgumentNullException(nameof(client));
        this.executor = executor ?? throw new ArgumentNullException(nameof(executor));
        Description = description;
    }

    public event EventHandler<ErrorEventArgs>? Faulted
    {
        add => executor.Faulted += value;
        remove => executor.Faulted -= value;
    }

    public string Description { get; }

    public void Move(Motion pan, Motion tilt, double speed)
    {
        var panSpeed = SpeedScale.ToRange(speed, ViscaCommands.MinPanSpeed, ViscaCommands.MaxPanSpeed);
        var tiltSpeed = SpeedScale.ToRange(speed, ViscaCommands.MinTiltSpeed, ViscaCommands.MaxTiltSpeed);
        Send(ViscaCommands.PanTiltDrive(pan, tilt, panSpeed, tiltSpeed), PanTiltKey);
    }

    public void Zoom(Motion direction, double speed) =>
        Send(ViscaCommands.Zoom(direction, SpeedScale.ToRange(speed, ViscaCommands.MinZoomSpeed, ViscaCommands.MaxZoomSpeed)), ZoomKey);

    public void Focus(Motion direction) => Send(ViscaCommands.Focus(direction), FocusKey);

    public void SetAutoFocus(bool enabled) => Send(ViscaCommands.AutoFocus(enabled));

    public void Home() => Send(ViscaCommands.Home(), PanTiltKey);

    public void StorePreset(Preset preset) => Send(ViscaCommands.MemorySet(RequirePreset(preset).Slot));

    public void RecallPreset(Preset preset) => Send(ViscaCommands.MemoryRecall(RequirePreset(preset).Slot), PanTiltKey);

    public void ForgetPreset(Preset preset) => Send(ViscaCommands.MemoryReset(RequirePreset(preset).Slot));

    public void Dispose()
    {
        executor.Dispose();
        client.Dispose();
    }

    private static Preset RequirePreset(Preset preset) => preset ?? throw new ArgumentNullException(nameof(preset));

    private void Send(ViscaCommand command, string? coalesceKey = null) =>
        executor.Post(() => client.Execute(command), coalesceKey);
}
