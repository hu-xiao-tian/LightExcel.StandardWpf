using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using LightExcel.Controls;
using LightExcel.Memory;
using LightExcel.Models;
using LightExcel.StandardWpf.Models;
using LightExcel.StandardWpf.ViewModels;
using CoreSheet = LightExcel.Engine.SpreadsheetVisual;
using StandardSheet = LightExcel.Controls.SpreadsheetVisual;

namespace LightExcel.StandardWpf.Views.Pages;

/// <summary>
/// 订单表格测试页（原主窗口内容整体搬过来）。
/// 页面自身只关心表格：列样式、筛选状态提示、错误提示。
/// </summary>
public partial class OrdersPage : UserControl, ITestPage
{
    private readonly DependencyPropertyDescriptor _filteringDescriptor;
    private readonly DependencyPropertyDescriptor _sourceDescriptor;
    private bool _disposed;

    public OrdersPage(
        OrdersPageViewModel viewModel,
        string title = "订单表格 · 三级表头",
        string description = "20 列三级表头 · 排序 / 筛选 / 合并 / 导入导出 · 100 万行上限",
        string glyph = "▦",
        bool persistLayout = true)
    {
        // 先挂 DataContext，避免 InitializeComponent 时绑定报错。
        DataContext = viewModel;
        InitializeComponent();

        Title = title;
        Description = description;
        Glyph = glyph;
        // 同一套页面代码复用两次时，列布局缓存按控件名存储会互相覆盖，
        // 第二个实例关掉持久化，各自用默认列宽。
        Sheet.IsLayoutPersistenceEnabled = persistLayout;

        _filteringDescriptor = DependencyPropertyDescriptor.FromProperty(
            CoreSheet.IsFilteringProperty, typeof(CoreSheet));
        _sourceDescriptor = DependencyPropertyDescriptor.FromProperty(
            StandardSheet.ItemsSourceProperty, typeof(StandardSheet));

        // 方式 3（后台 code-behind）：直接用字段名或中文列标题，支持 WhenValue / When(Lambda)
        Sheet.ColumnStyles.ForColumn("生产厂区")
            .When<CorrugatedOrder>(o => o.Plant is "深圳厂" or "杭州厂", "#38BDF8");

        _filteringDescriptor.AddValueChanged(Sheet.Engine, OnFilteringChanged);
        _sourceDescriptor.AddValueChanged(Sheet, OnItemsSourceChanged);
        Sheet.Engine.ColumnOperationFailed += ShowQueryError;
        Sheet.FilterFailed += ShowQueryError;
    }

    public string Title { get; }

    public string Description { get; }

    public string Glyph { get; }

    private void OnItemsSourceChanged(object? sender, EventArgs e)
        => QueryStatus.Text = "";

    private void OnFilteringChanged(object? sender, EventArgs e)
    {
        if (Sheet.Engine.IsFiltering)
            QueryStatus.Text = "正在处理排序或筛选…";
        else if (Sheet.FilterError is { } error)
            ShowQueryError(error);
        else
            QueryStatus.Text = $"当前显示 {Sheet.DataRowCount:N0} 行";
    }

    private void ShowQueryError(Exception error)
    {
        QueryStatus.Text = error is EngineMemoryBudgetExceededException
            ? "查询超出引擎内存预算，保留上次结果。可减少排序列后重试。"
            : $"查询失败：{error.Message}";
    }

    private async void ClearFilter_Click(object sender, RoutedEventArgs e)
    {
        await Sheet.ClearFilterAsync();
        QueryStatus.Text = $"已清空筛选，当前显示 {Sheet.DataRowCount:N0} 行";
    }

    private void OpenColumnSettings_Click(object sender, RoutedEventArgs e)
    {
        if (!Sheet.IsLayoutPersistenceEnabled || !Sheet.CanUserHideColumns)
        {
            QueryStatus.Text = "请先勾选“列设置菜单”并启用布局记忆后再打开列设置。";
            return;
        }
        Sheet.OpenColumnSettings();
    }

    private void ThemeToggle_Changed(object sender, RoutedEventArgs e)
    {
        if (Sheet is null) return;
        Sheet.Theme = LightThemeToggle.IsChecked == true ? SpreadsheetTheme.Light : SpreadsheetTheme.Dark;
    }

    private void FrozenCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (Sheet is null || FrozenCombo.SelectedIndex < 0) return;
        Sheet.FrozenColumnCount = FrozenCombo.SelectedIndex;
    }

    private void ApplyComputedMerge_Click(object sender, RoutedEventArgs e)
    {
        if (Sheet.DataRowCount == 0)
        {
            QueryStatus.Text = "请先加载数据库订单后再演示代码合并。";
            return;
        }
        Sheet.ConfigureCellMerges<CorrugatedOrder>(m => m
            // 1. 按同「优先级」相邻合并：同时展示原值合并、自定义单数统计（Compute）与展开长/宽实时求和（Sum）
            .WhenSame(o => o.Priority, g => g
                .Merge("Priority")
                .Merge("OrderStatus").Compute(rows => $"同优先级 {rows.Count} 单")
                .Merge("DevelopL").Sum(o => (decimal)o.DevelopL, format: "合计 #,##0")
                .Merge("DevelopW").Sum(o => (decimal)o.DevelopW, format: "合计 #,##0"))
            // 2. 按同「瓦楞层数」相邻合并：展示箱长均值（Average）
            .WhenSame(o => o.Layers, g => g
                .Merge("Layers")
                .Merge("BoxL").Average(o => (decimal)o.BoxL, format: "均 #0")));
        QueryStatus.Text = "已应用代码流式合并：同优先级自动合并并求和展开长/宽（Sum）+ 统计单数（Compute），同瓦楞层数自动求箱长均值（Average）。";
    }

    private void ApplyGridMerge_Click(object sender, RoutedEventArgs e)
    {
        if (Sheet.DataRowCount == 0)
        {
            QueryStatus.Text = "请先加载数据库订单后再演示二维 Grid 合并。";
            return;
        }
        Sheet.ConfigureCellMerges<CorrugatedOrder>(m => m
            // 演示跨多行 × 多列的定点大块合并（高优占坑）
            .Merge(startRow: 0, rowCount: 3, "CustCode", "CustName")
            .Merge(startRow: 4, rowCount: 2, "Product", "BoxType")
            // 演示左右两屏连续同值二维矩形 Grid 合并（自动避让高优块、杜绝梯形/L 形）
            .Grid("Salesman", "OrderStatus", "Plant", "Priority")
                .Direction(SheetMergeDirection.HorizontalFirst)
            .Grid("BoxType", "Layers", "Flute")
                .Direction(SheetMergeDirection.HorizontalFirst));
        QueryStatus.Text = "已应用二维 Grid 合并：前几行展示跨列大块合并，业务员/状态/厂区/优先级与箱型/层数/楞型自动执行无梯形矩形合并。";
    }

    private void ClearConfiguredMerge_Click(object sender, RoutedEventArgs e)
    {
        Sheet.ClearConfiguredCellMerges();
        QueryStatus.Text = "已清除代码合并规则。";
    }

    /// <summary>窗口关闭时由外壳统一调用：解绑控件事件并释放 ViewModel。</summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _filteringDescriptor.RemoveValueChanged(Sheet.Engine, OnFilteringChanged);
        _sourceDescriptor.RemoveValueChanged(Sheet, OnItemsSourceChanged);
        Sheet.Engine.ColumnOperationFailed -= ShowQueryError;
        Sheet.FilterFailed -= ShowQueryError;

        (DataContext as OrdersPageViewModel)?.Dispose();
    }
}
