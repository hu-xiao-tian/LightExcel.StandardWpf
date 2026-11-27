# SpreadsheetVisual 查询结果绑定

内置双击／F2编辑现复用同一原对象写回链，见[单元格编辑说明](单元格内置编辑.md)。

`SpreadsheetVisual` 是业务表格控件，用于替代 DataGrid。数据流为：数据库查询 → DataTable.DefaultView／对象集合 → SpreadsheetVisual.ItemsSource → 表格显示与编辑写回。使用时无需创建或绑定 DataGrid，也不需要另建整表字符串矩阵。新增入口：`ItemsSource` 依赖属性、`AutoGenerateColumns`、`Columns`、`GetBoundItem` 和 `TrySetCellValue`。原自定义／分页数据源接口保留。

在 XAML 与后台代码中选择一个位置设置同一绑定属性。列配置示例见[使用手册](LightExcel使用手册.md#2-自定义列和多级表头)。

## DataTable / DataView

```csharp
// table 是业务查询返回的 DataTable；赋值在 UI 线程执行。
Sheet.ItemsSource = table.DefaultView;
// 需要立即读取绑定后的行列状态时，可同步应用：
Sheet.RefreshItemsSource();
```

MVVM 的 QueryRows 属性应发出 INotifyPropertyChanged，查询完成后直接赋给该属性，SpreadsheetVisual 负责显示查询结果。

```xml
<sheet:SpreadsheetVisual
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:sheet="clr-namespace:LightExcel.Controls;assembly=LightExcel.Benchmark"
    ItemsSource="{Binding QueryRows}" AutoGenerateColumns="True" />
```

DataView 自动表头采用 DataColumn.Caption，DBNull 显示为空；支持增删、编辑、排序和筛选。对绑定视图设置 Sort／RowFilter 后，SpreadsheetVisual 会更新显示。

## 每秒更新订单

持续轮询数据库时，可以在后台取回本轮变化，然后在 UI 线程同一批次更新原来绑定的 `ObservableCollection<T>`。新增用 `Add` 或 `Insert`，删除用 `Remove` 或 `RemoveAt`，修改可用属性变更通知，也可用 `Orders[index] = updatedOrder` 替换该条记录。按订单主键找记录，显示行号会随筛选和排序变化。

例如本轮只改了一个订单，在 ViewModel 的 UI 线程上按原集合的位置替换该条记录。

```csharp
// sourceIndex 是业务按订单主键在 Orders 里定位的位置。
Orders[sourceIndex] = updatedOrder;
```

同一轮有多条变化时，查完后在一次 UI 回调内提交，下一轮等本轮查询完成再开始。业务已经确认没有变化时可以跳过赋值，集合条数相同仍可能有字段变化。数据库只提供整份结果时，查询完成后给 `Orders` 赋值一次并发出属性通知。

控件自动合并普通集合通知，保留当前筛选条件并更新显示和汇总。筛选字段发生变化时，符合条件的记录进入结果，不符合的离开结果。需要整批重新查询时，可给绑定属性赋新的集合，控件按原条件重新计算；新集合中没有符合条件的记录时，显示零行。

在默认自动执行模式下，标准 `ObservableCollection<T>`、sealed 行类型、可确认的行内字段及普通无外部筛选／分组的集合视图，可以自动维护小批变化。仅筛选、不排序时，也只重判受影响的行；不需要为了获得增量更新先调用排序。

排序键唯一时，复用未变化记录的顺序并插入变更记录。排序键有重复值时，小批增删改复用筛选成员和未变化行的类型化排序键，仅重新读取变化行的字段；未变化键的复制和排序放到后台。排序仍按原 WPF 同值顺序重建，不会为了加速悄悄改变同值行的顺序。首次增加排序时，符合上述标准集合条件的源通知状态也会复用，避免再次建立整份行监听。位置提示只作快速定位，必须核对原对象引用；提示失效时按分片重新定位。

这些处理由引擎自动完成，无需新开关或手动维护缓存。第一次排序仍需读取全部排序字段；后续小批更新仍有后台复制、排序和结果映射成本。普通 `List<T>` 的静默替换无法通过集合通知确认，因此显式刷新时继续重新读取，避免复用旧对象。取消、换源、预算不足或资格不满足时走原有正确性路径。

1.9.59 减少了重复键排序的临时分配，也加快了把筛选结果交给业务汇总的取数。用原有绑定即可获得改进。50 张独立报表各 3,000 行、20 列并同时每秒更新的 120 批检查通过。百万行重复键短测仍有一批超过一秒，具体耗时随数据、业务公式和宿主负载变化。多表接入见[使用手册](LightExcel使用手册.md#13-一个程序使用多张报表)。

以上维护最多合并 128 次结构操作和 128 个脏行／对象，并受共享内存预算约束。Reset、Move、换源、超限、重复使用同一对象实例、未知计算属性依赖或自定义视图等情况保留准确的完整计算路径。显式 `Scan` 模式继续执行扫描。实时数据库写入、轮询与按主键合并由业务管理。

## 对象集合与列映射

支持 List、ObservableCollection、BindingList 和可索引的 WPF CollectionView。空类型化集合可以推断列；对象通过 INotifyPropertyChanged 通知更新。普通 List 无通知的手动改动后调用 RefreshItemsSource。

```xml
<sheet:SpreadsheetVisual
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:sheet="clr-namespace:LightExcel.Controls;assembly=LightExcel.Benchmark"
    xmlns:model="clr-namespace:LightExcel.Benchmark.Models;assembly=LightExcel.Benchmark"
    ItemsSource="{Binding Orders}" AutoGenerateColumns="False">
    <sheet:SpreadsheetVisual.Columns>
        <model:SheetColumn Header="单号" BindingPath="OrderNo" IsReadOnly="True" />
        <model:SheetColumn Header="金额" BindingPath="Amount" StringFormat="{}{0:F2}" />
    </sheet:SpreadsheetVisual.Columns>
</sheet:SpreadsheetVisual>
```

BindingPath 支持平面 CLR 属性或 DataColumn 名。显式 Columns 优先；无显式列时才按 AutoGenerateColumns 生成。关闭自动列且未提供列时不显示数据。每列可以单独设置 `Width`、`MinWidth`、`MaxWidth`；未指定宽度时按表头文字测量，详见[使用手册](LightExcel使用手册.md)。

另支持常见列写法 `Binding="{Binding Amount, Mode=TwoWay}"`，可用于普通单级表头的 XAML 或 C# 列配置。多级表头可在 ViewModel 构造 `SheetHeader` 定义，通过 `Headers="{Binding OrderHeaders}"` 接入；也可在 View 中调用 `Sheet.SetHeaders(...)`。参数与支持范围见[使用手册](LightExcel使用手册.md#2-自定义列和多级表头)。BindingPath 简写继续兼容。

| 配置 | 含义 |
|---|---|
| ItemsSource | 整份查询结果／对象集合 |
| AutoGenerateColumns | 没有显式列时，是否自动按字段／属性生成列 |
| SheetColumn.BindingPath | 数据库字段名或对象属性名，与表头文字分别配置 |
| SheetColumn.Header | 显示的表头文字 |
| SheetColumn.StringFormat | 显示格式；XAML 复合格式写作 `{}{0:F2}`，C# 写作 `{0:F2}` |
| SheetColumn.IsReadOnly | 拒绝该显示列的写回；数据字段自身只读时也不能写入 |

Columns 中的定义顺序就是显示顺序，可以只配置查询结果中的部分字段。写回按映射的原字段，不依赖查询结果的字段序号。BindingPath 与 Binding.Path 两种写法均可，不能冲突；支持常用 Converter 和独立列宽。任意 WPF 模板列不在当前支持范围。

## 写回原数据

```csharp
object? row = Sheet.GetBoundItem(selectedRow); // POCO 或 DataRowView
if (!Sheet.TrySetCellValue(selectedRow, selectedColumn, editor.Text, out string? error))
    ShowValidationError(error);
```

写回支持基本类型转换、只读、nullable 和 DBNull。DataRowView 没有现存编辑事务时自行开始／结束编辑，使 DataTable 的变更跟踪生效；已有事务仍由原宿主结束。**写回内存对象不等于执行数据库 UPDATE**，保存数据库及业务校验仍由业务负责。排序后应使用原行对象／主键，不能把显示行号当作数据库主键。

绑定与结构变化在 Dispatcher.DataBind 合并处理；错误通过 ItemsSourceBindingError / ItemsSourceBindingFailed 暴露，不继续显示旧查询结果。修改列定义或 RefreshItemsSource 可以恢复。自定义 getter 必须快速、安全返回，setter 应保持原子性。

## 线程和生命周期

- 查询可异步完成，但赋值、集合结构修改、视图排序和列配置放在 UI 线程。跨线程集合同步由宿主负责。
- 不在 getter 内同步查询数据库。裸 yield／未物化 LINQ 不作为随机访问源；先转 List，或采用现有分页 API。
- 渲染按可见行维护订阅；符合增量筛选／排序条件时，在内存预算内额外维护源级属性订阅。删除记录及时解绑，页面释放时退还预算；WPF CollectionView 自身的缓存由框架及原集合负责。
- 标准控件 `LightExcel.Controls.SpreadsheetVisual` 卸载时自动释放实例自有的渲染、查询和历史资源，恢复 Loaded 后按绑定重新接入。窗口和 ViewModel 的业务资源由宿主按自身生命周期处理。低层手动数据源的显式释放接口不应作为标准控件的必需接线。
- ItemsSource 与 SetDataSource 二选一；从 MVVM 切换到手动源时显式 ClearBinding，避免后续模型通知覆盖手动源。
- 原生 presenter 的 Sheet 脱离普通逻辑树，ItemsSource Binding 需要显式 Source 或直接赋集合，不应假定继承 Window.DataContext。
