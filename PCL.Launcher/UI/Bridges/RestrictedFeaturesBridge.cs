namespace PCL.UI.Bridges;

/// <summary>
/// PCL-CE-macOS 不按国家或地区禁用 Minecraft 下载、安装、修复与整合包功能。
/// 保留该接口是为了兼容原版的调用点与自定义事件扩展。
/// </summary>
public sealed class RestrictedFeaturesBridge : IRestrictedFeatures
{
    public bool IsRestricted => false;

    public bool Check(string feature) => true;

    public void ThrowIfRestricted(string feature)
    {
    }
}
