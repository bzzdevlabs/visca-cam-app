using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows.Forms;
using Microsoft.Win32;
using TenveoPtz.Core.Diagnostics;

namespace TenveoPtz.App.Infrastructure.Diagnostics;

/// <summary>Plain-text summary of the environment, settings and recent log, to paste into a bug report.</summary>
internal static class DiagnosticsReport
{
    private const int LogLines = 300;

    public static string Build(LogHub log, string dataFolder)
    {
        var report = new StringBuilder()
            .AppendLine("Tenveo PTZ diagnostics, " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture))
            .AppendLine("App version:     " + Application.ProductVersion)
            .AppendLine("Windows:         " + Environment.OSVersion.VersionString + (Environment.Is64BitOperatingSystem ? ", 64-bit" : ", 32-bit"))
            .AppendLine(".NET Framework:  " + FrameworkRelease() + ", CLR " + Environment.Version + (Environment.Is64BitProcess ? ", 64-bit process" : ", 32-bit process"))
            .AppendLine("Culture:         " + CultureInfo.CurrentCulture.Name)
            .AppendLine("Screen DPI:      " + DeviceDpi())
            .AppendLine("Data folder:     " + dataFolder)
            .AppendLine("Log level:       " + log.MinimumLevel)
            .AppendLine();

        AppendFile(report, Path.Combine(dataFolder, "settings.xml"));
        AppendFile(report, Path.Combine(dataFolder, "presets.xml"));

        var entries = log.Recent();
        report.AppendLine($"--- Last {Math.Min(LogLines, entries.Count)} log entries ---");
        for (var i = Math.Max(0, entries.Count - LogLines); i < entries.Count; i++)
        {
            report.AppendLine(entries[i].ToString());
        }

        return report.ToString();
    }

    private static void AppendFile(StringBuilder report, string path)
    {
        report.AppendLine("--- " + Path.GetFileName(path) + " ---");
        try
        {
            report.AppendLine(File.Exists(path) ? File.ReadAllText(path).Trim() : "(none)");
        }
        catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
        {
            report.AppendLine("(unreadable: " + ex.Message + ")");
        }

        report.AppendLine();
    }

    /// <summary>The installed .NET Framework 4.x release number (e.g. 533320 for 4.8.1).</summary>
    private static string FrameworkRelease()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full");
            return key?.GetValue("Release") is int release ? "release " + release : "unknown";
        }
        catch (Exception ex) when (ex is System.Security.SecurityException || ex is UnauthorizedAccessException || ex is IOException)
        {
            return "unknown";
        }
    }

    private static string DeviceDpi()
    {
        using var graphics = System.Drawing.Graphics.FromHwnd(IntPtr.Zero);
        return graphics.DpiX.ToString(CultureInfo.InvariantCulture);
    }
}
