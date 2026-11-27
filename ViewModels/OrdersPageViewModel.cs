using System.Collections;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows;
using LightExcel.Models;
using LightExcel.Controls;
using LightExcel.DataExchange;
using LightExcel.StandardWpf.Models;
using LightExcel.StandardWpf.Services;

namespace LightExcel.StandardWpf.ViewModels;

/// <summary>
/// “订单表格”页面的 ViewModel。
/// 负责加载 SQLite 订单、导入导出、选中行读取与汇总行计算。
///
/// 用 CommunityToolkit.Mvvm 的两个基类，不用源生成器：
///   ObservableObject                  → SetProperty / OnPropertyChanged
///   AsyncRelayCommand / RelayCommand  → 现成的 ICommand 实现
///
/// 约定：属性 setter 只做一件事——SetProperty；派生通知和命令刷新放在赋值处。
/// </summary>
public sealed class OrdersPageViewModel : ObservableObject, IDisposable
{
    // 表格在后台调用此方法，rows 是当前筛选结果的全部订单。
    public async Task<OrderSummary?> OnOrdersViewChanged(
        IAsyncEnumerable<CorrugatedOrder> rows, CancellationToken token)
    {
        int count = 0;
        long layers = 0;
        long boxLength = 0;
        long boxWidth = 0;
        long boxHeight = 0;
        long developLength = 0;
        long developWidth = 0;
        double thickness = 0;
        int completed = 0;
        int urgent = 0;
        DateTime? firstOrderDate = null;
        DateTime? lastDeliveryDate = null;

        // 一次遍历同时计算多列，修改业务公式从这里开始。
        await foreach (var row in rows.WithCancellation(token))
        {
            count++;
            layers += row.Layers;
            boxLength += row.BoxL;
            boxWidth += row.BoxW;
            boxHeight += row.BoxH;
            thickness += row.BoardThickness;
            developLength += row.DevelopL;
            developWidth += row.DevelopW;

            if (row.OrderStatus == "已完成")
                completed++;
            if (row.Priority == "加急")
                urgent++;

            if (row.OrderDate != default &&
                (firstOrderDate is null || row.OrderDate < firstOrderDate))
                firstOrderDate = row.OrderDate;
            if (row.DeliveryDate != default &&
                (lastDeliveryDate is null || row.DeliveryDate > lastDeliveryDate))
                lastDeliveryDate = row.DeliveryDate;
        }

        if (count == 0) return null;

        // 赋给哪个字段，结果就显示在哪一列下面。
        return new OrderSummary
        {
            OrderNo = "000000000000000000000",
            OrderDate = firstOrderDate is { } first ? $"最早 {first:yyyy-MM-dd}" : null,
            DeliveryDate = lastDeliveryDate is { } last ? $"最晚 {last:yyyy-MM-dd}" : null,
            OrderStatus = $"已完成 {completed:N0} 条",
            Priority = $"加急 {urgent:N0} 条",
            Layers = $"平均 {(double)layers / count:N2}",
            BoxL = $"平均 {(double)boxLength / count:N2}",
            BoxW = $"平均 {(double)boxWidth / count:N2}",
            BoxH = $"平均 {(double)boxHeight / count:N2}",
            BoardThickness = $"平均 {thickness / count:N2}",
            DevelopL = $"合计 {developLength:N0}",
            DevelopW = $"合计 {developWidth:N0}"
        };
    }

    private readonly OrderRepository _repository;
    private readonly CancellationTokenSource _lifetime = new();

    // 命令的具体实现留在私有字段里，对外只暴露 ICommand
    private readonly AsyncRelayCommand _loadCommand;
    private readonly RelayCommand _clearCommand;
    private readonly AsyncRelayCommand _importCommand;
    private readonly AsyncRelayCommand<SpreadsheetVisual> _exportCommand;
    private readonly AsyncRelayCommand<SpreadsheetVisual> _exportMergedCommand;
    private readonly AsyncRelayCommand _exportSourceCommand;

    public ICommand ImportCommand => _importCommand;
    public ICommand ExportCommand => _exportCommand;
    public ICommand ExportMergedCommand => _exportMergedCommand;
    public ICommand ExportSourceCommand => _exportSourceCommand;

    private IList? _selectedOrders;

    /// <summary>选中行的完整数据，只读实时集合；业务命令中可用 SelectedOrders?.Cast<CorrugatedOrder>().ToList() 取得当前选中订单。</summary>
    public IList? SelectedOrders
    {
        get => _selectedOrders;
        set => SetProperty(ref _selectedOrders, value);
    }

    private readonly RelayCommand _readSelectedOrdersCommand;
    private readonly RelayCommand<SheetColumn?> _renameColumnCommand;
    private readonly RelayCommand<object?> _previewReceiptCommand;
    private readonly RelayCommand<object?> _printReceiptCommand;
    private readonly RelayCommand<object?> _handPrintReceiptCommand;

    public ICommand ReadSelectedOrdersCommand => _readSelectedOrdersCommand;
    public ICommand RenameColumn => _renameColumnCommand;
    public ICommand PreviewReceipt => _previewReceiptCommand;
    public ICommand PrintReceipt => _printReceiptCommand;
    public ICommand HandPrintReceipt => _handPrintReceiptCommand;

    private string _selectedOrdersPreview = "Ctrl＋单击选中几行，再点击“读取选中行数据”查看。";
    public string SelectedOrdersPreview
    {
        get => _selectedOrdersPreview;
        private set => SetProperty(ref _selectedOrdersPreview, value);
    }

    // 按钮命令在 UI 线程执行。这里只读取选中项，不操作窗口或控件。
    private void ReadSelectedOrders()
    {
        // 这一行就是获取全部选中行：每一项都是完整的原始订单对象。
        List<CorrugatedOrder> orders = SelectedOrders?.Cast<CorrugatedOrder>().ToList() ?? [];

        if (orders.Count == 0)
        {
            SelectedOrdersPreview = "没有选中订单，请先选择一行或用 Ctrl 选择多行。";
            return;
        }

        // 在这里使用 orders 做业务处理；所有 100 个字段都可以读取。
        // 例如：orders[0].OrderNo、orders[0].CustName、orders[0].BoxL。
        // 下方只是展示前 5 条，避免把大量文本一次塞进界面；orders 包含全部选中行。
        SelectedOrdersPreview = $"上次读取：共 {orders.Count:N0} 条，以下预览前 {Math.Min(orders.Count, 5)} 条：\n"
            + string.Join("\n", orders.Take(5).Select(order =>
                $"订单号：{order.OrderNo}　客户：{order.CustName}　状态：{order.OrderStatus}　箱长：{order.BoxL} mm"));
    }

    private CorrugatedOrder? _selectedOrder;
    private int _selectedOrderIndex = 0;

    /// <summary>当前选中行的完整订单；未选中时为空。</summary>
    public CorrugatedOrder? SelectedOrder
    {
        get => _selectedOrder;
        set => SetProperty(ref _selectedOrder, value);
    }

    /// <summary>当前排序、筛选结果中的行索引，从 0 开始；未选中时为 -1。</summary>
    public int SelectedOrderIndex
    {
        get => _selectedOrderIndex;
        set => SetProperty(ref _selectedOrderIndex, value);
    }

    private async Task ImportAsync()
    {
        IsLoading = true;
        try
        {
            if (await SpreadsheetFiles.ImportAsync<CorrugatedOrder>(
                OrderHeaders, importedOrders => SourceOrders = importedOrders, cancellationToken: _lifetime.Token))
                Status = $"已导入 {SourceOrders?.Count ?? 0:N0} 条源数据。";
        }
        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested) { }
        catch (Exception ex) { Status = "导入失败：" + ex.Message; }
        finally { IsLoading = false; }
    }

    private Task ExportViewAsync(SpreadsheetVisual? currentViewSheet, bool includeMerges)
    {
        if (currentViewSheet is null || !CanClear()) return Task.CompletedTask;

        return ExportFileAsync(async token =>
        {
            await currentViewSheet.FilterWhenIdle.WaitAsync(token);
            if (includeMerges)
                return await currentViewSheet.ExportAsync(new FileExportOptions { CellMergeMode = CellMergeExportMode.CurrentView }, token);
            return await currentViewSheet.ExportAsync(cancellationToken: token);
        }, includeMerges ? "当前合并视图已导出。" : "当前视图已导出。");
    }

    private Task ExportSourceAsync()
    {
        if (SourceOrders is not { } sourceOrders || IsLoading) return Task.CompletedTask;

        return ExportFileAsync(
            token => SpreadsheetFiles.ExportAsync(OrderHeaders, sourceOrders, cancellationToken: token),
            "全部源数据已导出。");
    }

    private async Task ExportFileAsync(Func<CancellationToken, Task<bool>> export, string successMessage)
    {
        IsLoading = true;
        try
        {
            if (await export(_lifetime.Token))
                Status = successMessage;
        }
        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested) { }
        catch (Exception ex) { Status = "导出失败：" + ex.Message; }
        finally { IsLoading = false; }
    }

    /// <summary>20 列精简版表头，第一个导航页默认用它。</summary>
    private static readonly IReadOnlyList<SheetHeader> CompactHeaders =
    [
            SheetHeader.Column(nameof(CorrugatedOrder.OrderNo), "订单号", c =>
            {
                c.Width = 150;
                c.IsReadOnly = true;
                c.HorizontalContentAlignment = HorizontalAlignment.Center;
                c.VerticalContentAlignment = VerticalAlignment.Center;
                c.HeaderHorizontalContentAlignment = HorizontalAlignment.Center;
                c.HeaderVerticalContentAlignment = VerticalAlignment.Center;
            }),
            SheetHeader.Column(nameof(CorrugatedOrder.OrderDate), "订单日期", c =>
            {
                c.MergePolicy = SheetMergePolicy.Always;
                c.Width = 120;
                c.MinWidth = 108;
                c.StringFormat = "{0:yyyy-MM-dd}";
            }),
            SheetHeader.Group("订单信息",
                SheetHeader.Group("客户资料",
                    SheetHeader.Column(nameof(CorrugatedOrder.CustCode), "客户编码"),
                    SheetHeader.Column(nameof(CorrugatedOrder.CustName), "客户名称", c => { c.Width = 160; c.MinWidth = 156;c.MergePolicy = SheetMergePolicy.Always;  c.IsReadOnly = true;      }),
                    SheetHeader.Column(nameof(CorrugatedOrder.Industry), "客户行业")),
                    SheetHeader.Group("订单执行",
                    SheetHeader.Column(nameof(CorrugatedOrder.DeliveryDate), "交货日期", c =>{
                        c.Width = 120;
                        c.MinWidth = 108;
                        c.StringFormat = "{0:yyyy-MM-dd}";
                      

                    }),
                    SheetHeader.Column(nameof(CorrugatedOrder.OrderStatus), "订单状态", c => c.MinWidth = 112),
                    SheetHeader.Column(nameof(CorrugatedOrder.Salesman), "业务员"),
                    SheetHeader.Column(nameof(CorrugatedOrder.Plant), "生产厂区"),
                    SheetHeader.Column(nameof(CorrugatedOrder.Priority), "优先级", c => c.MinWidth = 112))),
            SheetHeader.Group("产品信息",
                SheetHeader.Group("箱型规格",
                    SheetHeader.Column(nameof(CorrugatedOrder.Product), "产品名称", c => c.Width = 160),
                    SheetHeader.Column(nameof(CorrugatedOrder.BoxType), "箱型代码"),
                    // 方式 2（后台列定义链式写法）：按值设置前景色/背景色，未命中自动继承 RowStyle
                    SheetHeader.Column(nameof(CorrugatedOrder.Layers), "瓦楞层数", c =>
                    {
                        c.HorizontalContentAlignment = HorizontalAlignment.Center;
                        c.WhenValue(7, "#4ADE80", "#2622C55E")
                         .WhenValue(3, "#FF6B81", "#26FF4D6D");
                    }),
                    SheetHeader.Column(nameof(CorrugatedOrder.Flute), "楞型")),
                SheetHeader.Group("成箱尺寸",
                    SheetHeader.Column(nameof(CorrugatedOrder.BoxL), "箱长(mm)"),
                    SheetHeader.Column(nameof(CorrugatedOrder.BoxW), "箱宽(mm)"),
                    SheetHeader.Column(nameof(CorrugatedOrder.BoxH), "箱高(mm)")),
                SheetHeader.Group("纸板展开",
                    SheetHeader.Column(nameof(CorrugatedOrder.BoardThickness), "纸板厚度(mm)", c => c.StringFormat = "{0:0.##}"),
                    SheetHeader.Column(nameof(CorrugatedOrder.DevelopL), "展开长(mm)", c => c.MinWidth = 144),
                    SheetHeader.Column(nameof(CorrugatedOrder.DevelopW), "展开宽(mm)", c => c.MinWidth = 144)))
    ];

    /// <summary>绑定到表格的三级表头。100 列页面由构造函数注入另一套。</summary>
    public IReadOnlyList<SheetHeader> OrderHeaders { get; }

    private readonly string _rangeHint;

    private ObservableCollection<CorrugatedOrder>? _sourceOrders;
    private SheetViewItems? _viewOrders;
    private string _status = "点击“加载数据库订单”读取数据。";
    private bool _isLoading;
    private double _loadMilliseconds;

    /// <param name="repository">数据源。</param>
    /// <param name="headers">表头定义；不传则用 20 列精简版。</param>
    /// <param name="rangeHint">加载提示里显示的范围说明。</param>
    public OrdersPageViewModel(
        OrderRepository repository,
        IReadOnlyList<SheetHeader>? headers = null,
        string rangeHint = "20 列 · 最多 100 万行")
    {
        _repository = repository;
        _rangeHint = rangeHint;
        OrderHeaders = headers ?? CompactHeaders;
        _loadCommand = new AsyncRelayCommand(LoadAsync, CanLoad);
        _clearCommand = new RelayCommand(Clear, CanClear);
        _importCommand = new AsyncRelayCommand(ImportAsync, CanLoad);
        _exportCommand = new AsyncRelayCommand<SpreadsheetVisual>(sheet => ExportViewAsync(sheet, false), sheet => sheet is not null && CanClear());
        _exportMergedCommand = new AsyncRelayCommand<SpreadsheetVisual>(sheet => ExportViewAsync(sheet, true), sheet => sheet is not null && CanClear());
        _exportSourceCommand = new AsyncRelayCommand(ExportSourceAsync, CanClear);
        _readSelectedOrdersCommand = new RelayCommand(ReadSelectedOrders);
        _renameColumnCommand = new RelayCommand<SheetColumn?>(col =>
        {
            if (col is null) return;
            SelectedOrdersPreview = $"[列头右键菜单] 触发「重命名列」→ 标题：{col.Header}，字段：{col.BindingPath}（当前宽：{col.Width}）";
            Status = $"右键列头：{col.Header}";
        });
        _previewReceiptCommand = new RelayCommand<object?>(item => HandleRowMenuAction("预览标签", item));
        _printReceiptCommand = new RelayCommand<object?>(item => HandleRowMenuAction("打印标签", item));
        _handPrintReceiptCommand = new RelayCommand<object?>(item => HandleRowMenuAction("手动打印", item));
    }

    private void HandleRowMenuAction(string actionName, object? item)
    {
        var order = item as CorrugatedOrder ?? SelectedOrder;
        if (order is null)
        {
            SelectedOrdersPreview = $"[行右键菜单] 触发「{actionName}」，未命中有效订单行。";
            return;
        }
        SelectedOrdersPreview = $"[行右键菜单] 触发「{actionName}」→ 订单号：{order.OrderNo}　客户：{order.CustName}　状态：{order.OrderStatus}　厂区：{order.Plant}";
        Status = $"已执行「{actionName}」：{order.OrderNo}";
    }

    /// <summary>查询或导入得到的完整订单集合。表格从这里生成筛选后的视图。</summary>
    public ObservableCollection<CorrugatedOrder>? SourceOrders
    {
        get => _sourceOrders;
        set
        {
            if (ReferenceEquals(_sourceOrders, value)) return;
            if (SetProperty(ref _sourceOrders, value))
            {
                _clearCommand.NotifyCanExecuteChanged();
                _exportCommand.NotifyCanExecuteChanged();
                _exportMergedCommand.NotifyCanExecuteChanged();
                _exportSourceCommand.NotifyCanExecuteChanged();
            }
        }
    }

    /// <summary>表格输出的实时视图，按当前筛选和排序读取。集合只能在 UI 线程访问。</summary>
    public SheetViewItems? ViewOrders
    {
        get => _viewOrders;
        set => SetProperty(ref _viewOrders, value);
    }

    public string Status
    {
        get => _status;
        private set => SetProperty(ref _status, value);
    }


    public bool IsLoading
    {
        get => _isLoading;
        private set
        {
            if (!SetProperty(ref _isLoading, value)) return;

            _loadCommand.NotifyCanExecuteChanged();
            _clearCommand.NotifyCanExecuteChanged();
            _importCommand.NotifyCanExecuteChanged();
            _exportCommand.NotifyCanExecuteChanged();
            _exportMergedCommand.NotifyCanExecuteChanged();
            _exportSourceCommand.NotifyCanExecuteChanged();
        }
    }

    public double LoadMilliseconds
    {
        get => _loadMilliseconds;
        private set => SetProperty(ref _loadMilliseconds, value);
    }

    public int TotalRows => _repository.Count;




    /// <summary>加载命令。对外只声明标准 ICommand，具体实现是 AsyncRelayCommand。</summary>
    public ICommand LoadCommand => _loadCommand;

    /// <summary>清空命令。</summary>
    public ICommand ClearCommand => _clearCommand;

    private async Task LoadAsync()
    {
        if (IsLoading) return;

        IsLoading = true;
    
        Status = $"正在从 SQLite 加载订单（{_rangeHint}）…";

        try
        {
           
            var orders = await _repository.LoadAllAsync(_lifetime.Token);
            _lifetime.Token.ThrowIfCancellationRequested();
          

            // 不会逐条触发 CollectionChanged，10 万行也不会有 10 万次通知
            SourceOrders = new ObservableCollection<CorrugatedOrder>(orders);
            Status = $"已加载 {orders.Count:N0} 条订单。";

   
        }
        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested) { }
        catch (Exception ex)
        {
            Status = "加载失败：" + ex.Message;
        }
        finally
        {
            IsLoading = false;
            
        }
    }

    private void Clear()
    {
        SourceOrders = null;
        Status = "已清空表格。";

     
    }

    private bool CanLoad() => !IsLoading;

    private bool CanClear() => SourceOrders is not null && !IsLoading;

    public void Dispose() => _lifetime.Cancel();
}
