using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using LightExcel.StandardWpf.Models;
using LightExcel.StandardWpf.Services;
using LightExcel.StandardWpf.ViewModels;
using LightExcel.StandardWpf.Views;
using LightExcel.StandardWpf.Views.Pages;

namespace LightExcel.StandardWpf;

/// <summary>
/// 组合根：在这里把 Repository、ViewModel、Window 组装起来。
/// 项目规模再大就换成 Microsoft.Extensions.DependencyInjection，这里保持零依赖。
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        string databasePath = Path.Combine(AppContext.BaseDirectory, "data", "corrugated.db");

        if (!File.Exists(databasePath))
        {
            MessageBox.Show($"找不到数据库：\n{databasePath}\n\n请从 GitHub Releases 下载 corrugated.zip，解压后将 corrugated.db 放到上述位置。",
                "数据库缺失", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
            return;
        }

        // ===== 页面一：20 列 × 10 万行（原有页面，逻辑不动）=====
        var repository = new OrderRepository(databasePath);
        var ordersViewModel = new OrdersPageViewModel(repository);

        // ===== 页面二：复用同一套页面代码，全 100 列 × 200 行 =====
        // 优先用 200 万行的库；没有就退回原库（前 200 行结果一致）。
        string fullDatabasePath = Path.Combine(AppContext.BaseDirectory, "data", "corrugated-2m.db");
        if (!File.Exists(fullDatabasePath)) fullDatabasePath = databasePath;

        var fullRepository = new OrderRepository(
            fullDatabasePath,
            maxColumns: CorrugatedOrder.ColumnCount,
            maxRows: 2_000_000);
        var fullOrdersViewModel = new OrdersPageViewModel(
            fullRepository,
            FullOrderHeaders.Create(),
            "100 列 · 200 万行");

        // ===== 新增测试页只改这里：准备好页面后 AddPage 一行即可 =====
        var shell = new MainViewModel(Path.GetFileName(databasePath));

        // 第一个注册的页面会自动选中
        shell.AddPage(new OrdersPage(ordersViewModel));

        // 同一个 OrdersPage 类，换表头、换数据范围即成为第二个测试页
        shell.AddPage(new OrdersPage(
            fullOrdersViewModel,
            title: "全字段订单 · 100 列",
            description: "复用订单页代码 · 100 列全字段 · 最多加载 200 万行 · 关闭列布局缓存",
            glyph: "☰",
            persistLayout: false));

        // 新增 LRU 分页演示页（标准 MVVM：ViewModel + 极薄 View）
        var lruViewModel = new LruPagingPageViewModel();
        shell.AddPage(new LruPagingPage(lruViewModel));

        // 星号列宽 + 格内控件 + 禁止选中 + 合并 + 标准右键菜单综合演示页（纯 MVVM）
        var showcaseViewModel = new FeatureShowcasePageViewModel();
        shell.AddPage(new FeatureShowcasePage(showcaseViewModel));

        // 空白模板页：演示接入方式，正式页面做好后可以删掉这一行
        shell.AddPage(new PlaceholderPage());

        var window = new MainWindow { DataContext = shell };
        MainWindow = window;
        window.Show();

        // 临时验证：--demo100 直接切到 100 列页，并记录加载耗时与内存
        if (e.Args.Contains("--demo100"))
        {
            shell.SelectedPage = shell.Pages[1];
            var sw = Stopwatch.StartNew();

            _ = Dispatcher.InvokeAsync(async () =>
            {
                try
                {
                    await ((CommunityToolkit.Mvvm.Input.IAsyncRelayCommand)fullOrdersViewModel.LoadCommand)
                        .ExecuteAsync(null);
                }
                catch (Exception ex)
                {
                    File.AppendAllText(@"D:\Temp\load100.txt", "EXCEPTION " + ex + Environment.NewLine);
                }

                string line =
                    $"load={sw.Elapsed.TotalSeconds:F1}s " +
                    $"rows={fullOrdersViewModel.SourceOrders?.Count:N0} " +
                    $"managed={GC.GetTotalMemory(false) / 1048576:N0}MB " +
                    $"workingSet={Process.GetCurrentProcess().WorkingSet64 / 1048576:N0}MB " +
                    $"status={fullOrdersViewModel.Status}";
                File.AppendAllText(@"D:\Temp\load100.txt", line + Environment.NewLine);

                if (e.Args.Contains("--capture"))
                {
                    await Task.Delay(3000);
                    await Dispatcher.InvokeAsync(() => { }, System.Windows.Threading.DispatcherPriority.ContextIdle);
                    window.UpdateLayout();
                    var root = (FrameworkElement)window.Content;
                    int w = (int)Math.Ceiling(root.ActualWidth);
                    int h = (int)Math.Ceiling(root.ActualHeight);

                    var visual = new System.Windows.Media.DrawingVisual();
                    using (var dc = visual.RenderOpen())
                    {
                        dc.DrawRectangle(window.Background ?? Brushes.White, null, new Rect(0, 0, w, h));
                        dc.DrawRectangle(new VisualBrush(root), null, new Rect(0, 0, w, h));
                    }

                    var bmp = new RenderTargetBitmap(w, h, 96, 96, PixelFormats.Pbgra32);
                    bmp.Render(visual);
                    var enc = new PngBitmapEncoder();
                    enc.Frames.Add(BitmapFrame.Create(bmp));
                    using var fs = File.Create(@"D:\Temp\shot-100col-2m.png");
                    enc.Save(fs);
                }

                if (e.Args.Contains("--quit")) Shutdown();
            });
        }


        int shotIdx = Array.IndexOf(e.Args, "--shot");
        if (shotIdx >= 0)
        {
            var timer = new DispatcherTimer(TimeSpan.FromSeconds(10), DispatcherPriority.Background,
                (_, _) =>
                {
                    window.UpdateLayout();
                    var root = (FrameworkElement)window.Content;
                    int w = (int)Math.Ceiling(root.ActualWidth);
                    int h = (int)Math.Ceiling(root.ActualHeight);

                    var visual = new DrawingVisual();
                    using (var dc = visual.RenderOpen())
                    {
                        // 用窗口自身背景铺底：深色主题下若固定画白底，浅色文字会看不清。
                        dc.DrawRectangle(window.Background ?? Brushes.White, null, new Rect(0, 0, w, h));
                        dc.DrawRectangle(new VisualBrush(root), null, new Rect(0, 0, w, h));
                    }

                    var bmp = new RenderTargetBitmap(w, h, 96, 96, PixelFormats.Pbgra32);
                    bmp.Render(visual);

                    var enc = new PngBitmapEncoder();
                    enc.Frames.Add(BitmapFrame.Create(bmp));
                    using (var fs = File.Create(e.Args[shotIdx + 1])) enc.Save(fs);

                    Shutdown();
                }, window.Dispatcher);
            timer.Start();
        }

        int benchIdx = Array.IndexOf(e.Args, "--bench");
        if (benchIdx >= 0)
        {
            // 轮询等待数据加载完成，避免定时器早于加载触发后进程挂起
            var started = Stopwatch.StartNew();
            string output = e.Args[benchIdx + 1];

            DispatcherTimer? timer = null;
            timer = new DispatcherTimer(TimeSpan.FromMilliseconds(400), DispatcherPriority.Background,
                (_, _) =>
                {
                  
                }, window.Dispatcher);

            timer.Start();
        }
    }
}
