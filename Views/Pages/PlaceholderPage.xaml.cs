using System.Windows.Controls;

namespace LightExcel.StandardWpf.Views.Pages;

/// <summary>
/// 空白测试页模板：演示“最小可用页面”，同时把接入步骤写在界面上。
/// 想加新功能时，复制这个文件改名即可。
/// </summary>
public partial class PlaceholderPage : UserControl, ITestPage
{
    public PlaceholderPage(
        string title = "新增测试页",
        string description = "复制本页作为起点，三步接入新的功能测试。",
        string glyph = "＋")
    {
        Title = title;
        Description = description;
        Glyph = glyph;

        DataContext = this;
        InitializeComponent();
    }

    public string Title { get; }

    public string Description { get; }

    public string Glyph { get; }

    /// <summary>界面上展示的接入示例，直接从这里复制走。</summary>
    public string Snippet =>
        "// ① Views/Pages/MyTestPage.xaml(.cs)：让 code-behind 实现 ITestPage\n" +
        "public partial class MyTestPage : UserControl, ITestPage\n" +
        "{\n" +
        "    public string Title => \"我的功能测试\";\n" +
        "    public string Description => \"验证 XXX 行为\";\n" +
        "    public string Glyph => \"✎\";\n" +
        "    public void Dispose() { }\n" +
        "}\n" +
        "\n" +
        "// ② App.xaml.cs · OnStartup：注册到导航\n" +
        "shell.AddPage(new MyTestPage());";

    public void Dispose()
    {
        // 模板页没有后台任务，无需释放。
    }
}
