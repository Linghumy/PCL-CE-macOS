using PCL.Core.Utils;
using PCL.Network;

namespace PCL;

/// <summary>从 PCL-CE-macOS 的 GitHub Releases 获取版本信息。</summary>
public sealed class GitHubReleaseUpdateSource : IUpdateSource
{
    private const string ReleasesApi =
        "https://api.github.com/repos/Linghumy/PCL-CE-macOS/releases?per_page=30";

    private JsonArray? _releases;

    public string SourceName { get; set; } = "GitHub Releases";

    public bool IsAvailable()
    {
        return true;
    }

    public bool RefreshCache()
    {
        _releases = Requester.FetchJson(
                ReleasesApi,
                new RequestParam
                {
                    Retries = 2,
                    UseBrowserUserAgent = true
                })
            as JsonArray
            ?? throw new InvalidOperationException("GitHub Releases 返回了无效数据");
        return true;
    }

    public VersionDataModel GetLatestVersion(UpdateSourceChannel channel, UpdateArch arch)
    {
        var release = GetRelease(channel);
        if (release is null)
            throw new InvalidOperationException("GitHub 仓库暂未发布可用版本");

        var tag = release["tag_name"]?.ToString() ?? string.Empty;
        var name = tag.StartsWith('v') ? tag[1..] : tag;
        if (string.IsNullOrWhiteSpace(name))
            throw new InvalidOperationException("GitHub Release 缺少 tag_name");

        return new VersionDataModel
        {
            Source = SourceName,
            VersionCode = 0,
            VersionName = name,
            Sha256 = GetAssetSha256(release, arch),
            Changelog = release["body"]?.ToString() ?? string.Empty
        };
    }

    public bool IsLatest(
        UpdateSourceChannel channel,
        UpdateArch arch,
        SemVer currentVersion,
        int currentVersionCode)
    {
        if (_releases is null)
            RefreshCache();

        var release = GetRelease(channel);
        if (release is null)
            return true;

        var tag = release["tag_name"]?.ToString() ?? string.Empty;
        var version = tag.StartsWith('v') ? tag[1..] : tag;
        return !SemVer.TryParse(version, out var latest)
               || currentVersion >= latest;
    }

    public VersionAnnouncementDataModel GetAnnouncementList()
    {
        // 启动器公告单独使用原 PCL-CE 的公开公告接口。
        return new VersionAnnouncementDataModel
        {
            Content = []
        };
    }

    public List<Loading.LoaderBase> GetDownloadLoader(
        UpdateSourceChannel channel,
        UpdateArch arch,
        string output)
    {
        throw new NotSupportedException("PCL-CE-macOS 使用 GitHub Releases 页面手动更新");
    }

    private JsonObject? GetRelease(UpdateSourceChannel channel)
    {
        if (_releases is null)
            RefreshCache();

        var candidates = _releases?
            .OfType<JsonObject>()
            .Where(release => release["draft"]?.GetValue<bool>() != true)
            .ToList() ?? [];
        return channel == UpdateSourceChannel.Beta
            ? candidates.FirstOrDefault(release => release["prerelease"]?.GetValue<bool>() == true)
              ?? candidates.FirstOrDefault(release => release["prerelease"]?.GetValue<bool>() != true)
            : candidates.FirstOrDefault(release => release["prerelease"]?.GetValue<bool>() != true);
    }

    private static string? GetAssetSha256(JsonObject release, UpdateArch arch)
    {
        if (release["assets"] is not JsonArray assets)
            return null;

        var expectedArch = arch == UpdateArch.Arm64 ? "arm64" : "x64";
        var asset = assets
            .OfType<JsonObject>()
            .FirstOrDefault(item =>
                item["name"]?.ToString().Contains(expectedArch, StringComparison.OrdinalIgnoreCase)
                == true);
        var digest = asset?["digest"]?.ToString();
        return digest?.StartsWith("sha256:", StringComparison.OrdinalIgnoreCase) == true
            ? digest["sha256:".Length..]
            : null;
    }
}
