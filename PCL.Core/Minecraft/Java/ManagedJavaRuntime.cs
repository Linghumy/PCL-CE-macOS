using System;
using System.IO;
using System.Linq;
using PCL.Core.App;

namespace PCL.Core.Minecraft.Java;

/// <summary>PCL-CE 下载并维护的 Java 运行时目录。</summary>
public static class ManagedJavaRuntime
{
    public static string Root
    {
        get
        {
            var configured = Environment.GetEnvironmentVariable("PCL_JAVA_DIR");
            var defaultRoot = Paths.PackageKind == "macos-app"
                ? Path.Combine(Paths.CentralRoot, "Java")
                : Path.Combine(Paths.SharedLocalData, "Java");
            return Path.TrimEndingDirectorySeparator(
                Path.GetFullPath(
                    string.IsNullOrWhiteSpace(configured)
                        ? defaultRoot
                        : Paths.ExpandUserPath(configured)));
        }
    }

    public static bool Contains(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;

        var root = Root + Path.DirectorySeparatorChar;
        var fullPath = Path.GetFullPath(path);
        return fullPath.StartsWith(
            root,
            Path.DirectorySeparatorChar == '\\'
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal);
    }

    public static bool TryGetInstallationRoot(string path, out string installationRoot)
    {
        installationRoot = string.Empty;
        if (!Contains(path))
            return false;

        var relative = Path.GetRelativePath(Root, Path.GetFullPath(path));
        var component = relative.Split(
                [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar],
                StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault();
        if (string.IsNullOrWhiteSpace(component))
            return false;

        var candidate = Path.GetFullPath(Path.Combine(Root, component));
        if (!candidate.StartsWith(
                Root + Path.DirectorySeparatorChar,
                Path.DirectorySeparatorChar == '\\'
                    ? StringComparison.OrdinalIgnoreCase
                    : StringComparison.Ordinal))
        {
            return false;
        }

        installationRoot = candidate;
        return true;
    }
}
