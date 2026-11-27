using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LightExcel.Controls;
using LightExcel.Models;
using LightExcel.StandardWpf.Models;

namespace LightExcel.StandardWpf.ViewModels;

/// <summary>
/// “星号列宽 · 格内控件 · 合并演示”页面 ViewModel（纯 MVVM：零 View 后台事件）。
/// </summary>
public sealed class FeatureShowcasePageViewModel : ObservableObject, IDisposable
{
    private bool _isSelectionEnabled;
    private bool _isReadOnly;
    private bool _canUserCopyPaste;
    private bool _canUserFilterColumns = true;
    private bool _showRowNumbers;
    private bool _isLightTheme;
    private bool _isAutoMergeEnabled;
    private bool _isManualMergeEnabled;
    private object? _summaryItem;
    private string _actionLog =
        "提示：本页采用纯 MVVM 写法，所有列使用星号比例宽度（* / 100*）无多余横向滚动条；可体验禁止选中、7 种格内控件、自动换肤右键菜单，以及自动合并/代码流式求和/二维 Grid 合并。";

    public ObservableCollection<PaperBatchItem> Batches { get; } = [];
    public IReadOnlyList<string> ShiftOptions { get; } = ["早班", "中班", "晚班"];

    public bool IsSelectionEnabled { get => _isSelectionEnabled; set => SetProperty(ref _isSelectionEnabled, value); }
    public bool IsReadOnly { get => _isReadOnly; set => SetProperty(ref _isReadOnly, value); }
    public bool CanUserCopyPaste { get => _canUserCopyPaste; set => SetProperty(ref _canUserCopyPaste, value); }
    public bool CanUserFilterColumns { get => _canUserFilterColumns; set => SetProperty(ref _canUserFilterColumns, value); }
    public bool ShowRowNumbers { get => _showRowNumbers; set => SetProperty(ref _showRowNumbers, value); }

    public bool IsLightTheme
    {
        get => _isLightTheme;
        set
        {
            if (SetProperty(ref _isLightTheme, value))
                OnPropertyChanged(nameof(Theme));
        }
    }

    public SpreadsheetTheme Theme => _isLightTheme ? SpreadsheetTheme.Light : SpreadsheetTheme.Dark;

    public bool IsAutoMergeEnabled { get => _isAutoMergeEnabled; set => SetProperty(ref _isAutoMergeEnabled, value); }
    public bool IsManualMergeEnabled { get => _isManualMergeEnabled; set => SetProperty(ref _isManualMergeEnabled, value); }
    public object? SummaryItem { get => _summaryItem; private set => SetProperty(ref _summaryItem, value); }
    public string ActionLog { get => _actionLog; set => SetProperty(ref _actionLog, value); }

    public ICommand AddRowCommand { get; }
    public ICommand RemoveLastCommand { get; }
    public ICommand RushBatchCommand { get; }
    public ICommand InspectBatchCommand { get; }
    public ICommand PreviewRowCommand { get; }
    public ICommand AddMetersCommand { get; }
    public ICommand DeleteRowCommand { get; }
    public ICommand ApplyComputedMergeCommand { get; }
    public ICommand ApplyWidgetMergeCommand { get; }
    public ICommand ApplyGridMergeCommand { get; }
    public ICommand ClearMergeCommand { get; }

    public FeatureShowcasePageViewModel()
    {
        AddRowCommand = new RelayCommand(AddRow);
        RemoveLastCommand = new RelayCommand(RemoveLast);
        RushBatchCommand = new RelayCommand<PaperBatchItem?>(RushBatch);
        InspectBatchCommand = new RelayCommand<PaperBatchItem?>(InspectBatch);
        PreviewRowCommand = new RelayCommand<object?>(PreviewRow);
        AddMetersCommand = new RelayCommand<object?>(AddMeters);
        DeleteRowCommand = new RelayCommand<object?>(DeleteRow);
        ApplyComputedMergeCommand = new RelayCommand<SpreadsheetVisual?>(ApplyComputedMerge);
        ApplyWidgetMergeCommand = new RelayCommand<SpreadsheetVisual?>(ApplyWidgetMerge);
        ApplyGridMergeCommand = new RelayCommand<SpreadsheetVisual?>(ApplyGridMerge);
        ClearMergeCommand = new RelayCommand<SpreadsheetVisual?>(ClearMerge);

        Batches.CollectionChanged += OnBatchesCollectionChanged;

        SeedInitialBatches();
        RefreshSummary();
    }

    private void SeedInitialBatches()
    {
        Batches.Add(new PaperBatchItem { Serial = 1, FluteCode = "BC", PaperCode = "Q/9/2/9/E", WidthMm = 2950, Meters = 93, Shift = "早班", PlanDate = new DateTime(2026, 11, 27), IsApproved = true, IsEnabled = true, Progress = 85 });
        Batches.Add(new PaperBatchItem { Serial = 2, FluteCode = "BC", PaperCode = "L/9/2/8/E", WidthMm = 2950, Meters = 661, Shift = "早班", PlanDate = new DateTime(2026, 11, 27), IsApproved = true, IsEnabled = true, Progress = 62 });
        Batches.Add(new PaperBatchItem { Serial = 3, FluteCode = "BC", PaperCode = "L/9/2/9/E", WidthMm = 2950, Meters = 804, Shift = "中班", PlanDate = new DateTime(2026, 11, 28), IsApproved = false, IsEnabled = true, Progress = 40 });
        Batches.Add(new PaperBatchItem { Serial = 4, FluteCode = "BC", PaperCode = "L/6/2/9/E", WidthMm = 2950, Meters = 277, Shift = "中班", PlanDate = new DateTime(2026, 11, 28), IsApproved = false, IsEnabled = false, Progress = 100 });
        Batches.Add(new PaperBatchItem { Serial = 5, FluteCode = "BE", PaperCode = "K/7/3/7/K", WidthMm = 2500, Meters = 520, Shift = "晚班", PlanDate = new DateTime(2026, 11, 29), IsApproved = true, IsEnabled = true, Progress = 25 });
        Batches.Add(new PaperBatchItem { Serial = 6, FluteCode = "BE", PaperCode = "K/8/2/8/K", WidthMm = 2500, Meters = 410, Shift = "晚班", PlanDate = new DateTime(2026, 11, 29), IsApproved = true, IsEnabled = true, Progress = 58 });
        Batches.Add(new PaperBatchItem { Serial = 7, FluteCode = "BE", PaperCode = "K/6/2/7/K", WidthMm = 2500, Meters = 345, Shift = "早班", PlanDate = new DateTime(2026, 11, 30), IsApproved = false, IsEnabled = true, Progress = 74 });
        Batches.Add(new PaperBatchItem { Serial = 8, FluteCode = "EB", PaperCode = "A/9/3/9/A", WidthMm = 2200, Meters = 680, Shift = "早班", PlanDate = new DateTime(2026, 11, 30), IsApproved = true, IsEnabled = true, Progress = 92 });
    }

    private void OnBatchesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems is not null)
        {
            foreach (PaperBatchItem item in e.OldItems)
                item.PropertyChanged -= OnBatchPropertyChanged;
        }
        if (e.NewItems is not null)
        {
            foreach (PaperBatchItem item in e.NewItems)
                item.PropertyChanged += OnBatchPropertyChanged;
        }
        RefreshSummary();
    }

    private void OnBatchPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(PaperBatchItem.Meters) or nameof(PaperBatchItem.Progress))
            RefreshSummary();
    }

    private void RefreshSummary()
    {
        if (Batches.Count == 0)
        {
            SummaryItem = null;
            return;
        }
        int totalMeters = Batches.Sum(x => x.Meters);
        double avgProgress = Batches.Average(x => x.Progress);
        SummaryItem = new
        {
            Serial = "合计",
            FluteCode = $"{Batches.Count} 批",
            Meters = $"{totalMeters:N0} m",
            Progress = Math.Round(avgProgress, 1)
        };
    }

    private void AddRow()
    {
        int next = Batches.Count + 1;
        string flute = next % 3 == 0 ? "BE" : "BC";
        Batches.Add(new PaperBatchItem
        {
            Serial = next,
            FluteCode = flute,
            PaperCode = $"L/{6 + next % 4}/2/9/E",
            WidthMm = flute == "BE" ? 2500 : 2950,
            Meters = 120 + next * 35,
            Shift = ShiftOptions[next % ShiftOptions.Count],
            PlanDate = DateTime.Today.AddDays(next % 5),
            IsApproved = next % 2 == 0,
            IsEnabled = true,
            Progress = (next * 17) % 101
        });
        ActionLog = $"[MVVM 命令] 已新增第 {next} 批纸质排程（当前共 {Batches.Count} 批）。";
    }

    private void RemoveLast()
    {
        if (Batches.Count == 0) return;
        var last = Batches[^1];
        Batches.RemoveAt(Batches.Count - 1);
        ActionLog = $"[MVVM 命令] 已删除序号 {last.Serial}（{last.PaperCode}），当前剩余 {Batches.Count} 批。";
    }

    private void RushBatch(PaperBatchItem? batch)
    {
        if (batch is null) return;
        batch.Progress = Math.Min(100, batch.Progress + 15);
        ActionLog = $"[格内按钮 Command] 序号 {batch.Serial}（{batch.PaperCode}）已「加急」→ 完工进度 {batch.Progress:0}%。";
    }

    private void InspectBatch(PaperBatchItem? batch)
    {
        if (batch is null) return;
        ActionLog = $"[格内按钮 Command] 序号 {batch.Serial} | 类别 {batch.FluteCode} | 纸质 {batch.PaperCode} | 纸宽 {batch.WidthMm}mm | 米数 {batch.Meters}m | 班次 {batch.Shift} | 日期 {batch.PlanDate:yyyy-MM-dd}";
    }

    private void PreviewRow(object? item)
    {
        if (item is PaperBatchItem batch)
            ActionLog = $"[右键菜单 Command] 预览排程卡 → 序号 {batch.Serial}（{batch.PaperCode}，{batch.Meters}m）";
    }

    private void AddMeters(object? item)
    {
        if (item is PaperBatchItem batch)
        {
            batch.Meters += 100;
            ActionLog = $"[右键菜单 Command] 序号 {batch.Serial} 快速补米 +100m → 当前 {batch.Meters}m";
        }
    }

    private void DeleteRow(object? item)
    {
        if (item is PaperBatchItem batch)
        {
            Batches.Remove(batch);
            ActionLog = $"[右键菜单 Command] 已删除序号 {batch.Serial}（{batch.PaperCode}）";
        }
    }

    private void ApplyComputedMerge(SpreadsheetVisual? sheet)
    {
        if (sheet is null || Batches.Count == 0) return;
        sheet.ConfigureCellMerges<PaperBatchItem>(m => m
            .WhenSame(x => x.FluteCode, g => g
                .Merge("FluteCode")
                .Merge("WidthMm").Compute(rows => $"同类别 {rows.Count} 批")
                .Merge("Meters").Sum(x => (decimal)x.Meters, format: "合计 #,##0 m")));
        ActionLog = "[代码流式合并] 已按相邻「类别」自动合并，并实时计算批数（Compute）与总米数（Sum）；右侧格内控件仍可正常交互。";
    }

    private void ApplyWidgetMerge(SpreadsheetVisual? sheet)
    {
        if (sheet is null || Batches.Count == 0) return;
        sheet.ConfigureCellMerges<PaperBatchItem>(m => m
            // 演示全部 6 类格内控件列按同「类别」分组合并：
            // 下拉/日期/勾选框/开关同列纵向合并后，点击一次自动批量同步整组所有行；
            // 进度条列按组聚合显示平均进度；操作按钮列同列合并为居中组按钮！
            .WhenSame(x => x.FluteCode, g => g
                .Merge("FluteCode")
                .Merge("Meters").Sum(x => (decimal)x.Meters, format: "合计 #,##0 m")
                .Merge("Shift")
                .Merge("PlanDate")
                .Merge("IsApproved")
                .Merge("IsEnabled")
                .Merge("Progress").Compute(rows => $"均 {rows.Average(x => x.Progress):0}%")
                .Merge("快捷按钮")
                .Merge("自定义模板")));
        ActionLog = "[控件列分组合并] 班次(下拉)/排产日期(日历)/审核(勾选框)/启用(开关)/操作(按钮)已同列纵向合并，点击合并后的控件将自动批量同步整组行值；进度列显示组均值。注：手动跨列框选不同控件列时，引擎会自动拦截非法跨列混并。";
    }

    private void ApplyGridMerge(SpreadsheetVisual? sheet)
    {
        if (sheet is null || Batches.Count == 0) return;
        sheet.ConfigureCellMerges<PaperBatchItem>(m => m
            .Merge(startRow: 0, rowCount: 2, "FluteCode", "PaperCode")
            .Grid("FluteCode", "WidthMm").Direction(SheetMergeDirection.HorizontalFirst)
            // 即便将多个相邻控件列传入 Grid（如审核/启用同为 true），引擎也会自动禁止跨控件列横向混并，只做各自同列纵向合并！
            .Grid("Shift", "PlanDate", "IsApproved", "IsEnabled").Direction(SheetMergeDirection.HorizontalFirst));
        ActionLog = "[二维 Grid 合并] 左侧文本列演示跨列大块与二维矩形合并；右侧「班次/排产日期/审核/启用」即便同值（如均勾选 true），引擎也会自动阻断不同控件列的横向混并，安全执行各自同列纵向合并。";
    }

    private void ClearMerge(SpreadsheetVisual? sheet)
    {
        IsAutoMergeEnabled = false;
        sheet?.ClearConfiguredCellMerges();
        ActionLog = "[合并控制] 已清除全部代码合并与自动合并规则。";
    }

    public void Dispose()
    {
        Batches.CollectionChanged -= OnBatchesCollectionChanged;
        foreach (var item in Batches)
            item.PropertyChanged -= OnBatchPropertyChanged;
    }
}
