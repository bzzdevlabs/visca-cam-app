namespace TenveoPtz.Core.Video;

/// <summary>A video capture device as listed by the operating system.</summary>
public sealed class VideoDevice
{
    public VideoDevice(int index, string name)
    {
        Index = index;
        Name = name;
    }

    /// <summary>Position in the system device enumeration.</summary>
    public int Index { get; }

    public string Name { get; }

    public override string ToString() => Name;
}
