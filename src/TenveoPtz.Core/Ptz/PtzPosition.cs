namespace TenveoPtz.Core.Ptz;

/// <summary>Absolute camera position, in the units reported by the camera driver.</summary>
public sealed class PtzPosition
{
    public PtzPosition()
    {
    }

    public PtzPosition(int? pan, int? tilt, int? zoom)
    {
        Pan = pan;
        Tilt = tilt;
        Zoom = zoom;
    }

    public int? Pan { get; set; }

    public int? Tilt { get; set; }

    public int? Zoom { get; set; }
}
