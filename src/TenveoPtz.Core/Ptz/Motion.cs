namespace TenveoPtz.Core.Ptz;

/// <summary>
/// Direction of travel along a single axis.
/// Pan: <see cref="Negative"/> is left. Tilt: <see cref="Positive"/> is up.
/// Zoom: <see cref="Positive"/> is tele (in). Focus: <see cref="Positive"/> is far.
/// </summary>
public enum Motion
{
    Negative = -1,
    None = 0,
    Positive = 1,
}
