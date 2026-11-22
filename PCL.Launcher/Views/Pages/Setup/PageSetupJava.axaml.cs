using Avalonia.Controls;
using Avalonia.Media;
using PCL.Core.App;
using PCL.Core.App.Localization;
using PCL.Core.Minecraft.Java;
using PCL.Core.UI;
using PCL.Core.UI.Dialogs;
using PCL.Core.UI.Hints;
using PCL.Navigation;
using PCL.UI.Controls;

namespace PCL.Views.Pages.Setup;

/// <summary>
/// Java 设置页：列出本地 Java、启用/禁用、选择全局 Java 与手动添加。 列表操作由
/// <see cref="PageSetupJavaViewModel" /> 负责，页面负责列表控件构建与滚动位置保持。
/// </summary>
public partial class PageSetupJava : MyPageRight, IRefreshable
{
    private readonly Loading.LoaderTask<bool, List<JavaEntry>> _loader;
    private bool _loaded;
    private bool _scanning;

    public PageSetupJava(PageSetupJavaViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
        PanScroll = PanBack;

        _loader = new Loading.LoaderTask<bool, List<JavaEntry>>("JavaPageLoader", LoadJavaList);
        BtnAdd.Click += async (_, _) => await AddJavaAsync();

        Loaded += (_, _) =>
        {
            if (_loaded) return;
            _loaded = true;
            PageLoaderInit(
                PanLoad,
                CardLoad,
                PanMain,
                null,
                _loader,
                _ => OnLoadFinished());
        };
    }

    public PageSetupJavaViewModel ViewModel { get; }

    public void Refresh()
    {
        PageLoaderRestart();
    }

    public override void OnNavigatedTo(PageParameter? parameter)
    {
        if (!_loaded) return;
        PageLoaderRestart(null, false);
    }

    public override void OnNavigatedFrom()
    {
    }

    private void LoadJavaList(Loading.LoaderTask<bool, List<JavaEntry>> task)
    {
        if ((task.input || task.isForceRestarting) && !_scanning)
        {
            _scanning = true;
            _ = ScanThenRefreshAsync();
        }

        task.output = JavaRuntime.Javas.GetSortedJavaList();
    }

    private async Task ScanThenRefreshAsync()
    {
        try
        {
            await ViewModel.ScanJavaAsync();
            LauncherCommon.RunInUi(OnLoadFinished);
        }
        finally
        {
            _scanning = false;
        }
    }

    private void OnLoadFinished()
    {
        PanContent.Children.Clear();

        var autoItem = new MyListItem
        {
            Type = MyListItem.CheckType.RadioBox
        };
        autoItem.SetLocalizedValue(
            MyListItem.TitleProperty,
            "Setup.Java.AutoSelect.Title");
        autoItem.SetLocalizedValue(
            MyListItem.InfoProperty,
            "Setup.Java.AutoSelect.Info");
        autoItem.Check += (_, _) =>
        {
            if (autoItem.Checked)
                Config.Launch.SelectedJava = "";
        };
        PanContent.Children.Add(autoItem);

        var currentSetJava = Config.Launch.SelectedJava;
        foreach (var entry in JavaRuntime.Javas.GetSortedJavaList())
        {
            var item = BuildItem(entry);
            PanContent.Children.Add(item);
            if (entry.Installation.JavaExePath == currentSetJava)
                item.SetChecked(true, false, false);
        }

        if (string.IsNullOrEmpty(currentSetJava))
            autoItem.SetChecked(true, false, false);
    }

    private MyListItem BuildItem(JavaEntry entry)
    {
        var item = new MyListItem
        {
            Title = PageSetupJavaViewModel.BuildTitle(entry),
            Info = PageSetupJavaViewModel.BuildInfo(entry),
            PlainTags = PageSetupJavaViewModel.BuildTags(entry),
            Type = MyListItem.CheckType.RadioBox
        };
        item.Check += (_, args) =>
        {
            if (!ViewModel.TrySelectJava(entry))
                args.handled = true;
        };

        var btnOpenFolder = new MyIconButton
        {
            SvgIcon = "lucide/folder-open"
        };
        btnOpenFolder.SetLocalizedValue(ToolTip.TipProperty, "Common.Action.Open");
        btnOpenFolder.Click += (_, _) =>
        {
            if (!PageSetupJavaViewModel.EnsureAvailable(entry)) return;
            Basics.OpenPath(entry.Installation.JavaFolder);
        };

        var btnInfo = new MyIconButton
        {
            SvgIcon = "lucide/info"
        };
        btnInfo.SetLocalizedValue(ToolTip.TipProperty, "Setup.Java.Detail.ToolTip");
        btnInfo.Click += async (_, _) =>
        {
            // 防止重复点击打开多个详情弹窗。
            if (!btnInfo.IsEnabled) return;
            if (!PageSetupJavaViewModel.EnsureAvailable(entry)) return;
            btnInfo.IsEnabled = false;
            try
            {
                await DialogService.Instance.ShowAsync(
                    new TextDialogRequest
                    {
                        Title = Lang.Text("Setup.Java.Info.Title"),
                        Content = PageSetupJavaViewModel.BuildInfoMessage(entry),
                        Buttons = DialogButtons.Ok
                    });
            }
            catch (Exception ex)
            {
                LauncherCommon.Log(
                    ex,
                    Lang.Text("Setup.Error.OperationFailed"),
                    LauncherLogLevel.Feedback,
                    userSummary: Lang.Text("Setup.Error.OperationFailed"));
            }
            finally
            {
                btnInfo.IsEnabled = true;
            }
        };

        var btnEnableSwitch = new MyIconButton();
        var buttons = new List<MyIconButton>
        {
            btnOpenFolder,
            btnInfo
        };

        if (entry.Source == JavaSource.AutoInstalled)
        {
            var btnDelete = new MyIconButton
            {
                SvgIcon = "lucide/trash-2"
            };
            btnDelete.SetLocalizedValue(
                ToolTip.TipProperty,
                "Setup.Java.Delete.ToolTip");
            btnDelete.Click += async (_, _) =>
                await DeleteManagedJavaAsync(entry);
            buttons.Add(btnDelete);
        }

        buttons.Add(btnEnableSwitch);
        item.Buttons = buttons;

        btnEnableSwitch.Click += (_, _) =>
        {
            var enabled = ViewModel.ToggleJavaEnabled(entry);
            if (enabled is not null)
            {
                entry.IsEnabled = enabled.Value;
                UpdateEnableStyle(enabled.Value);
            }
        };

        UpdateEnableStyle(entry.IsEnabled);
        return item;

        void UpdateEnableStyle(bool isEnabled)
        {
            if (!entry.Installation.IsStillAvailable)
            {
                btnEnableSwitch.SvgIcon = "lucide/circle-minus";
                btnEnableSwitch.SetLocalizedValue(
                    ToolTip.TipProperty,
                    "Setup.Java.Unavailable");
                return;
            }

            item.LabTitle.TextDecorations = isEnabled ? null : TextDecorations.Strikethrough;
            btnEnableSwitch.SvgIcon = isEnabled ? "lucide/circle-minus" : "lucide/circle-check";
            btnEnableSwitch.SetLocalizedValue(
                ToolTip.TipProperty,
                isEnabled ? "Setup.Java.Disable" : "Setup.Java.Enable");
        }
    }

    private async Task AddJavaAsync()
    {
        BtnAdd.IsEnabled = false;
        try
        {
            var path = await SystemDialogs.SelectFileAsync(
                Lang.Text(
                    "Setup.Java.SelectFile.Filter",
                    JavaExecutableLayout.JavaExecutableName),
                Lang.Text("Setup.Java.SelectFile.Title"));
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return;
            if (JavaRuntime.Javas.Exist(path))
            {
                HintService.Show(Hints.Info(Lang.Text("Setup.Java.AlreadyExists")));
                return;
            }

            if (await ViewModel.AddJavaAsync(path))
            {
                HintService.Show(Hints.Success(Lang.Text("Setup.Java.Added")));
                _loader.Start(true, true);
            }
            else
            {
                HintService.Show(Hints.Error(Lang.Text("Setup.Java.AddFailed")));
            }
        }
        catch (Exception ex)
        {
            LauncherCommon.Log(
                ex,
                Lang.Text("Setup.Java.AddFailed"),
                LauncherLogLevel.Hint,
                userSummary: Lang.Text("Setup.Java.AddFailed"));
        }
        finally
        {
            BtnAdd.IsEnabled = true;
        }
    }

    private async Task DeleteManagedJavaAsync(JavaEntry entry)
    {
        var result = await DialogService.Instance.ShowAsync(
            new TextDialogRequest
            {
                Title = Lang.Text("Setup.Java.Delete.Title"),
                Content = Lang.Text(
                    "Setup.Java.Delete.Message",
                    entry.Installation.Version,
                    entry.Installation.Architecture),
                Buttons = DialogButtons.From(
                    DialogButton.Create(Lang.Text("Common.Action.Delete")),
                    DialogButton.Create(Lang.Text("Common.Action.Cancel"))),
                IsWarn = true
            });
        if (!result.IsConfirmed)
            return;

        try
        {
            if (await ViewModel.RemoveManagedJavaAsync(entry))
            {
                HintService.Show(Hints.Success(Lang.Text("Setup.Java.Delete.Success")));
                _loader.Start(true, true);
            }
            else
            {
                HintService.Show(Hints.Error(Lang.Text("Setup.Java.Delete.Failed")));
            }
        }
        catch (Exception ex)
        {
            LauncherCommon.Log(
                ex,
                Lang.Text("Setup.Java.Delete.Failed"),
                LauncherLogLevel.Hint,
                userSummary: Lang.Text("Setup.Java.Delete.Failed"));
        }
    }
}
