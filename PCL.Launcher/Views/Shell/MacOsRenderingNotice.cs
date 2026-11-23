using PCL.Core.App;
using PCL.Core.App.Localization;
using PCL.Core.UI.Dialogs;

namespace PCL.Views.Shell;

/// <summary>
/// macOS OpenGL 渲染性能的一次性提示。
/// MC 原生支持 Vulkan 后，可直接删除本文件、StartupEffects 中的调用、对应状态项与本地化资源。
/// </summary>
internal static class MacOsRenderingNotice
{
    public static async Task ShowIfNeededAsync()
    {
        if (!OperatingSystem.IsMacOS()
            || States.Hint.MacOsVulkanNoticeShown
            || !States.System.LauncherEula)
            return;

        await DialogService.Instance.ShowAsync(
            new TextDialogRequest
            {
                Title = Lang.Text("Main.MacOsVulkanNotice.Title"),
                Content = Lang.Text("Main.MacOsVulkanNotice.Message"),
                Buttons = DialogButtons.Ok,
                IsWarn = true
            });

        States.Hint.MacOsVulkanNoticeShown = true;
    }
}
