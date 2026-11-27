using System.Globalization;
using LightExcel.Models;

namespace LightExcel.StandardWpf.Services;

/// <summary>
/// 面向 <see cref="PagedSheetDataSource"/> 的超大规模按需分页提供者（实现 <see cref="ISheetPageProvider"/>）。
/// 不在内存中持有全量行，只在视口需要某页且 LRU 缓存未命中时异步生成/读取该页（每页默认 128 行），
/// 即使设为 1 亿行，占用内存依然恒定由 LRU 常驻页数上限决定。
/// </summary>
public sealed class VirtualOrderPageProvider : ISheetPageProvider
{
    private static readonly string[] Headers =
    [
        "订单号", "所属页号", "行序号", "订单日期",
        "客户行业", "客户名称", "产品名称", "订单状态",
        "生产厂区", "优先级", "数量", "金额(元)"
    ];

    private static readonly string[] Industries = ["电子制造", "日用化工", "食品饮料", "电商物流", "家用电器", "建材陶瓷", "家具制造", "外贸出口"];
    private static readonly string[] Customers = ["富士康电子", "蓝月亮", "稻香村食品", "拼多多电商仓", "格力电器", "景德镇陶瓷", "顾家家居", "中远海运", "立白日化", "立讯精密"];
    private static readonly string[] Products = ["电子产品箱", "洗护箱", "礼盒外箱", "快递箱", "空调外箱", "陶瓷防震箱", "家具包装箱", "出口重型箱", "洗衣液外箱", "连接器箱"];
    private static readonly string[] Statuses = ["已完成", "生产中", "已排产", "待排产"];
    private static readonly string[] Plants = ["杭州厂", "上海厂", "深圳厂", "广州厂", "北京厂", "嘉兴厂", "顺德厂"];
    private static readonly string[] Priorities = ["低", "普通", "加急"];

    private readonly int _pageRows;
    private readonly object _logGate = new();
    private readonly Queue<int> _recentPages = new();
    private long _backendReadCount;
    private int _simulatedDelayMs;

    public VirtualOrderPageProvider(int rowCount, int pageRows = 128, int simulatedDelayMs = 15)
    {
        RowCount = rowCount;
        _pageRows = pageRows;
        _simulatedDelayMs = Math.Max(0, simulatedDelayMs);
    }

    public int RowCount { get; }
    public int ColumnCount => Headers.Length;

    public int SimulatedDelayMs
    {
        get => Volatile.Read(ref _simulatedDelayMs);
        set => Volatile.Write(ref _simulatedDelayMs, Math.Max(0, value));
    }

    public long BackendReadCount => Interlocked.Read(ref _backendReadCount);

    public string GetColumnHeader(int column) => Headers[column];

    public string GetRecentPagesText()
    {
        lock (_logGate)
        {
            if (_recentPages.Count == 0) return "暂无后端读页";
            return string.Join(" → ", _recentPages.Select(p => $"#{p:N0}"));
        }
    }

    public async ValueTask<SheetPage> ReadPageAsync(SheetPageRequest request, CancellationToken cancellationToken)
    {
        int delay = SimulatedDelayMs;
        if (delay > 0)
            await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();

        int pageIndex = request.FirstRow / _pageRows;
        string pageTag = $"第 {pageIndex:N0} 页";
        int cols = request.ColumnCount;
        var cells = new string[request.RowCount * cols];
        var baseDate = new DateTime(2026, 1, 1);

        for (int r = 0; r < request.RowCount; r++)
        {
            int row = request.FirstRow + r;
            int offset = r * cols;
            int qty = ((row * 37) & 511) + 10;
            decimal amount = qty * (8.5m + (row % 25) * 1.25m);

            cells[offset + 0] = $"LRU{row + 1:D9}";
            cells[offset + 1] = pageTag;
            cells[offset + 2] = (row + 1).ToString("N0", CultureInfo.InvariantCulture);
            cells[offset + 3] = baseDate.AddDays(row % 365).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            cells[offset + 4] = Industries[row % Industries.Length];
            cells[offset + 5] = Customers[(row / 3) % Customers.Length];
            cells[offset + 6] = Products[(row / 5) % Products.Length];
            cells[offset + 7] = Statuses[(row * 7) % Statuses.Length];
            cells[offset + 8] = Plants[(row * 11) % Plants.Length];
            cells[offset + 9] = Priorities[(row * 13) % Priorities.Length];
            cells[offset + 10] = qty.ToString("N0", CultureInfo.InvariantCulture);
            cells[offset + 11] = amount.ToString("N2", CultureInfo.InvariantCulture);
        }

        Interlocked.Increment(ref _backendReadCount);
        lock (_logGate)
        {
            _recentPages.Enqueue(pageIndex);
            while (_recentPages.Count > 12) _recentPages.Dequeue();
        }

        return new SheetPage(request, cells);
    }
}
