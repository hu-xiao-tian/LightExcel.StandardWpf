namespace LightExcel.StandardWpf.Views.Pages;

/// <summary>
/// 导航页契约：一个测试页面 = 一个 UserControl + 一段导航元数据。
///
/// 新增一个功能测试页只要三步：
///   1. 在 Views/Pages 下新建 XxxPage.xaml（UserControl）；
///   2. 让 code-behind 实现本接口，构造函数里设置 DataContext；
///   3. 在 App.OnStartup 里 shell.AddPage(new XxxPage(...))。
/// 页面实例会被缓存，来回切换不会丢状态（表格筛选、滚动位置都保留）。
/// </summary>
public interface ITestPage : IDisposable
{
    /// <summary>导航栏标题。</summary>
    string Title { get; }

    /// <summary>导航栏副标题，也显示在页面内容区顶部标题下方。</summary>
    string Description { get; }

    /// <summary>导航栏左侧图标字符，用普通文本字形即可（如 ▦ ✎ ⚙）。</summary>
    string Glyph { get; }
}
