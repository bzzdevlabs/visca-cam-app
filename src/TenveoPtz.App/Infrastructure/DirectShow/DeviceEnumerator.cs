using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.ComTypes;
using TenveoPtz.App.Infrastructure.DirectShow.Interop;

namespace TenveoPtz.App.Infrastructure.DirectShow;

/// <summary>Enumerates DirectShow video capture devices and binds them to capture filters.</summary>
internal static class DeviceEnumerator
{
    private const string UnknownDeviceName = "Unknown camera";

    /// <summary>Lists the friendly names of all video capture devices, in system order.</summary>
    public static IReadOnlyList<string> GetDeviceNames()
    {
        var names = new List<string>();
        ForEachMoniker(moniker =>
        {
            names.Add(ReadFriendlyName(moniker));
            return false;
        });
        return names;
    }

    /// <summary>Creates the capture filter of the device at <paramref name="index"/>.</summary>
    public static IBaseFilter CreateFilter(int index)
    {
        IBaseFilter? filter = null;
        var current = 0;
        ForEachMoniker(moniker =>
        {
            if (current++ != index)
            {
                return false;
            }

            var interfaceId = DsGuids.BaseFilterInterface;
            moniker.BindToObject(null!, null!, ref interfaceId, out var bound);
            filter = (IBaseFilter)bound;
            return true;
        });

        return filter ?? throw new InvalidOperationException("The video device was disconnected.");
    }

    /// <param name="visit">Called for each device moniker; return <c>true</c> to stop enumerating.</param>
    private static void ForEachMoniker(Func<IMoniker, bool> visit)
    {
        var deviceEnum = ComHelper.CreateInstance<ICreateDevEnum>(DsGuids.SystemDeviceEnum);
        IEnumMoniker? enumMoniker = null;
        try
        {
            var category = DsGuids.VideoInputDeviceCategory;
            if (deviceEnum.CreateClassEnumerator(ref category, out enumMoniker, 0) != 0 || enumMoniker is null)
            {
                return; // No video devices installed.
            }

            var monikers = new IMoniker[1];
            while (enumMoniker.Next(1, monikers, IntPtr.Zero) == 0)
            {
                try
                {
                    if (visit(monikers[0]))
                    {
                        return;
                    }
                }
                finally
                {
                    ComHelper.Release(monikers[0]);
                }
            }
        }
        finally
        {
            ComHelper.Release(enumMoniker);
            ComHelper.Release(deviceEnum);
        }
    }

    private static string ReadFriendlyName(IMoniker moniker)
    {
        object? bag = null;
        try
        {
            var interfaceId = DsGuids.PropertyBagInterface;
            moniker.BindToStorage(null!, null!, ref interfaceId, out bag);
            return ((IPropertyBag)bag).Read("FriendlyName", out var value, IntPtr.Zero) == 0 && value is not null
                ? value.ToString()
                : UnknownDeviceName;
        }
        catch (Exception ex) when (ex is System.Runtime.InteropServices.COMException || ex is InvalidCastException)
        {
            return UnknownDeviceName;
        }
        finally
        {
            ComHelper.Release(bag);
        }
    }
}
