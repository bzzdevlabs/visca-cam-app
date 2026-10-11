namespace TenveoPtz.Core.Updates;

/// <summary>Replaces the installed application with a downloaded release asset.</summary>
public interface IPackageInstaller
{
    void Install(string packagePath);
}
