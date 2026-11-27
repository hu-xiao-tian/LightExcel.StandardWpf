using System.Windows.Controls;
using LightExcel.StandardWpf.ViewModels;

namespace LightExcel.StandardWpf.Views.Pages;

/// <summary>
/// “LRU 分页 · 千万行”演示页（纯 MVVM View，零业务与控件胶水代码）。
/// </summary>
public partial class LruPagingPage : UserControl, ITestPage
{
    private readonly LruPagingPageViewModel _viewModel;

    public string Title => "LRU 分页 · 千万行";
    public string Description => "PagedSheetDataSource · 双向链表 LRU 常驻页缓存 · 100万~1亿行恒定低内存";
    public string Glyph => "▤";

    public LruPagingPage(LruPagingPageViewModel viewModel)
    {
        _viewModel = viewModel;
        DataContext = viewModel;
        InitializeComponent();
    }

    public void Dispose() => _viewModel.Dispose();
}
