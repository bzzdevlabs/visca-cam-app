using System;
using AppKit;

namespace TenveoPtz.Mac;

internal static class Program
{
    private static void Main(string[] args)
    {
        NSApplication.Init();
        NSApplication.SharedApplication.Delegate = new AppDelegate(args);
        NSApplication.Main(args);
    }
}
