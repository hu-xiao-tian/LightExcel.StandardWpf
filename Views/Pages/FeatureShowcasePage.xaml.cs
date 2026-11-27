using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using LightExcel.Controls;
using LightExcel.Models;

namespace LightExcel.StandardWpf.Views.Pages;

public sealed class PaperBatchItem : INotifyPropertyChanged
{
    private int _serial;
    private string _fluteCode = "BC";
    private string _paperCode = "Q/9/2/9/E";
    private int _widthMm = 2950;
    private int _meters = 100;
    private string _shift = "早班";
    private DateTime _planDate = DateTime.Today;
    private bool _isApproved;
    private bool _isEnabled = true;
    private double _progress;

    public int Serial { get => _serial; set { if (_serial != value) { _serial = value; Notify(nameof(Serial)); } } }
    public string FluteCode { get => _fluteCode; set { if (_fluteCode != value) { _fluteCode = value; Notify(nameof(FluteCode)); } } }
    public string PaperCode { get => _paperCode; set { if (_paperCode != value) { _paperCode = value; Notify(nameof(PaperCode)); } } }
    public int WidthMm { get => _widthMm; set { if (_widthMm != value) { _widthMm = value; Notify(nameof(WidthMm)); } } }
    public int Meters { get => _meters; set { if (_meters != value) { _meters = value; Notify(nameof(Meters)); } } }
    public string Shift { get => _shift; set { if (_shift != value) { _shift = value; Notify(nameof(Shift)); } } }
    public DateTime PlanDate { get => _planDate; set { if (_planDate != value) { _planDate = value; Notify(nameof(PlanDate)); } } }
    public bool IsApproved { get => _isApproved; set { if (_isApproved != value) { _isApproved = value; Notify(nameof(IsApproved)); } } }
    public bool IsEnabled { get => _isEnabled; set { if (_isEnabled != value) { _isEnabled = value; Notify(nameof(IsEnabled)); } } }
    public double Progress { get => _progress; set { if (Math.Abs(_progress - value) > 0.001) { _progress = value; Notify(nameof(Progress)); } } }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void Notify(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public partial class FeatureShowcasePage : UserControl, ITestPage
{
    public ObservableCollection<PaperBatchItem> Batches { get; } = [];
    public IReadOnlyList<string> ShiftOptions { get; } = ["早班", "中班", "晚班"];

    public string Title => "星号列宽 · 格内控件";
    public string Description => "Width=\"*\" 自适应无多余横向滚动条 · 禁止选中开关 · 格内按钮 / 下拉 / 日期 / 勾选框 / 开关 / 进度条";
    public string Glyph => "✦";

    public FeatureShowcasePage()
    {
        DataContext = this;
        InitializeComponent();

        Batches.Add(new PaperBatchItem { Serial = 1, FluteCode = "BC", PaperCode = "Q/9/2/9/E", WidthMm = 2950, Meters = 93, Shift = "早班", PlanDate = new DateTime(2026, 11, 27), IsApproved = true, IsEnabled = true, Progress = 85 });
        Batches.Add(new PaperBatchItem { Serial = 2, FluteCode = "BC", PaperCode = "L/9/2/8/E", WidthMm = 2950, Meters = 661, Shift = "中班", PlanDate = new DateTime(2026, 11, 27), IsApproved = true, IsEnabled = true, Progress = 62 });
        Batches.Add(new PaperBatchItem { Serial = 3, FluteCode = "BC", PaperCode = "L/9/2/9/E", WidthMm = 2950, Meters = 804, Shift = "早班", PlanDate = new DateTime(2026, 11, 28), IsApproved = false, IsEnabled = true, Progress = 40 });
        Batches.Add(new PaperBatchItem { Serial = 4, FluteCode = "BC", PaperCode = "L/6/2/9/E", WidthMm = 2950, Meters = 277, Shift = "晚班", PlanDate = new DateTime(2026, 11, 28), IsApproved = false, IsEnabled = false, Progress = 100 });
        Batches.Add(new PaperBatchItem { Serial = 5, FluteCode = "BE", PaperCode = "K/7/3/7/K", WidthMm = 2500, Meters = 520, Shift = "中班", PlanDate = new DateTime(2026, 11, 29), IsApproved = true, IsEnabled = true, Progress = 25 });

        ShowcaseSheet.ItemsSource = Batches;
        Batches.CollectionChanged += (_, _) => UpdateSummary();
        UpdateSummary();
    }

    private void UpdateSummary()
    {
        if (Batches.Count == 0)
        {
            ShowcaseSheet.SummaryItem = null;
            return;
        }
        int totalMeters = Batches.Sum(x => x.Meters);
        double avgProgress = Batches.Average(x => x.Progress);
        ShowcaseSheet.SummaryItem = new
        {
            Serial = "合计",
            FluteCode = $"{Batches.Count} 批",
            Meters = $"{totalMeters:N0} m",
            Progress = Math.Round(avgProgress, 1)
        };
    }

    private void AddRow_Click(object sender, RoutedEventArgs e)
    {
        int next = Batches.Count + 1;
        Batches.Add(new PaperBatchItem
        {
            Serial = next,
            FluteCode = next % 2 == 0 ? "BE" : "BC",
            PaperCode = $"L/{6 + next % 4}/2/9/E",
            WidthMm = 2950,
            Meters = 120 + next * 35,
            Shift = ShiftOptions[next % ShiftOptions.Count],
            PlanDate = DateTime.Today.AddDays(next % 5),
            IsApproved = next % 2 == 0,
            IsEnabled = true,
            Progress = (next * 17) % 101
        });
        ActionLog.Text = $"已新增第 {next} 批纸质排程（当前共 {Batches.Count} 行）。";
    }

    private void RemoveLast_Click(object sender, RoutedEventArgs e)
    {
        if (Batches.Count == 0) return;
        var last = Batches[^1];
        Batches.RemoveAt(Batches.Count - 1);
        ActionLog.Text = $"已删除序号 {last.Serial}（{last.PaperCode}），当前剩余 {Batches.Count} 行。";
    }

    private void ThemeCheck_Changed(object sender, RoutedEventArgs e)
    {
        if (ShowcaseSheet is null) return;
        ShowcaseSheet.Theme = ThemeCheck.IsChecked == true ? SpreadsheetTheme.Light : SpreadsheetTheme.Dark;
    }

    private void ShowcaseSheet_CellButtonClicked(object? sender, SheetCellButtonClickArgs e)
    {
        if (e.RowItem is not PaperBatchItem batch) return;
        if (e.ButtonKey == "Rush")
        {
            batch.Progress = Math.Min(100, batch.Progress + 15);
            UpdateSummary();
            ActionLog.Text = $"[格内按钮] 已对序号 {batch.Serial}（{batch.PaperCode}）执行「加急」→ 进度更新为 {batch.Progress:0}%。";
        }
        else
        {
            ActionLog.Text = $"[格内按钮] 查看详情 → 序号 {batch.Serial} | 类别 {batch.FluteCode} | 纸质 {batch.PaperCode} | 纸宽 {batch.WidthMm}mm | 米数 {batch.Meters}m | 班次 {batch.Shift}";
        }
    }

    private void RowMenu_Preview_Click(object sender, RoutedEventArgs e)
    {
        if (ShowcaseSheet.RightClickedItem is PaperBatchItem batch)
            ActionLog.Text = $"[行右键菜单] 预览排程卡 → 序号 {batch.Serial}（{batch.PaperCode}，{batch.Meters}m）";
    }

    private void RowMenu_AddMeters_Click(object sender, RoutedEventArgs e)
    {
        if (ShowcaseSheet.RightClickedItem is PaperBatchItem batch)
        {
            batch.Meters += 100;
            UpdateSummary();
            ActionLog.Text = $"[行右键菜单] 序号 {batch.Serial} 已快速补米 +100m → 当前 {batch.Meters}m";
        }
    }

    private void RowMenu_Delete_Click(object sender, RoutedEventArgs e)
    {
        if (ShowcaseSheet.RightClickedItem is PaperBatchItem batch)
        {
            Batches.Remove(batch);
            ActionLog.Text = $"[行右键菜单] 已删除序号 {batch.Serial}（{batch.PaperCode}）";
        }
    }

    public void Dispose() { }
}
