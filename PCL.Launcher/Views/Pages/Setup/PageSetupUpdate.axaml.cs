using Avalonia.Threading;
using PCL.Core.App;
using PCL.Navigation;
using PCL.UI.Controls;

namespace PCL.Views.Pages.Setup;

/// <summary>
/// 更新设置页：更新通道、自动更新行为与 GitHub Releases 检查入口。 检查更新和更新日志由
/// <see cref="PageSetupUpdateViewModel" /> 负责。
/// </summary>
public partial class PageSetupUpdate : MyPageRight
{
    private bool _loaded;
    private bool _reloading;

    public PageSetupUpdate()
    {
        InitializeComponent();
        Current = this;
        PanScroll = PanBack;
        DataContext = ViewModel;

        ComboSystemUpdateChannel.SelectionChanged += async (_, _) =>
            await OnChannelChangedAsync();
        BtnCheckAgain.Click += async (_, _) => await ViewModel.CheckUpdateAsync();
        BtnChangelog.Click += (_, _) => PageSetupUpdateViewModel.OpenReleasesPage();
        BtnChangelogDetail.Click += async (_, _) => await ViewModel.ShowChangelogAsync();
        BtnUpdate.Click += async (_, _) => await ViewModel.StartUpdateAsync();
        ComboSystemUpdateMode.IsEnabled = UpdateManager.SupportsInAppUpdate;
        Loaded += async (_, _) =>
        {
            if (_loaded) return;
            _loaded = true;
            Reload();
            await ViewModel.CheckUpdateAsync();
        };
    }

    public static PageSetupUpdate? Current { get; private set; }

    public PageSetupUpdateViewModel ViewModel { get; } = new();

    public void SetUpdateButton(string textKey, bool enabled, params object?[] args)
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(() => SetUpdateButton(textKey, enabled, args));
            return;
        }

        ViewModel.SetExternalUpdateButtonText(textKey, args);
        ViewModel.UpdateButtonEnabled = enabled;
    }

    public override void OnNavigatedTo(PageParameter? parameter)
    {
        PanBack.ScrollToHome();
        if (!_loaded) return;
        Reload();
        _ = ViewModel.SyncUpdateButtonStateAsync();
    }

    public override void OnNavigatedFrom()
    {
    }

    private void Reload()
    {
        _reloading = true;
        try
        {
            ViewModel.Reload();
            Config.Update.UpdateChannel = UpdateChannel.Release;
            ComboSystemUpdateChannel.SelectedIndex = 0;
            ComboSystemUpdateMode.SelectedIndex = Math.Clamp(
                (int)Config.Update.UpdateMode,
                0,
                3);
        }
        finally
        {
            _reloading = false;
        }
    }

    private async Task OnChannelChangedAsync()
    {
        if (_reloading || ComboSystemUpdateChannel.SelectedIndex < 0) return;
        var index = ComboSystemUpdateChannel.SelectedIndex;
        if (!await ViewModel.ChangeChannelAsync(index))
        {
            _reloading = true;
            try
            {
                ComboSystemUpdateChannel.SelectedIndex = 0;
            }
            finally
            {
                _reloading = false;
            }

            return;
        }

        await ViewModel.CheckUpdateAsync();
    }
}
