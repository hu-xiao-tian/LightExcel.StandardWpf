using System.Windows;
using LightExcel.StandardWpf.Views.Pages;

namespace LightExcel.StandardWpf.ViewModels;

/// <summary>
/// 导航项：把页面契约包装成导航栏可直接绑定的数据。
/// 页面本身就是 UserControl，所以 Content 直接持有实例（不是模板），切换只换引用。
/// </summary>
public sealed class TestPageItem
{
    public TestPageItem(ITestPage page)
    {
        ArgumentNullException.ThrowIfNull(page);

        if (page is not FrameworkElement view)
            throw new ArgumentException(
                "测试页面必须是 UserControl 等 FrameworkElement。", nameof(page));

        Page = page;
        Content = view;
    }

    public ITestPage Page { get; }

    public FrameworkElement Content { get; }

    public string Title => Page.Title;

    public string Description => Page.Description;

    public string Glyph => Page.Glyph;

    public override string ToString() => Title;
}
