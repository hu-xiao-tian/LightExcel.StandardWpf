using System.Windows.Controls;
using LightExcel.StandardWpf.ViewModels;

namespace LightExcel.StandardWpf.Views.Pages;

/// <summary>
/// “星号列宽 · 格内控件 · 合并”综合演示页（纯 MVVM View，零业务与事件代码）。
/// </summary>
public partial class FeatureShowcasePage : UserControl, ITestPage
{
    private readonly FeatureShowcasePageViewModel _viewModel;

    public string Title => "星号列宽 · 格内控件 · 合并";
    public string Description => "纯 MVVM · Width=\"*\" 自适应无多余滚动条 · 禁止选中 · 7 种零开销格内控件 · 自动/代码/二维合并";
    public string Glyph => "✦";

    public FeatureShowcasePage(FeatureShowcasePageViewModel viewModel)
    {
        _viewModel = viewModel;
        DataContext = viewModel;
        InitializeComponent();
    }

    public void Dispose() => _viewModel.Dispose();
}
