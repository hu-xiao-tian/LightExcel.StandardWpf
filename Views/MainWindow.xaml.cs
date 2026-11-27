using System.Windows;
using LightExcel.StandardWpf.ViewModels;

namespace LightExcel.StandardWpf.Views;

/// <summary>
/// 外壳窗口：只负责导航与内容承载，不包含任何业务逻辑。
/// 具体测试页在 App.OnStartup 里注册，见 <see cref="ViewModels.MainViewModel.AddPage"/>。
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        // 关窗时统一释放页面（取消后台加载、解绑表格事件）。
        Closed += (_, _) => (DataContext as MainViewModel)?.Dispose();

        // 绑定初始化时 SelectedItem 与 ItemsSource 的赋值顺序可能不同步，
        // 首帧后以 ViewModel 为准再同步一次，避免“导航高亮”和“内容区”不一致。
        Loaded += OnFirstLoaded;
    }

    private void OnFirstLoaded(object sender, RoutedEventArgs e)
    {
        Loaded -= OnFirstLoaded;
        if (DataContext is not MainViewModel viewModel) return;

        NavList.SelectedItem = viewModel.SelectedPage ?? viewModel.Pages.FirstOrDefault();
        viewModel.SelectedPage = NavList.SelectedItem as TestPageItem;
    }
}
