using System.Collections.Generic;
using TenveoPtz.Core.Diagnostics;
using TenveoPtz.Core.Ptz;

namespace TenveoPtz.Core.Tests.Fakes;

/// <summary>Recording controller that also takes raw commands, like the VISCA one.</summary>
internal sealed class RawRecordingPtzController : RecordingPtzController, IRawCommandSender
{
    public void SendRaw(IReadOnlyList<byte> bytes) => Record("Raw " + HexBytes.Format(bytes));
}
