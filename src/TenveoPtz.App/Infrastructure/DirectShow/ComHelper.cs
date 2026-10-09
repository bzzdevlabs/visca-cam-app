using System;
using System.Runtime.InteropServices;

namespace TenveoPtz.App.Infrastructure.DirectShow;

internal static class ComHelper
{
    public static T CreateInstance<T>(Guid classId)
        where T : class
    {
        var type = Type.GetTypeFromCLSID(classId, throwOnError: true)!;
        return (T)Activator.CreateInstance(type);
    }

    public static void ThrowOnFailure(int hresult, string operation)
    {
        if (hresult < 0)
        {
            throw new COMException($"DirectShow {operation} failed (0x{hresult:X8}).", hresult);
        }
    }

    public static void Release(object? comObject)
    {
        if (comObject is not null && Marshal.IsComObject(comObject))
        {
            Marshal.ReleaseComObject(comObject);
        }
    }
}
