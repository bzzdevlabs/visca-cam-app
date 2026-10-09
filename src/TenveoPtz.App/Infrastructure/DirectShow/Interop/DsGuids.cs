using System;

namespace TenveoPtz.App.Infrastructure.DirectShow.Interop;

/// <summary>DirectShow class, category and media type identifiers (from uuids.h / strmif.h).</summary>
internal static class DsGuids
{
    public static readonly Guid SystemDeviceEnum = new("62BE5D10-60EB-11d0-BD3B-00A0C911CE86");
    public static readonly Guid VideoInputDeviceCategory = new("860BB310-5D01-11d0-BD3B-00A0C911CE86");
    public static readonly Guid FilterGraph = new("E436EBB3-524F-11CE-9F53-0020AF0BA770");
    public static readonly Guid CaptureGraphBuilder2 = new("BF87B6E1-8C27-11d0-B3F0-00AA003761C5");
    public static readonly Guid PinCategoryPreview = new("fb6c4282-0353-11d1-905f-0000c0cc16ba");
    public static readonly Guid PinCategoryCapture = new("fb6c4281-0353-11d1-905f-0000c0cc16ba");
    public static readonly Guid MediaTypeVideo = new("73646976-0000-0010-8000-00AA00389B71");
    public static readonly Guid BaseFilterInterface = typeof(IBaseFilter).GUID;
    public static readonly Guid PropertyBagInterface = typeof(IPropertyBag).GUID;
}
