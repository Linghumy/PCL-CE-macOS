using System.Linq;
using PCL.Core.App;
using PCL.Core.Platform;

namespace PCL;

/// <summary>业务层使用的路径常量。</summary>
public static class LauncherPaths
{
    private const string LegacyMinecraftFolderName = ".minecraft";

    /// <summary>Mesa Loader Windows 版本。</summary>
    public const string MesaLoaderWindowsVersion = "26.0.4";

    /// <summary>程序内嵌图片根路径，具体 URI 方案由 UI 层注册的资源访问实现决定。</summary>
    public static string ImageRoot => Basics.GetAppImageRootUri();

    /// <summary>程序可执行文件所在目录，以路径分隔符结尾。</summary>
    public static string ExecutableDirectory { get; } =
        Basics.ExecutableDirectory.EndsWith(Path.DirectorySeparatorChar)
            ? Basics.ExecutableDirectory
            : Basics.ExecutableDirectory + Path.DirectorySeparatorChar;

    /// <summary>启动器用户数据根目录，以路径分隔符结尾。</summary>
    public static string UserDataRoot { get; } = _GetUserDataRoot();

    /// <summary>默认 Minecraft 文件夹，不是以路径分隔符结尾的路径。</summary>
    public static string DefaultMinecraftDirectory { get; } = _GetDefaultMinecraftDirectory();

    /// <summary>默认 Minecraft 文件夹在用户数据目录中的配置占位符。</summary>
    public static string DefaultMinecraftFolderToken =>
        "$"
        + (OperatingSystem.IsMacOS() ? "Minecraft" : LegacyMinecraftFolderName)
        + Path.DirectorySeparatorChar;

    /// <summary>临时目录，以路径分隔符结尾。</summary>
    public static string Temp => Paths.Temp + Path.DirectorySeparatorChar;

    /// <summary>可用于临时存放文件的、不含特殊字符的目录，以路径分隔符结尾。</summary>
    public static string PureAsciiDirectory
    {
        get
        {
            var dataDirectory = Paths.Data;
            if (dataDirectory.All(c => c < 128))
                return dataDirectory.EndsWith(Path.DirectorySeparatorChar)
                    ? dataDirectory
                    : dataDirectory + Path.DirectorySeparatorChar;

            var temp = Path.GetTempPath();
            if (temp.All(c => c < 128))
                return temp.EndsWith(Path.DirectorySeparatorChar)
                    ? temp
                    : temp + Path.DirectorySeparatorChar;

            return ApplicationRuntime
                    .GetService<IPlatformPathService>()
                    ?.GetPureAsciiDirectory()
                ?? Path.Combine(temp, "PCL") + Path.DirectorySeparatorChar;
        }
    }

    /// <summary>
    /// 官方启动器的默认 Minecraft 文件夹，以路径分隔符结尾。 Windows 为 <c>%APPDATA%\.minecraft</c>，macOS 为
    /// <c>~/Library/Application Support/minecraft</c>， Linux 为 <c>~/.minecraft</c>。
    /// </summary>
    public static string OfficialMinecraftDirectory =>
        _WithTrailingSeparator(
            ApplicationRuntime
                .GetService<IPlatformPathService>()
                ?.GetOfficialMinecraftDirectory()
            ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".minecraft"));

    /// <summary>当前 Minecraft 文件夹路径，由 WPF 宿主同步设置。</summary>
    public static string? MinecraftFolder { get; set; }

    private static string _GetUserDataRoot()
    {
        var dataDirectory = Environment.GetEnvironmentVariable("PCL_DATA_DIR");
        if (!string.IsNullOrWhiteSpace(dataDirectory))
            return _WithTrailingSeparator(Paths.ExpandUserPath(dataDirectory));

        if (!string.IsNullOrWhiteSpace(Paths.PackageKind))
            return _WithTrailingSeparator(Paths.Data);

        return ExecutableDirectory;
    }

    private static string _GetDefaultMinecraftDirectory()
    {
        var minecraftDirectory = Environment.GetEnvironmentVariable("PCL_MINECRAFT_DIR");
        if (!string.IsNullOrWhiteSpace(minecraftDirectory))
        {
            var explicitPath = Path.TrimEndingDirectorySeparator(
                Path.GetFullPath(Paths.ExpandUserPath(minecraftDirectory)));
            var parent = Directory.GetParent(explicitPath)?.FullName;
            if (OperatingSystem.IsMacOS() && !string.IsNullOrWhiteSpace(parent))
            {
                var folderName = Path.GetFileName(explicitPath);
                if (folderName is LegacyMinecraftFolderName or "Minecraft")
                    return _ResolveDefaultMinecraftDirectory(parent);
            }

            return explicitPath;
        }

        var dataDirectory = Environment.GetEnvironmentVariable("PCL_DATA_DIR");
        if (!string.IsNullOrWhiteSpace(dataDirectory))
        {
            return _ResolveDefaultMinecraftDirectory(
                Path.GetFullPath(Paths.ExpandUserPath(dataDirectory)));
        }

        if (!string.IsNullOrWhiteSpace(Paths.PackageKind))
            return _ResolveDefaultMinecraftDirectory(Paths.CentralRoot);

        return _ResolveDefaultMinecraftDirectory(ExecutableDirectory);
    }

    /// <summary>
    /// 将旧版 macOS 集中目录中的 <c>.minecraft</c> 迁移为可见的 <c>Minecraft</c>。
    /// 若新旧目录同时存在，则保留两者并继续使用新目录，避免覆盖用户数据。
    /// </summary>
    private static string _ResolveDefaultMinecraftDirectory(string baseDirectory)
    {
        var preferredName = OperatingSystem.IsMacOS()
            ? "Minecraft"
            : LegacyMinecraftFolderName;
        var preferred = Path.TrimEndingDirectorySeparator(
            Path.Combine(baseDirectory, preferredName));
        if (!OperatingSystem.IsMacOS())
            return preferred;

        var legacy = Path.TrimEndingDirectorySeparator(
            Path.Combine(baseDirectory, LegacyMinecraftFolderName));
        if (!Directory.Exists(legacy))
            return preferred;
        if (Directory.Exists(preferred))
        {
            if (!_IsMinecraftSkeleton(preferred))
                return preferred;

            var backup = preferred + $".empty-{Guid.NewGuid():N}";
            Directory.Move(preferred, backup);
            try
            {
                Directory.Move(legacy, preferred);
                Directory.Delete(backup, true);
                return preferred;
            }
            catch
            {
                if (!Directory.Exists(preferred) && Directory.Exists(backup))
                    Directory.Move(backup, preferred);
                throw;
            }
        }

        try
        {
            Directory.Move(legacy, preferred);
            return preferred;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Console.Error.WriteLine(
                $"[Paths] 无法将 Minecraft 文件夹迁移到可见位置，继续使用旧目录：{ex.Message}");
            return legacy;
        }
    }

    private static bool _IsMinecraftSkeleton(string directory)
    {
        try
        {
            foreach (var entry in Directory.EnumerateFileSystemEntries(directory))
            {
                var name = Path.GetFileName(entry);
                if (name == "launcher_profiles.json" && File.Exists(entry))
                    continue;
                if (name == "versions"
                    && Directory.Exists(entry)
                    && !Directory.EnumerateFileSystemEntries(entry).Any())
                {
                    continue;
                }

                return false;
            }

            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    /// <summary>将旧版 <c>.minecraft</c> 绝对路径映射到迁移后的默认目录。</summary>
    public static string MigrateLegacyMinecraftDirectoryPath(string path)
    {
        if (!OperatingSystem.IsMacOS() || string.IsNullOrWhiteSpace(path))
            return path;

        var hasTrailingSeparator = path.EndsWith(Path.DirectorySeparatorChar);
        var normalized = Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));
        var legacy = Path.TrimEndingDirectorySeparator(
            Path.Combine(UserDataRoot, LegacyMinecraftFolderName));
        if (!PathsEqual(normalized, legacy)
            || Directory.Exists(legacy)
            || !Directory.Exists(DefaultMinecraftDirectory))
        {
            return path;
        }

        return DefaultMinecraftDirectory
            + (hasTrailingSeparator ? Path.DirectorySeparatorChar : string.Empty);
    }

    /// <summary>把用户数据目录下的路径转换为可持久化的 <c>$</c> 占位符形式。</summary>
    public static string ToUserDataToken(string path)
    {
        var hasTrailingSeparator = path.EndsWith(Path.DirectorySeparatorChar);
        var fullPath = Path.GetFullPath(path);
        var relative = Path.GetRelativePath(UserDataRoot, fullPath);
        if (relative == "."
            || relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal)
            || relative == "..")
        {
            return fullPath;
        }

        return "$"
               + relative.Replace(Path.DirectorySeparatorChar, '/')
               + (hasTrailingSeparator ? "/" : string.Empty);
    }

    private static bool PathsEqual(string left, string right)
    {
        return string.Equals(
            Path.TrimEndingDirectorySeparator(left),
            Path.TrimEndingDirectorySeparator(right),
            OperatingSystem.IsMacOS() || OperatingSystem.IsWindows()
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal);
    }

    private static string _WithTrailingSeparator(string path)
    {
        return path.EndsWith(Path.DirectorySeparatorChar)
            ? path
            : path + Path.DirectorySeparatorChar;
    }
}
