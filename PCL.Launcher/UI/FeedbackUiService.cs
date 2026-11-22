using PCL.Core.App;
using PCL.Core.App.Localization;
using PCL.Core.Logging;
using PCL.Core.UI.Dialogs;
using PCL.Core.UI.Hints;

namespace PCL.UI;

/// <summary>将业务层日志等级映射到启动器的本地提示与错误弹窗。</summary>
internal static class UiLogBridge
{
    private static bool _installed;

    public static void Install()
    {
        if (_installed) return;
        _installed = true;
        LauncherCommon.UiLogHandler = Handle;
    }

    private static void Handle(
        Exception? exception,
        string text,
        LauncherLogLevel level,
        string? title,
        string? userSummary)
    {
        switch (level)
        {
            case LauncherLogLevel.Hint:
                HintService.Show(Hints.Error(userSummary ?? text));
                break;

            case LauncherLogLevel.Msgbox:
                DialogService.Instance.ShowNotification(
                    new TextDialogRequest
                    {
                        Title = string.IsNullOrWhiteSpace(title)
                            ? Lang.Text("SystemDialog.Error.Title")
                            : title,
                        Content = userSummary ?? text,
                        IsWarn = true
                    });
                break;

            case LauncherLogLevel.Feedback:
                FeedbackUiService.Show(exception, text, userSummary, title, false);
                break;

            case LauncherLogLevel.Critical:
                FeedbackUiService.Show(exception, text, userSummary, title, true);
                break;
        }
    }
}

/// <summary>本地错误提示，不再连接外部反馈服务。</summary>
public static class FeedbackUiService
{
    private static int _criticalTriggered;

    public static void Show(
        Exception? exception,
        string text,
        string? userSummary,
        string? title,
        bool critical)
    {
        _ = ShowAsync(exception, text, userSummary, title, critical);
    }

    private static async Task ShowAsync(
        Exception? exception,
        string text,
        string? userSummary,
        string? title,
        bool critical)
    {
        try
        {
            if (critical && Interlocked.Exchange(ref _criticalTriggered, 1) != 0)
                return;

            var message = userSummary ?? text;
            if (exception is not null && userSummary is null)
                message += Environment.NewLine + Environment.NewLine + exception.Message;

            await DialogService.Instance.ShowAsync(
                new TextDialogRequest
                {
                    Title = string.IsNullOrWhiteSpace(title)
                        ? Lang.Text("SystemDialog.Error.Title")
                        : title,
                    Content = message,
                    Buttons = DialogButtons.Ok,
                    IsWarn = true
                });
        }
        catch (Exception ex)
        {
            LogWrapper.Warn(ex, "UI", "显示错误提示失败");
            HintService.Show(Hints.Error(userSummary ?? text));
        }
    }
}
