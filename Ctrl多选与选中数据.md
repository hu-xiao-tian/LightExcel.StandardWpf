# Ctrl 多选与选中数据（1.9.52）

状态：已交付。Ctrl 多选专项 1,874 项、六幅完整画面比对、标准控件 169 项、选区与非系统剪贴板 8,374 项、格内编辑 32,992 项、严格完整主回归 103,726 项及 1,000 次释放 live=0 通过。原生编辑宿主的 UI Automation 引用释放已修复，保留原回收断言并验证过期提供程序不可继续访问。保护包已在实际 StandardWpf 窗口通过 14 项短验收；正式包和示例为 1.9.52。未新增规模、性能或长测保证。

## 打开多选

只读表默认单选整行；设置 CanSelectMultipleRows 后支持 Ctrl 跳选。复制权限单独由 CanUserCopyPaste 控制，只读行可以多选而不开放复制。

```xml
<sheet:SpreadsheetVisual x:Name="Sheet"
    ItemsSource="{Binding Orders}"
    IsReadOnly="True"
    CanSelectMultipleRows="True"
    CanUserCopyPaste="True"
    SelectedItems="{Binding SelectedOrders, Mode=OneWayToSource}" />
```

- 单击：只选择当前行。
- Ctrl＋单击：追加该行，再次点击已选行则取消该行；取消最后一行后清空。
- Shift＋单击／方向键：从锚点选择连续范围。
- Ctrl＋Shift：在现有选区中追加锚点到目标的范围。
- 拖动：选择连续范围。
- IsReadOnly=False 时按单元格选择；SelectionMode=Extended 且 CanUserCopyPaste=True 时支持 Ctrl 跳选单元格。
- 单选模式下 Ctrl 不会开启多选。运行时关闭多选会收缩到活动项。

## 在 StandardWpf 示例里直接体验

打开 D:/Temp/LightExcel.StandardWpf，加载订单，勾选“允许多选行”，按住 Ctrl 点击不相邻的几行，再点击表格下方的 **读取选中行数据**。

- MainWindow.xaml 将 SelectedItems 传给 ViewModel 的 SelectedOrders，并将按钮绑定到 ReadSelectedOrdersCommand。
- MainViewModel.cs 的 ReadSelectedOrders 方法中，`List<CorrugatedOrder> orders = SelectedOrders?.Cast<CorrugatedOrder>().ToList() ?? [];` 就是获取全部选中行。
- orders 中每项是完整的原始 CorrugatedOrder，可以直接读 OrderNo、CustName、BoxL 等全部属性，包括未显示的列。预览只显示前 5 条，不限制实际取得的行数。
- 页面标明“上次读取”，这是点击按钮时的结果；重新选择后再次点击即可。没有选中时会明确提示。
- 按钮命令在 UI 线程获取快照；如后续计算耗时，先取列表，再将业务计算放到后台。列表快照保留原对象引用，不是独立深拷贝，也不会自动保存数据库。

## ViewModel 获取完整选中行

```csharp
using System.Collections;
using System.Linq;

private IList? _selectedOrders;
public IList? SelectedOrders
{
    get => _selectedOrders;
    set => SetProperty(ref _selectedOrders, value);
}

// 在你的命令里直接使用：
var selected = SelectedOrders?.Cast<OrderRow>().ToList();
foreach (var order in selected ?? [])
{
    // order 是完整原始业务对象，这里写业务处理。
}
```

SelectedOrders 是引擎提供的只读实时集合，不能在集合上 Add、Remove 或 Clear。它实现 INotifyCollectionChanged 和 Count 属性通知，UI 可以绑定 SelectedOrders.Count。在 UI 线程读取；如果要在后台处理，先在 UI 线程 ToList() 取得行引用快照。快照不深拷贝业务对象。筛选／刷新进行中不提供未提交视图的行，请等待 FilterWhenIdle 后读取。

无需增加 SelectedOrders 数据源，也无需自己订阅每一个单元格的鼠标事件。集合只按选区读取选中的记录，不复制整张表。

窗口后台也可以这样读：

```csharp
var selected = Sheet.SelectedItems.Cast<OrderRow>().ToList();
var count = Sheet.SelectedItems.Count;
Sheet.ClearSelection();
```

SelectedItem / SelectedIndex 保留现有活动行的语义。SelectedItems 返回所有选中行，按当前显示顺序排列。编辑模式下，同一行有多个选中格时，这一行只返回一次；单元格精确范围使用 SelectedRanges。SelectedRange 继续表示活动的连续块，不代表两块之间的空隙也被选中。

对象集合返回原始对象；DataTable／DataView 返回 DataRowView（可用 .Row 获取 DataRow）。排序按记录身份维护选中项，筛掉／删除的记录从选中项中移除，清除筛选不自动重新选中这些记录；替换整个 ItemsSource 清空选区。身份与布局维护使用引擎统一预算，预算不足时拒绝扩展并保留原选区，通过 ClipboardError 提示。

## 复制与写入边界

只读模式 Ctrl 选择第 2、5、8 行，复制时只输出这三行的可见列。可编辑模式的不连续单元格复制会跳过未选中的整行，在涉及的列范围内以空字段保留未选中格的位置，不会读取这些格的数据。不连续目标暂不支持粘贴和拖拽填充；请先选择一个起始格或连续范围，再粘贴。连续选区的原有粘贴、填充、只读检查和撤销规则保持原有用法。

## 整行高亮

引擎内置、冻结并共享以下纵向渐变，不需要每张表添加资源：

```xml
<LinearGradientBrush StartPoint="0,0" EndPoint="0,1">
    <GradientStop Color="#0FA8FF" Offset="0" />
    <GradientStop Color="#0075FA" Offset="0.53" />
    <GradientStop Color="#0053DF" Offset="1" />
</LinearGradientBrush>
```

每条选中行各自渐变，选中文字为白色。单选、连续多选和 Ctrl 跳选统一，固定列与横向滚动区域一致。只绘制可见选中行，文字使用现有有界缓存，无动画、模糊或阴影。

交互参考：微软 WPF DataGrid 官方键鼠行为文档：https://learn.microsoft.com/en-us/dotnet/desktop/wpf/controls/default-keyboard-and-mouse-behavior-in-the-datagrid-control
