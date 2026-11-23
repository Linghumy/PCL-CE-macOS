using Avalonia.Controls;
using Avalonia.Data;
using PCL.Core.App;
using PCL.Core.App.Configuration;
using PCL.Core.App.Localization;
using PCL.Core.Platform;
using PCL.Core.UI;
using PCL.Core.UI.Dialogs;
using PCL.Core.UI.Hints;
using PCL.Navigation;
using PCL.UI;
using PCL.UI.Controls;

namespace PCL.Views.Pages.Setup;

/// <summary>
/// 启动器杂项设置页：公告显示、日志上限、网络代理与调试选项。 配置状态由 <see cref="PageSetupLauncherMiscViewModel" />
/// 持有，页面处理系统级文件操作与重启。
/// </summary>
public partial class PageSetupLauncherMisc : MyPageRight, ISetupResetPage
{
    private bool _loaded;
    private bool _reloading;
    private bool _storageDialogOpen;

    public PageSetupLauncherMisc()
    {
        InitializeComponent();
        PanScroll = PanBack;
        DataContext = ViewModel;

        SliderMaxLog.getHintText = new Func<object, object>(FormatMaxLogHint);
        SliderDebugAnim.getHintText = new Func<object, object>(FormatAnimationSpeedHint);
        ComboDebugLinuxProtocolAuto.Bind(
            ContentControl.ContentProperty,
            new Binding(nameof(ViewModel.LinuxProtocolAutoText))
            {
                Source = ViewModel
            });
        ComboDebugLinuxProtocolAuto.Bind(
            ToolTip.TipProperty,
            new Binding(nameof(ViewModel.LinuxProtocolAutoToolTip))
            {
                Source = ViewModel
            });

        ComboSystemActivity.SelectionChanged += async (_, _) => await OnAnnouncementChangedAsync();
        SliderMaxLog.Change += (_, _) =>
            WriteIfReady(() => Config.System.MaxGameLog = SliderMaxLog.Value);
        SliderDebugAnim.Change += (_, _) =>
            WriteIfReady(() => Config.Debug.AnimationSpeed = SliderDebugAnim.Value);

        var supportsProtocol = ApplicationRuntime.Current.Platform.Capabilities
            .SupportsDisplayProtocolSelection;
        ComboDebugLinuxProtocol.IsEnabled = supportsProtocol;
        ComboDebugLinuxProtocol.SetLocalizedValue(
            ToolTip.TipProperty,
            supportsProtocol
                ? "Setup.Misc.Debug.LinuxProtocol.ToolTip"
                : "Setup.Misc.Debug.LinuxProtocol.OnlyLinux");
        ToolTip.SetShowOnDisabled(ComboDebugLinuxProtocol, true);
        ComboDebugLinuxProtocol.SelectionChanged += (_, _) =>
        {
            if (_reloading) return;
            WriteIfReady(() =>
            {
                Config.Debug.LinuxProtocol = Math.Clamp(
                    ComboDebugLinuxProtocol.SelectedIndex,
                    0,
                    2);
                HintService.Show(
                    Hints.Info(
                        Lang.Text("Setup.Misc.Debug.LinuxProtocol.Restart"),
                        false));
            });
        };
        CheckSystemDisableHardwareAcceleration.Change += (_, _) => WriteIfReady(() =>
        {
            Config.System.DisableHardwareAcceleration =
                CheckSystemDisableHardwareAcceleration.Checked == true;
            HintService.Show(
                Hints.Info(Lang.Text("Setup.Misc.HardwareAcceleration.RestartNotice")));
        });
        CheckNetDohEnable.Change += (_, _) =>
            WriteIfReady(() => Config.Network.EnableDoH = CheckNetDohEnable.Checked == true);
        CheckDebugSkipCopy.Change += (_, _) =>
            WriteIfReady(() => Config.Debug.DontCopy = CheckDebugSkipCopy.Checked == true);
        CheckDebugMode.Change += (_, _) => WriteIfReady(() =>
        {
            Config.Debug.Enabled = CheckDebugMode.Checked == true;
            HintService.Show(Hints.Info(Lang.Text("Setup.Misc.Debug.Mode.Hint"), false));
        });
        CheckDebugDelay.Change += (_, _) =>
            WriteIfReady(() => Config.Debug.AddRandomDelay = CheckDebugDelay.Checked == true);

        RadioHttpProxyType0.Check += (_, _) => WriteProxyType(0);
        RadioHttpProxyType1.Check += (_, _) => WriteProxyType(1);
        RadioHttpProxyType2.Check += (_, _) => WriteProxyType(2);

        BtnApplyHttpProxy.Click += (_, _) => ViewModel.ApplyHttpProxy();
        BtnSystemSettingExp.Click += (_, _) => ExportSettings();
        BtnSystemSettingImp.Click += async (_, _) => await ImportSettingsAsync();
        CardStorage.IsVisible = OperatingSystem.IsMacOS();
        BtnStorageOpenRoot.Click += (_, _) => Basics.OpenPath(Paths.CentralRoot);
        BtnStorageClear.Click += async (_, _) => await ClearSelectedStorageAsync();
        CheckStorageAll.Change += (_, _) =>
        {
            var all = CheckStorageAll.Checked == true;
            CheckStorageSettings.Checked = all;
            CheckStorageCache.Checked = all;
            CheckStorageJava.Checked = all;
            CheckStorageMinecraft.Checked = all;
        };
        CheckStorageSettings.Checked = true;
        CheckStorageCache.Checked = true;
        CheckStorageJava.Checked = true;
        CheckStorageMinecraft.Checked = false;

        Loaded += (_, _) =>
        {
            if (_loaded) return;
            _loaded = true;
            Reload();
        };
    }

    public PageSetupLauncherMiscViewModel ViewModel { get; } = new();

    public void Reset()
    {
        try
        {
            Config.Network.Reset();
            Config.Debug.Reset();
            Config.System.Reset();
            HintService.Show(Hints.Success(Lang.Text("Setup.Misc.Initialized"), false));
        }
        catch (Exception ex)
        {
            LauncherCommon.Log(
                ex,
                Lang.Text("Setup.Misc.Error.InitFailed"),
                LauncherLogLevel.Msgbox,
                userSummary: Lang.Text("Setup.Misc.Error.InitFailed"));
        }

        Reload();
    }

    public override void OnNavigatedTo(PageParameter? parameter)
    {
        PanBack.ScrollToHome();
        if (_loaded) Reload();
    }

    public override void OnNavigatedFrom()
    {
    }

    public void Reload()
    {
        _reloading = true;
        try
        {
            ViewModel.Reload();
            SliderMaxLog.Value = Config.System.MaxGameLog;
            CheckSystemDisableHardwareAcceleration.Checked =
                Config.System.DisableHardwareAcceleration;

            CheckNetDohEnable.Checked = Config.Network.EnableDoH;
            var proxyType = Math.Clamp(Config.Network.HttpProxy.Type, 0, 2);
            var radios = new[]
            {
                RadioHttpProxyType0,
                RadioHttpProxyType1,
                RadioHttpProxyType2
            };
            radios[proxyType].SetChecked(true, false);
            PanHttpProxyCustom.IsVisible = proxyType == 2;

            SliderDebugAnim.Value = Config.Debug.AnimationSpeed;
            ComboDebugLinuxProtocol.SelectedIndex =
                Math.Clamp(Config.Debug.LinuxProtocol, 0, 2);
            CheckDebugSkipCopy.Checked = Config.Debug.DontCopy;
            CheckDebugMode.Checked = Config.Debug.Enabled;
            CheckDebugDelay.Checked = Config.Debug.AddRandomDelay;
            LabStorageRoot.Text = Paths.CentralRoot;
        }
        finally
        {
            _reloading = false;
        }

    }

    private void WriteIfReady(Action action)
    {
        if (_reloading) return;
        try
        {
            action();
        }
        catch (Exception ex)
        {
            LauncherCommon.Log(ex, "写入杂项设置失败");
        }
    }

    private void WriteProxyType(int type)
    {
        if (_reloading) return;
        Config.Network.HttpProxy.Type = type;
        PanHttpProxyCustom.IsVisible = type == 2;
    }

    private async Task OnAnnouncementChangedAsync()
    {
        if (_reloading || ComboSystemActivity.SelectedIndex < 0) return;
        var index = ComboSystemActivity.SelectedIndex;
        if (index == 2)
        {
            var result = await DialogService.Instance.ShowAsync(
                new TextDialogRequest
                {
                    Title = Lang.Text("Common.Dialog.Warning"),
                    Content = Lang.Text("Setup.Misc.System.Announcement.Disabled.Warning.Message"),
                    Buttons = DialogButtons.Create(
                        "Setup.Misc.System.Announcement.Disabled.Warning.Confirm",
                        "Common.Action.Cancel"),
                    IsWarn = true
                });
            if (!result.IsConfirmed)
            {
                _reloading = true;
                try
                {
                    ComboSystemActivity.SelectedIndex =
                        Math.Clamp(States.System.AnnounceSolution, 0, 2);
                }
                finally
                {
                    _reloading = false;
                }

                return;
            }
        }

        States.System.AnnounceSolution = index;
        ViewModel.ActivityIndex = index;
    }

    private static async void ExportSettings()
    {
        var savePath = await SystemDialogs.SelectSaveFileAsync(
            Lang.Text("Setup.Misc.System.ExportSettings.SaveTitle"),
            "PCL CE 全局配置.json",
            Lang.Text("Setup.Misc.System.ExportSettings.Filter"),
            Basics.ExecutableDirectory);
        if (string.IsNullOrWhiteSpace(savePath)) return;
        File.Copy(ConfigService.SharedConfigPath, savePath, true);
        HintService.Show(Hints.Success(Lang.Text("Setup.Misc.System.ExportSettings.Success")));
        Basics.OpenPath(savePath);
    }

    private static async Task ImportSettingsAsync()
    {
        var sourcePath = await SystemDialogs.SelectFileAsync(
            Lang.Text("Setup.Misc.System.ExportSettings.Filter"),
            Lang.Text("Setup.Misc.System.ImportSettings.SelectTitle"));
        if (string.IsNullOrWhiteSpace(sourcePath)) return;
        try
        {
            File.Copy(sourcePath, ConfigService.SharedConfigPath, true);
            await DialogService.Instance.ShowAsync(
                new TextDialogRequest
                {
                    Title = Lang.Text("Common.Dialog.Title"),
                    Content = Lang.Text("Setup.Misc.System.ImportSettings.Success.Message"),
                    Buttons = DialogButtons.From(
                        DialogButton.Create(
                            Lang.Text("Setup.Misc.System.ImportSettings.Success.Restart")))
                });
            // 导入的配置需要重启后生效；macOS 上由 UiInteraction 关闭程序，交由用户手动启动。
            UiInteraction.Restart();
        }
        catch (Exception ex)
        {
            LauncherCommon.Log(
                ex,
                "导入配置失败",
                LauncherLogLevel.Msgbox,
                userSummary: Lang.Text("Setup.Error.OperationFailed"));
        }
    }

    private static object FormatMaxLogHint(object value)
    {
        var tick = Convert.ToInt32(value);
        return tick switch
        {
            <= 5 => tick * 10 + 50,
            <= 13 => tick * 50 - 150,
            <= 28 => tick * 100 - 800,
            _ => Lang.Text("Setup.Misc.System.MaxLogLines.Unlimited")
        };
    }

    private static object FormatAnimationSpeedHint(object value)
    {
        var tick = Convert.ToInt32(value);
        if (tick > 29) return Lang.Text("Common.Action.Close");
        return Lang.Number(Math.Round(tick / 10d + 0.1d, 1), "N1") + "x";
    }

    #region 停止使用 PCL CE

    [Flags]
    private enum StorageCategory
    {
        None = 0,
        Settings = 1,
        Cache = 2,
        Java = 4,
        Minecraft = 8
    }

    private async Task ClearSelectedStorageAsync()
    {
        if (_storageDialogOpen) return;
        _storageDialogOpen = true;
        BtnStorageClear.IsEnabled = false;
        try
        {
            var selected = GetSelectedStorageCategories();
            if (selected == StorageCategory.None)
            {
                HintService.Show(Hints.Info(Lang.Text("Setup.Misc.Storage.NothingSelected")));
                return;
            }

            if (selected.HasFlag(StorageCategory.Minecraft)
                && GameWatcher.hasRunningMinecraft)
            {
                HintService.Show(Hints.Info(Lang.Text("Setup.Misc.Storage.CloseMinecraft")));
                return;
            }

            var confirmed = await DialogService.Instance.ShowAsync(
                new TextDialogRequest
                {
                    Title = Lang.Text("Setup.Misc.Storage.Confirm.Title"),
                    Content = Lang.Text("Setup.Misc.Storage.Confirm.Message"),
                    Buttons = DialogButtons.ContinueCancel,
                    IsWarn = true
                });
            if (!confirmed.IsConfirmed) return;

            if (selected.HasFlag(StorageCategory.Minecraft))
            {
                var finalConfirm = await DialogService.Instance.ShowAsync(
                    new TextDialogRequest
                    {
                        Title = Lang.Text("Setup.Misc.Storage.MinecraftConfirm.Title"),
                        Content = Lang.Text("Setup.Misc.Storage.MinecraftConfirm.Message"),
                        Buttons = DialogButtons.ContinueCancel,
                        IsWarn = true
                    });
                if (!finalConfirm.IsConfirmed) return;
            }

            var folders = GetStorageCleanupPaths(selected);
            if (folders.Count == 0)
            {
                HintService.Show(Hints.Info(Lang.Text("Setup.Misc.Storage.NothingFound")));
                return;
            }

            ApplicationRuntime.Current.Platform.ProcessCleanup
                .DeleteFoldersAndRestart(folders);
            HintService.Show(Hints.Success(Lang.Text("Setup.Misc.Storage.Cleared")));
            await Task.Delay(650);
            UiInteraction.Shutdown();
        }
        catch (Exception ex)
        {
            LauncherCommon.Log(
                ex,
                "清理启动器数据失败",
                LauncherLogLevel.Msgbox,
                userSummary: Lang.Text("Setup.Error.OperationFailed"));
        }
        finally
        {
            _storageDialogOpen = false;
            BtnStorageClear.IsEnabled = true;
        }
    }

    private StorageCategory GetSelectedStorageCategories()
    {
        var selected = StorageCategory.None;
        if (CheckStorageSettings.Checked == true) selected |= StorageCategory.Settings;
        if (CheckStorageCache.Checked == true) selected |= StorageCategory.Cache;
        if (CheckStorageJava.Checked == true) selected |= StorageCategory.Java;
        if (CheckStorageMinecraft.Checked == true) selected |= StorageCategory.Minecraft;
        return selected;
    }

    private static List<string> GetStorageCleanupPaths(StorageCategory selected)
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var centralRoot = Path.GetFullPath(Paths.CentralRoot);
        var paths = new List<string>();

        void Add(string? path, bool mayDeleteCentralRoot = false)
        {
            if (string.IsNullOrWhiteSpace(path)) return;
            string fullPath;
            try
            {
                fullPath = Path.GetFullPath(Paths.ExpandUserPath(path));
            }
            catch
            {
                return;
            }

            var comparison = OperatingSystem.IsMacOS()
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal;
            if (string.Equals(fullPath, Path.GetPathRoot(fullPath), comparison)
                || string.Equals(fullPath, home, comparison)
                || (!mayDeleteCentralRoot
                    && string.Equals(
                        Path.TrimEndingDirectorySeparator(fullPath),
                        Path.TrimEndingDirectorySeparator(centralRoot),
                        comparison)))
                return;
            if (!paths.Contains(fullPath, StringComparer.FromComparison(comparison)))
                paths.Add(fullPath);
        }

        if (selected.HasFlag(StorageCategory.Settings))
        {
            Add(Paths.Data);
            Add(Paths.SharedData);
            Add(Paths.OldSharedData);
            Add(Path.Combine(appData, "PCLCE"));
            Add(Path.Combine(home, "PCLCE", "PCL"));
        }

        if (selected.HasFlag(StorageCategory.Cache))
        {
            Add(Paths.SharedLocalData);
            Add(Paths.Temp);
            Add(Path.Combine(localAppData, "PCLCE"));
        }

        if (selected.HasFlag(StorageCategory.Java))
        {
            var configuredJava = Environment.GetEnvironmentVariable("PCL_JAVA_DIR");
            Add(string.IsNullOrWhiteSpace(configuredJava)
                ? Path.Combine(Paths.SharedLocalData, "Java")
                : configuredJava);
            Add(Path.Combine(appData, ".minecraft", "runtime"));
        }

        if (selected.HasFlag(StorageCategory.Minecraft))
        {
            Add(LauncherPaths.DefaultMinecraftDirectory, true);
            Add(GameFolders.mcFolderSelected, true);
            Add(Path.Combine(home, "PCLCE", ".minecraft"), true);
            if (!string.IsNullOrWhiteSpace(States.Game.Folders))
                foreach (var folder in States.Game.Folders.Split('|'))
                {
                    var parts = folder.Split('>');
                    if (parts.Length >= 2) Add(parts[1], true);
                }
        }

        return paths;
    }

    #endregion
}
