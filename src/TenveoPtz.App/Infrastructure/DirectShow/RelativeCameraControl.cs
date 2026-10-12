using System;
using System.Runtime.InteropServices;
using TenveoPtz.App.Infrastructure.DirectShow.Interop;
using TenveoPtz.Core.Ptz;

namespace TenveoPtz.App.Infrastructure.DirectShow;

/// <summary>
/// Continuous pan, tilt and zoom through the KSPROPERTY_CAMERACONTROL_*_RELATIVE properties
/// (UVC CT_PANTILT_RELATIVE and CT_ZOOM_RELATIVE controls): the camera's motor runs at the
/// requested speed until told to stop, so moves are smooth instead of one-degree jumps.
/// </summary>
internal sealed class RelativeCameraControl
{
    private const int PanRelative = 10;
    private const int TiltRelative = 11;
    private const int ZoomRelative = 13;

    private const int TypeSet = 0x2;
    private const int TypeBasicSupport = 0x200;
    private const int FlagsManual = 0x2;
    private const int MemberRanges = 1;
    private const int MemberSteppedRanges = 2;

    // KSPROPERTY: GUID Set, ULONG Id, ULONG Flags.
    private const int PropertySize = 24;

    // KSPROPERTY_CAMERACONTROL_S: KSPROPERTY, LONG Value, ULONG Flags, ULONG Capabilities.
    private const int CameraControlSize = PropertySize + 12;

    // KSPROPERTY_DESCRIPTION (40 bytes), KSPROPERTY_MEMBERSHEADER (16 bytes), then the range.
    private const int DescriptionSize = 40;
    private const int MembersHeaderSize = 16;
    private const int BasicSupportSize = 256;

    private static readonly Guid CameraControlSet = new("C6E13370-30AC-11d0-A18C-00A0C9118956");

    private readonly IKsControl control;

    public RelativeCameraControl(IKsControl control)
    {
        this.control = control ?? throw new ArgumentNullException(nameof(control));
    }

    /// <summary>Slowest to fastest speed, or <c>null</c> when the driver does not offer the control.</summary>
    public AxisRange? GetSpeedRange(CameraAxis axis)
    {
        if (!TryGetId(axis, out var id))
        {
            return null;
        }

        var property = Marshal.AllocCoTaskMem(PropertySize);
        var data = Marshal.AllocCoTaskMem(BasicSupportSize);
        try
        {
            WriteProperty(property, id, TypeBasicSupport);
            if (control.KsProperty(property, PropertySize, data, BasicSupportSize, out var returned) != 0
                || returned < DescriptionSize + MembersHeaderSize + 8
                || (Marshal.ReadInt32(data, 0) & TypeSet) == 0
                || Marshal.ReadInt32(data, 32) < 1)
            {
                return null;
            }

            var members = data + DescriptionSize;
            var bounds = Marshal.ReadInt32(members, 0) switch
            {
                MemberRanges => members + MembersHeaderSize,
                MemberSteppedRanges when returned >= DescriptionSize + MembersHeaderSize + 16 => members + MembersHeaderSize + 8,
                _ => IntPtr.Zero,
            };
            if (bounds == IntPtr.Zero)
            {
                return null;
            }

            // Some drivers report -max..max, others the speeds only: keep the magnitudes.
            int min = Marshal.ReadInt32(bounds, 0), max = Marshal.ReadInt32(bounds, 4);
            var fastest = Math.Max(Math.Abs(min), Math.Abs(max));
            var slowest = min > 0 ? min : 1;
            return fastest >= slowest ? new AxisRange(slowest, fastest, 1, slowest) : null;
        }
        catch (Exception ex) when (ex is COMException or InvalidCastException)
        {
            return null;
        }
        finally
        {
            Marshal.FreeCoTaskMem(property);
            Marshal.FreeCoTaskMem(data);
        }
    }

    public void SetSpeed(CameraAxis axis, int speed)
    {
        if (!TryGetId(axis, out var id))
        {
            throw new NotSupportedException($"No continuous {axis.ToString().ToLowerInvariant()} control.");
        }

        var buffer = Marshal.AllocCoTaskMem(CameraControlSize);
        try
        {
            WriteProperty(buffer, id, TypeSet);
            Marshal.WriteInt32(buffer, PropertySize, speed);
            Marshal.WriteInt32(buffer, PropertySize + 4, FlagsManual);
            Marshal.WriteInt32(buffer, PropertySize + 8, 0);
            ComHelper.ThrowOnFailure(control.KsProperty(buffer, CameraControlSize, buffer, CameraControlSize, out _), $"set {axis} speed");
        }
        finally
        {
            Marshal.FreeCoTaskMem(buffer);
        }
    }

    private static bool TryGetId(CameraAxis axis, out int id)
    {
        id = axis switch
        {
            CameraAxis.Pan => PanRelative,
            CameraAxis.Tilt => TiltRelative,
            CameraAxis.Zoom => ZoomRelative,
            _ => -1,
        };
        return id >= 0;
    }

    private static void WriteProperty(IntPtr buffer, int id, int flags)
    {
        Marshal.Copy(CameraControlSet.ToByteArray(), 0, buffer, 16);
        Marshal.WriteInt32(buffer, 16, id);
        Marshal.WriteInt32(buffer, 20, flags);
    }
}
