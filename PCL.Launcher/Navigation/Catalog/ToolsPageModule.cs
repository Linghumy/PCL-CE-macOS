using Microsoft.Extensions.DependencyInjection;
using PCL.Core.App;
using PCL.Views.Pages.Tools;
using static PCL.Navigation.PageDescriptorFactory;

namespace PCL.Navigation.Catalog;

public sealed class ToolsPageModule : IPageModule
{
    private static SubPageDescriptor[] ToolsSubPages { get; } =
    [
        Sub(
            PageIds.Tools,
            SubPageIds.ToolsTest,
            "Tools.Left.Utilities.Title",
            "lucide/tool-case",
            "utilities",
            0,
            (sp, _) => ActivatorUtilities.CreateInstance<PageToolsTest>(sp),
            () => !Config.Preference.Hide.ToolsTest)
    ];

    public IReadOnlyList<PageDescriptor> Pages { get; } =
    [
        new(
            PageIds.Tools,
            "Tools.Left.Utilities.Title",
            "lucide/wrench",
            MainTab.Tools,
            150d,
            SubPageIds.ToolsTest,
            [
                new PageGroupDescriptor("utilities", "Tools.Left.Utilities", 0)
            ],
            ToolsSubPages,
            (sp, descriptor) => ActivatorUtilities.CreateInstance<ToolsPageHost>(sp, descriptor))
    ];

    public IReadOnlyList<MainTabDescriptor> MainTabs { get; } =
    [
        new(
            MainTab.Tools,
            PageIds.Tools,
            "Main.Tab.Tools",
            "lucide/wrench",
            3)
    ];
}
