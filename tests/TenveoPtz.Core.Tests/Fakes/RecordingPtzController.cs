using System;
using System.Collections.Generic;
using System.IO;
using TenveoPtz.Core.Presets;
using TenveoPtz.Core.Ptz;

namespace TenveoPtz.Core.Tests.Fakes;

/// <summary>PTZ strategy recording each call as a readable string.</summary>
internal sealed class RecordingPtzController : IPtzController
{
    public event EventHandler<ErrorEventArgs>? Faulted;

    public string Description => "Recording";

    public List<string> Calls { get; } = new();

    public Exception? FailWith { get; set; }

    public void Move(Motion pan, Motion tilt, double speed) => Record($"Move {pan} {tilt}");

    public void Zoom(Motion direction, double speed) => Record($"Zoom {direction}");

    public void Focus(Motion direction) => Record($"Focus {direction}");

    public void SetAutoFocus(bool enabled) => Record($"AutoFocus {enabled}");

    public void Home() => Record("Home");

    public void StorePreset(Preset preset) => Record($"Store {preset.Slot}");

    public void RecallPreset(Preset preset) => Record($"Recall {preset.Slot}");

    public void ForgetPreset(Preset preset) => Record($"Forget {preset.Slot}");

    public void Dispose() => Calls.Add("Dispose");

    public void RaiseFault(Exception ex) => Faulted?.Invoke(this, new ErrorEventArgs(ex));

    private void Record(string call)
    {
        if (FailWith is not null)
        {
            throw FailWith;
        }

        Calls.Add(call);
    }
}
