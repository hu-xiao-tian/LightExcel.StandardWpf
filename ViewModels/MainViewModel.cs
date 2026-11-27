using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using LightExcel.StandardWpf.Views.Pages;

namespace LightExcel.StandardWpf.ViewModels;

/// <summary>
/// 外壳 ViewModel：只负责“有哪些页面、当前选中哪一个”。
///
/// 页面自身的数据与命令留在各自的 ViewModel（如 <see cref="OrdersPageViewModel"/>），
/// 外壳不感知任何业务逻辑，新增测试页不需要改这里。
/// </summary>
public sealed class MainViewModel : ObservableObject, IDisposable
{
    private readonly List<ITestPage> _allPages = [];
    private TestPageItem? _selectedPage;

    public MainViewModel(string databaseName)
    {
        DatabaseName = databaseName;
    }

    /// <summary>左侧导航数据源，顺序即显示顺序。</summary>
    public ObservableCollection<TestPageItem> Pages { get; } = [];

    /// <summary>底栏显示的数据库文件名，仅用于提示当前数据源。</summary>
    public string DatabaseName { get; }

    /// <summary>当前选中的导航项。</summary>
    public TestPageItem? SelectedPage
    {
        get => _selectedPage;
        set
        {
            if (!SetProperty(ref _selectedPage, value)) return;

            OnPropertyChanged(nameof(CurrentContent));
            OnPropertyChanged(nameof(PageTitle));
            OnPropertyChanged(nameof(PageDescription));
            OnPropertyChanged(nameof(HasPage));
        }
    }

    /// <summary>内容区承载的页面实例；页面被缓存，切走再切回状态不丢。</summary>
    public FrameworkElement? CurrentContent => SelectedPage?.Content;

    public string PageTitle => SelectedPage?.Page.Title ?? "功能测试台";

    public string PageDescription => SelectedPage?.Page.Description ?? "从左侧选择一个功能开始测试。";

    public bool HasPage => SelectedPage is not null;

    /// <summary>
    /// 注册一个测试页。第一个注册的页面会自动选中。
    /// select = true 可强制切到该页（例如启动后直接进入某个压测页）。
    /// </summary>
    public TestPageItem AddPage(ITestPage page, bool select = false)
    {
        var item = new TestPageItem(page);
        Pages.Add(item);
        _allPages.Add(page);

        if (select || Pages.Count == 1)
            SelectedPage = item;

        return item;
    }

    /// <summary>窗口关闭时统一释放各页面（取消后台加载、解绑表格事件）。</summary>
    public void Dispose()
    {
        foreach (var page in _allPages)
            page.Dispose();

        _allPages.Clear();
    }
}
