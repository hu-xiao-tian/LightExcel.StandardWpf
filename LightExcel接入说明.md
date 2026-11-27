# LightExcel.StandardWpf 接入说明（1.9.97）

示例使用 **1.9.97** 保护包，在 XAML 绑定订单数据和三级表头，在 ViewModel 编写业务汇总与导入导出命令。功能和属性默认值见 [LightExcel 使用手册](LightExcel使用手册.md)。

## 运行这个项目

在 Visual Studio 打开 `LightExcel.StandardWpf.csproj`，还原 NuGet 后启动。示例读取运行目录的 `data/corrugated.db`。点“加载数据库订单”，当前数据文件有多少就读取多少，最多 100 万行，表格展示 20 列。左键点列头排序，按住 Shift 点击可追加排序列，右键点列头筛选。表格底部显示查询状态和当前结果行数。

项目目标是 .NET 10 WPF、Windows x64。项目内 `packages` 存放保护版 `LightExcel.Core` 与 `LightExcel.DataExchange` 1.9.97，`NuGet.Config` 配置本地源。新版本安装后若 IDE 使用旧缓存，可执行 Visual Studio 的“清理解决方案”，重新还原并生成。

## 数据和表头放在哪里

`ViewModels/MainViewModel.cs` 中的 `SourceOrders` 是查询或导入得到的完整源集合，`OrderHeaders` 是三级表头。表格的核心绑定如下。

```xml
<sheet:SpreadsheetVisual ItemsSource="{Binding SourceOrders}"
                         ViewItems="{Binding ViewOrders, Mode=OneWayToSource}"
                         Headers="{Binding OrderHeaders}"
                         SelectedItem="{Binding SelectedOrder}"
                         SelectedIndex="{Binding SelectedOrderIndex}" />
```

`OrderHeaders` 中的 `SheetHeader.Column` 配置字段、标题、宽度、对齐和只读等属性；`SheetHeader.Group` 配置合并分组。控件计算每格所占行列。用标准单级表头时可直接配置 XAML 的 `Columns`，只显示自动列时直接绑定 `ItemsSource`。

`ViewOrders` 是表格输出给 ViewModel 的实时只读视图，在 UI 线程读取。`ViewOrders?.Count` 是当前筛选后的行数，`SourceOrders?.Count` 是源集合行数。读取视图可用 `ViewOrders?.Cast<CorrugatedOrder>()`，得到按当前排序排列的记录，包括屏幕外的行。

点击任一数据格，窗口底部会显示订单号和从 0 开始的行索引。ViewModel 的 `SelectedOrder` 是完整的 `CorrugatedOrder` 对象，可直接读取未显示的字段；`SelectedOrderIndex` 是当前显示索引。没有选中行时分别为 `null` 和 `-1`。

示例默认勾选“只读 · 整行选中”和“允许多选行”。点击数据格选一行，Ctrl＋单击跳选或取消一行，Shift 连选。取消整表只读后，点击按单元格选中。订单号列仍为只读，订单状态等可写列可以编辑。

## 数据加载后默认选中

示例将 `MainViewModel` 的 `_selectedOrderIndex` 初始化为 `0`。首次加载出现数据后，控件通过上面的 `SelectedIndex` 绑定选中并高亮第一行。

接入自己的表格时，也可直接在 XAML 写 `SelectedIndex="0"`。如果要选中某条记录，提前将该对象赋给绑定的 `SelectedItem`，控件会在它出现在当前视图时选中。默认未选中。 [使用手册](LightExcel使用手册.md#数据加载后默认选中)

## 刷新订单和添加其他报表

再次点击“加载数据库订单”时，ViewModel 重新查询并替换 `SourceOrders`。控件用当前筛选条件匹配新数据。例如仍在筛选“待排产”，新订单满足条件就会显示。

业务每秒得到少量变化时，可保持同一个 `ObservableCollection<CorrugatedOrder>`，在 UI 线程按订单主键新增、删除或替换变更记录。需要在同一个窗口放多张报表时，各自绑定对应集合。开启布局记忆时，为同页不同表格取不同的 `x:Name`。 [绑定与实时更新](ItemsSource数据绑定.md#每秒更新订单) · [多表接入示例](LightExcel使用手册.md#13-一个程序使用多张报表)

## 在 ViewModel 导入导出

「导入 Excel」「导出当前视图」「导出全部源数据」「导出合并视图」分别绑定 `ImportCommand`、`ExportCommand`、`ExportSourceCommand`、`ExportMergedCommand`。命令实现位于 `MainViewModel.cs`。

```csharp
using LightExcel.DataExchange;

await SpreadsheetFiles.ImportAsync<CorrugatedOrder>(
    OrderHeaders, importedOrders => SourceOrders = importedOrders);

// currentViewSheet 来自导出按钮的 CommandParameter。
await currentViewSheet.FilterWhenIdle;
await currentViewSheet.ExportAsync();

// 传入源集合，始终导出全部已加载或导入的订单。
await SpreadsheetFiles.ExportAsync(OrderHeaders, SourceOrders);
```

文件选择、校验、业务对象转换和临时资源释放由库处理。导入全部成功后替换一次原始 `SourceOrders`。控件保留已有表头和筛选，使用新订单重新匹配。清除筛选后可以看到完整的导入数据。导入生成新订单对象，表头以外的模型字段使用默认值。

“导出当前视图”通过 `CommandParameter="{Binding ElementName=Sheet}"` 把当前表格传给 ViewModel。文件按当前视图顺序写入全部符合筛选条件的记录，包括屏幕外的行。未筛选时导出视图中的全部记录，筛选结果为零时只写表头。

“导出全部源数据”直接读取 `SourceOrders`，按源集合顺序输出完整记录。普通单级表头也能用 `sheet.ExportAsync()` 导出视图，并可按以下任一种方式导出源数据。

```csharp
// 自动生成列，导出模型全部可生成字段。
await SpreadsheetFiles.ExportAsync(SourceOrders);

// ViewModel 中已有的普通列定义。
await SpreadsheetFiles.ExportAsync(OrderColumns, SourceOrders);
```

如果普通列仅定义在 XAML 的 `Columns` 中，用表格命令参数入口保留实际标题与列顺序。完整命令写法见使用手册第 7 节。

默认格式为 XLSX，导出带表头。CSV 必须通过代码显式选择。

```csharp
await currentViewSheet.ExportAsync(new FileExportOptions { Format = SpreadsheetFileFormat.Csv });
```

带对话框的方法返回 bool，取消选择时返回 False。示例在执行时设置忙状态，统一控制加载、清空、导入和导出按钮。异常写入界面的状态文字。

## 表格功能开关

库默认深色、自动列、关闭序号、排序、筛选、编辑和复制粘贴。当前示例开启排序、筛选和复制，默认以只读方式打开。关闭“只读 · 整行选中”后，可编辑和粘贴可写列。左侧两列固定，查询索引由复选框控制。

只读列可复制，禁止界面编辑和粘贴。复制／粘贴、多选和填充通过 `CanUserCopyPaste` 控制。默认可撤销最近 50 步，支持设置 20～50 步。尺寸和逐列对齐设置在控件或列定义上。

## 数据保存与关闭

本示例查询 SQLite，单元格编辑和文件导入更新内存数据。数据库保存应通过业务自己的保存命令完成。

横纵滚动、编辑器、缓存和控件卸载资源由表格管理。`MainWindow.xaml.cs` 保留查询状态展示和窗口关闭处理，汇总通知与读取调度由控件管理，业务计算在 ViewModel。

## 自动记忆列布局

示例已启用 `IsLayoutPersistenceEnabled="True"`，保留已有的 `x:Name="Sheet"` 即可。引擎自动组合应用名称、所在页面完整类型和控件名称，保存列顺序，无需额外 LayoutKey 或保存事件。「列设置」默认隐藏。若要显示该入口并恢复列显隐，再设置 `CanUserHideColumns="True"`。不同页面同名表格互不影响，同类页面多个实例默认共用布局。特殊业务需要分别记忆时才指定稳定的 LayoutKey。详见同目录《列顺序记忆.md》。

旧版显式 `Orders.Main` 配置不会自动猜测迁移，新自动身份首次使用程序默认布局。若需要继续使用该旧偏好，可以保留原 LayoutKey。

## 字号与文字颜色（1.9.50）

```xaml
<sheet:SpreadsheetVisual ItemsSource="{Binding SourceOrders}"
                         FontSize="13" Foreground="#F1F8FF"
                         HeaderFontSize="14" HeaderForeground="#BDE5FF" />
```

正文和底部汇总使用 `FontSize`、`Foreground`，汇总默认加粗。各级列头使用 `HeaderFontSize`、`HeaderForeground`。省略颜色属性时随主题，省略字号时默认为 12 DIP。字号变大时可按需调整 `RowHeight`、`SummaryRowHeight` 或 `ColumnHeaderHeight`。

## 主页面汇总与视图变化

主页面通过 `ViewChangedMethod="OnOrdersViewChanged"` 指定业务方法。方法在后台接收筛选后的订单列表，直接写公式并返回结果。引擎在 UI 线程发布 `ViewChangedResult`，主表汇总行默认自动显示这个结果，省略 `SummaryItem` 即可。当前视图没有记录时汇总行折叠，订单号下方的数量由 ViewModel 显式赋给 `OrderNo`。

最小用法和绑定示例见[底部汇总行](底部汇总行.md)。


## 获取多选订单的可操作示例

加载数据后 Ctrl＋单击选择多行，点击下方“读取选中行数据”。代码在 `MainViewModel.ReadSelectedOrders()`：

```csharp
List<CorrugatedOrder> orders = SelectedOrders?.Cast<CorrugatedOrder>().ToList() ?? [];
// orders 就是全部选中订单，每条含全部字段，例如 orders[0].OrderNo、orders[0].CustName。
```

界面只预览前 5 条，实际列表包含全部选中行。无需 MainWindow.xaml.cs 的鼠标事件或额外控件引用。

条件颜色示例已配置在 MainWindow.xaml：Priority 为低／普通／加急时显示蓝／黄／红。使用方法见 [条件行字体颜色](条件行字体颜色.md)；默认 RowStyle 为空时不开启。

## 按列查找（1.9.79）

表格内按 Ctrl+F，无需在窗口或 ViewModel 注册事件。默认第一显示列，可切换列；Enter 向下查找，Shift+Enter 向上查找，Esc 关闭。范围为当前筛选、排序后的全部视图行，匹配显示文字，找到后自动滚动并选中。CanUserFind 默认 True，可按需关闭。完整说明见 [按列查找](按列查找.md)。

## 数据区合并

整表开关、每列策略和 XAML／C# 示例见 [数据区合并的属性与用法](LightExcel使用手册.md#数据区合并的属性与用法)。

加载订单后，可分别使用「自动合并指定列」和「允许手动合并」。自动合并作用于 `OrderHeaders` 中设置了 `MergePolicy` 的列。手动合并可独立启用，关闭「只读 · 整行选中」后，框选单元格并在右键菜单中合并。自动和手动合并格都可用 Ctrl+C 复制主格值，再粘贴到另一列的可写格。详细配置见 [数据区单元格合并](数据单元格合并.md)。

「导出当前视图」按当前筛选和排序逐行写入明细。「导出全部源数据」按源集合顺序逐行写入所有订单。开启合并后，点击旁边的「导出合并视图」，通过 `CellMergeMode = CellMergeExportMode.CurrentView` 将数据区合并结构写入 XLSX。三种 XLSX 导出只要包含表头，都会保留表头原有的合并。 [主手册与配置示例](LightExcel使用手册.md#数据区合并与-excel-导出)