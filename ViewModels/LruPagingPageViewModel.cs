using System.Windows.Input;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LightExcel.Memory;
using LightExcel.Models;
using LightExcel.StandardWpf.Services;

namespace LightExcel.StandardWpf.ViewModels;

/// <summary>下拉选项通用键值项。</summary>
public sealed record LruOptionItem(string Label, int Value)
{
    public override string ToString() => Label;
}

/// <summary>
/// “LRU 分页 · 千万行”页面的 ViewModel（纯 MVVM）：
/// 通过 <see cref="PagedSource"/> 与 <see cref="CurrentFirstRow"/> 直接与 XAML 中的
/// <c>SpreadsheetVisual.DataSource</c> / <c>FirstVisibleRow</c> 双向绑定，View 层零胶水代码。
/// </summary>
public sealed class LruPagingPageViewModel : ObservableObject, IDisposable
{
    public const int PageRows = 128;

    private readonly Random _rng = new();
    private readonly DispatcherTimer _statsTimer;
    private VirtualOrderPageProvider? _provider;
    private PagedSheetDataSource? _pagedSource;
    private CancellationTokenSource? _sweepCts;
    private LruOptionItem _selectedRowScale;
    private LruOptionItem _selectedMaxPages;
    private LruOptionItem _selectedDelay;
    private int _currentFirstRow;
    private bool _disposed;

    private string _totalRowsText = "10,000,000 行";
    private string _residentPagesText = "0 / 8 页";
    private string _peakPagesText = " (峰值 0)";
    private string _loadedPagesText = "0";
    private string _evictedPagesText = "0";
    private string _inFlightText = "0 / 0";
    private string _residentBytesText = "0 KB";
    private string _backendReadsText = "0 次";
    private string _recentPagesText = "等待加载...";
    private string _actionHint = "就绪";

    public LruPagingPageViewModel()
    {
        RowScaleOptions =
        [
            new("100 万行", 1_000_000),
            new("1,000 万行", 10_000_000),
            new("1 亿行", 100_000_000)
        ];
        MaxPagesOptions =
        [
            new("4 页 (512行)", 4),
            new("8 页 (1024行)", 8),
            new("16 页 (2048行)", 16),
            new("32 页 (4096行)", 32)
        ];
        DelayOptions =
        [
            new("0 ms (瞬时)", 0),
            new("15 ms (SSD)", 15),
            new("80 ms (远程库)", 80)
        ];

        _selectedRowScale = RowScaleOptions[1];
        _selectedMaxPages = MaxPagesOptions[1];
        _selectedDelay = DelayOptions[1];

        RebuildCommand = new RelayCommand(() => RebuildSession(resetScroll: true));
        JumpFirstCommand = new RelayCommand(() => JumpToRow(0, "第 1 行"));
        Jump500KCommand = new RelayCommand(() => JumpToRow(499_999, "第 50 万行"));
        JumpMiddleCommand = new RelayCommand(() => JumpToRow(SelectedRowScale.Value / 2, "中点行"));
        JumpEndCommand = new RelayCommand(() => JumpToRow(Math.Max(0, SelectedRowScale.Value - 30), "末尾页"));
        JumpRandomCommand = new RelayCommand(JumpRandom);
        Sweep20PagesCommand = new AsyncRelayCommand(Sweep20PagesAsync);

        RebuildSession(resetScroll: true);

        _statsTimer = new DispatcherTimer(TimeSpan.FromMilliseconds(150), DispatcherPriority.Background,
            (_, _) => RefreshTelemetry(), Dispatcher.CurrentDispatcher);
        _statsTimer.Start();
    }

    public IReadOnlyList<LruOptionItem> RowScaleOptions { get; }
    public IReadOnlyList<LruOptionItem> MaxPagesOptions { get; }
    public IReadOnlyList<LruOptionItem> DelayOptions { get; }

    public LruOptionItem SelectedRowScale
    {
        get => _selectedRowScale;
        set
        {
            if (value is null || !SetProperty(ref _selectedRowScale, value)) return;
            RebuildSession(resetScroll: true);
        }
    }

    public LruOptionItem SelectedMaxPages
    {
        get => _selectedMaxPages;
        set
        {
            if (value is null || !SetProperty(ref _selectedMaxPages, value)) return;
            RebuildSession(resetScroll: true);
        }
    }

    public LruOptionItem SelectedDelay
    {
        get => _selectedDelay;
        set
        {
            if (value is null || !SetProperty(ref _selectedDelay, value)) return;
            if (_provider is not null) _provider.SimulatedDelayMs = value.Value;
        }
    }

    /// <summary>直接绑定到 <c>SpreadsheetVisual.DataSource</c>。</summary>
    public PagedSheetDataSource? PagedSource
    {
        get => _pagedSource;
        private set => SetProperty(ref _pagedSource, value);
    }

    /// <summary>双向绑定到 <c>SpreadsheetVisual.FirstVisibleRow</c>（赋值即跳转，滚动自动回写）。</summary>
    public int CurrentFirstRow
    {
        get => _currentFirstRow;
        set
        {
            int clamped = Math.Max(0, value);
            if (!SetProperty(ref _currentFirstRow, clamped)) return;
            RefreshTelemetry();
        }
    }

    public string TotalRowsText { get => _totalRowsText; private set => SetProperty(ref _totalRowsText, value); }
    public string ResidentPagesText { get => _residentPagesText; private set => SetProperty(ref _residentPagesText, value); }
    public string PeakPagesText { get => _peakPagesText; private set => SetProperty(ref _peakPagesText, value); }
    public string LoadedPagesText { get => _loadedPagesText; private set => SetProperty(ref _loadedPagesText, value); }
    public string EvictedPagesText { get => _evictedPagesText; private set => SetProperty(ref _evictedPagesText, value); }
    public string InFlightText { get => _inFlightText; private set => SetProperty(ref _inFlightText, value); }
    public string ResidentBytesText { get => _residentBytesText; private set => SetProperty(ref _residentBytesText, value); }
    public string BackendReadsText { get => _backendReadsText; private set => SetProperty(ref _backendReadsText, value); }
    public string RecentPagesText { get => _recentPagesText; private set => SetProperty(ref _recentPagesText, value); }
    public string ActionHint { get => _actionHint; private set => SetProperty(ref _actionHint, value); }

    public ICommand RebuildCommand { get; }
    public ICommand JumpFirstCommand { get; }
    public ICommand Jump500KCommand { get; }
    public ICommand JumpMiddleCommand { get; }
    public ICommand JumpEndCommand { get; }
    public ICommand JumpRandomCommand { get; }
    public IAsyncRelayCommand Sweep20PagesCommand { get; }

    private void RebuildSession(bool resetScroll)
    {
        if (_disposed) return;
        _sweepCts?.Cancel();
        _sweepCts?.Dispose();
        _sweepCts = null;

        var old = _pagedSource;
        int rows = SelectedRowScale.Value;
        int maxPages = SelectedMaxPages.Value;
        int delayMs = SelectedDelay.Value;

        _provider = new VirtualOrderPageProvider(rows, PageRows, delayMs);
        long byteBudget = checked((long)maxPages * 256L * 1024L);
        var options = new PagedSheetOptions(
            PageRows: PageRows,
            MaximumResidentPages: maxPages,
            EstimatedByteBudget: byteBudget,
            MaximumInFlight: 2,
            MaximumTextLength: 512,
            HeaderByteBudget: 64 * 1024);

        var next = new PagedSheetDataSource(_provider, options, EngineMemoryBudget.Shared);
        if (resetScroll) CurrentFirstRow = 0;

        PagedSource = next;
        old?.Dispose();

        ActionHint = $"已创建 {rows:N0} 行 LRU 会话（每页 {PageRows} 行，最多常驻 {maxPages} 页）";
        RefreshTelemetry();
    }

    private void RefreshTelemetry()
    {
        if (_pagedSource is null || _provider is null) return;

        var stats = _pagedSource.Statistics;
        int currentPage = _currentFirstRow / PageRows;

        TotalRowsText = $"{_pagedSource.RowCount:N0} 行 (当前第 {currentPage:N0} 页)";
        ResidentPagesText = $"{stats.ResidentPages} / {SelectedMaxPages.Value} 页";
        PeakPagesText = $" (峰值 {stats.PeakResidentPages})";
        LoadedPagesText = $"{stats.LoadedPages:N0}";
        EvictedPagesText = $"{stats.DiscardedPages:N0}";
        InFlightText = $"{stats.InFlight} / {stats.CancellationErrors:N0}";
        ResidentBytesText = FormatBytes(stats.EstimatedResidentBytes);
        BackendReadsText = $"{_provider.BackendReadCount:N0} 次";
        RecentPagesText = _provider.GetRecentPagesText();
    }

    private void JumpToRow(int row, string label)
    {
        if (_pagedSource is null) return;
        int clamped = Math.Clamp(row, 0, Math.Max(0, _pagedSource.RowCount - 1));
        CurrentFirstRow = clamped;
        ActionHint = $"已跳转至 {label}（第 {clamped + 1:N0} 行 / 第 {clamped / PageRows:N0} 页）";
        RefreshTelemetry();
    }

    private void JumpRandom()
    {
        int totalPages = Math.Max(1, SelectedRowScale.Value / PageRows);
        int page = _rng.Next(0, totalPages);
        JumpToRow(page * PageRows, $"随机页 #{page:N0}");
    }

    private async Task Sweep20PagesAsync()
    {
        if (_pagedSource is null) return;
        _sweepCts?.Cancel();
        _sweepCts?.Dispose();
        var cts = _sweepCts = new CancellationTokenSource();
        try
        {
            int totalPages = Math.Max(20, SelectedRowScale.Value / PageRows);
            int step = Math.Max(2, totalPages / 21);
            for (int i = 1; i <= 20; i++)
            {
                cts.Token.ThrowIfCancellationRequested();
                int targetPage = Math.Min(totalPages - 1, i * step);
                CurrentFirstRow = targetPage * PageRows;
                ActionHint = $"LRU 压测跳页 {i}/20：第 {targetPage:N0} 页（观察“LRU 已淘汰冷页”递增、常驻页封顶）";
                await _pagedSource.WhenIdle.WaitAsync(cts.Token);
                RefreshTelemetry();
                await Task.Delay(90, cts.Token);
            }
            ActionHint = $"连跳 20 页完成！常驻页稳定封顶在 {SelectedMaxPages.Value} 页，已淘汰旧页详见上方仪表盘。";
        }
        catch (OperationCanceledException)
        {
            ActionHint = "连跳演示已取消。";
        }
        finally
        {
            RefreshTelemetry();
        }
    }

    private static string FormatBytes(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
        return $"{bytes / (1024.0 * 1024.0):F2} MB";
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _statsTimer.Stop();
        _sweepCts?.Cancel();
        _sweepCts?.Dispose();
        _pagedSource?.Dispose();
        _pagedSource = null;
    }
}
