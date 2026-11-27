# WPF 接入与资源所有权

新业务接入优先按 [使用手册](使用手册.md) 操作；本页补充线程／所有权，并保留按日期标识的历史验证说明。

当前工程：业务引用 `src/LightExcel.Core/LightExcel.Core.csproj`，输出兼容名称 `LightExcel.Benchmark.dll`。实验室已独立为 `tools/LightExcel.RenderingLab`，业务无需依赖它或 `LightExcel.Testing`。见[工程拆分](引擎工程拆分-20261031.md)。以下按日期标识的通过记录仅适用于对应旧构建。

适用于现有 `SpreadsheetVisual` / `ISheetDataSource` / `PagedSheetDataSource` 接口。当前阶段按[短期交付与运行日志约定](短期交付与运行日志约定.md)判断可用性。以下历史资格不能替代当前构建的证据。

业务示例已接入有界后台日志、轮转和诊断导出，以及外部编辑器的提交、取消、校验、只读和刷新冲突检查，见[宿主说明](../samples/LightExcel.WpfHost/README.md)。这些宿主组件不编入渲染 DLL。后台呈现时必须让 presenter 独占重绘，不应再直接调用其 `Sheet.Redraw()` 消费待绘制状态。

当前代码、后台 WPF 方案及交付范围统一见[当前内核交付状态](当前内核交付状态.md)。后台光栅化提供公开的显式开关及示例 `--wpf-background`；以下历史软件路径的通过结论不自动授予后台路径资格。

2026-10-25 当前 3602 的独立证据已通过“真实呈现及输入响应”：使用显式 `SoftwareViewportPresenter`（示例 `--native-software`）、窗口 SoftwareOnly、业务 EXE SegmentHeap，在 DPI 1、1332×628 DIP 下通过实际桌面响应、像素、实际宿主流程、千次冷态退出和完整 30 分钟资源/准备预算。准备 P99 16.18ms，Private 首末五分钟差 +5.398MiB，全进程分配 15.090MiB/s，退出后所有像素面与原生拥有者归零。该原生子窗口存在 HWND airspace 限制；默认示例仍使用原 WPF Visual，不自动取得新呈现路径的桌面响应资格。详见[当前验收单](真实呈现及输入响应验收单-20261025.md)。以下 CF90 接入记录与代码约定继续保留，当前版本使用自己的独立证据。

2026-10-24 已通过资源验收的是 **CF90 内核 + 原 WPF Visual + 窗口 SoftwareOnly + 业务 EXE SegmentHeap**。原 8MiB 冷态退出检查及完整 30 分钟资源复核已通过，见[修复记录](旧冷态退出内存差值修复-20261024.md)。当前源码为 3602 输入响应候选，不能继承 CF90 的完整验收。业务示例通过 `app.manifest` 和项目的 `ApplicationManifest` 嵌入原生堆策略。既有 WPF 宿主须合并 `heapType` 元素，保留自己的 DPI、权限和兼容性设置；控件 DLL 不会替父进程选择堆。SegmentHeap 是该 EXE 的进程级原生堆策略，窗口软件渲染仍是单个 Window 的策略，两者均不修改系统全局配置。

## 线程

控件由创建它的 WPF Dispatcher 独占。数据绑定、视口、选择、显式重绘、缓存管理、资源释放都在该线程调用。后台代码通过 `Dispatcher.InvokeAsync` 调用控件；不要在 UI 线程上同步等待需要回到 Dispatcher 的任务。

`IViewportSheetDataSource.RowsChanged` 允许在后台发出。控件会将当前可见范围的变化合并到 Dispatcher，后台生产者不需要清理控件缓存或调用 `Redraw`。事件的 sender 必须是该数据源实例，行范围必须有效。数据源应先完成自身状态更新，再在锁外通知，避免回调和 UI 读取形成锁等待。

`ISheetDataSource.TryGetText` 在 UI 绘制期间同步调用，必须立即返回已有值；未加载返回 false，空字符串是真实空值。不得在其中同步访问文件、数据库或网络。分页提供者的 I/O 由 `ReadPageAsync` 在后台执行，并须遵守取消、页大小和文本限制。

CF90 为五个原先遗漏的公开入口补充 `VerifyAccess`：`ResetGlyphTemplateCounters`、`ClearGlyphTemplateCache`、`ResetTextCacheCounters`、`ClearTextCache` 和惰性创建字体对象的 `AsciiGlyphAvailable`。同一个测试程序在旧构建上允许五项后台调用，新构建均拒绝且缓存状态未变；正确 UI 线程调用后的像素一致。完整回归已通过，不能据此宣布原生内存与长期验收通过。

## 所有者及退出顺序

| 资源 | 所有者 | 退出方式 |
|---|---|---|
| 可见行池、绘图命令、文字缓存、挂起重绘、保护计时器、数据订阅 | `SpreadsheetVisual` | UI 线程 `ReleaseResources`；`Unloaded` 自动释放绘制资源 |
| 当前数据引用 | 控件借用，宿主持有 | `ReleaseResources(true)` 解除借用；默认 false 供暂时卸载后恢复 |
| 分页驻留、页加载任务及其取消状态 | 每个控件独立的 `PagedSheetDataSource` 会话 | `await DisposeAsync()`；同步 Dispose 仅发起退出，不等待 I/O 排空 |
| 文件句柄、数据库连接或外部 provider | 创建它们的宿主 | 会话异步释放完成后再 Dispose provider |
| `DrawCompleted`、`CellSelected`、`WheelRowsRequested` 的宿主处理器 | 订阅者 | 宿主退出时取消订阅 |
| 整个窗口的软件渲染策略 | 宿主 Window | 显式 `WindowRendering.SetMode(window, RenderMode.SoftwareOnly)`；当前资源验收同时要求业务 EXE 采用 SegmentHeap，见本页顶部 |

暂时移出可视树不代表业务数据会话已结束：控件保留数据引用以便恢复，也不代替宿主 Dispose 数据源。每个控件分别创建分页会话，可以共享真正支持并发及独立取消的 provider。不要让两个控件共享同一个视口会话，否则一个控件的取消会干扰另一个。

S08 已补齐释放时的列属性退订、自动调度停止及数据源订阅会话隔离。释放后的列变化和旧数据通知不会重新创建画面；WPF Loaded、显式绑定、Redraw 或 SetViewport 可恢复使用，同坐标 SetViewport 也有效。外部继续持有列、列集合或模型时，已释放控件及绑定适配器的回收已验证，见[精确失效与生命周期管理](精确失效与生命周期管理.md)。

自行创建离屏控件并手工调用 Measure/Arrange 的代码，应完成公共 UpdateLayout 再判断对象回收；未处理的 WPF SizeChanged 布局任务会暂时持有控件。正常可视树由 WPF 完成布局。这个前提不取代 ReleaseResources，也不要求访问或清理 WPF 私有状态。

业务示例默认在 Window 创建原生句柄前设置 SoftwareOnly；该设置不更改进程级渲染偏好或系统图形配置。`--default-hardware` 是未验收的诊断选项。当前 CF90 / 宿主 2F3ABC 的实际窗口检查确认默认 CompositionTarget 为 SoftwareOnly，实际 EXE 内嵌 SegmentHeap 清单，编辑、更新、替换及关闭排空通过；该配置另有完整 30 分钟资源证据。此前 EF8314 仅作为原堆策略的历史宿主保留。

关闭或切换数据时，先停止宿主自己的更新生产者并取消其订阅，再在 UI 线程释放控件，随后异步排空旧会话，最后释放其 provider。外部取消回调抛出异常时，控件仍清理自己的资源，但异常会向宿主传播；宿主必须用 finally 完成自己拥有的资源退出。例如：

```csharp
// 在 UI 线程开始调用；关闭期间不再向旧 source/view 发出更新。
try
{
    view.ReleaseResources(releaseData: true);
}
finally
{
    try { await session.DisposeAsync(); }
    finally { provider.Dispose(); }
}
```

若这是最后一个窗口，不能在默认自动退出之后才于 `Closed` 异步等待 I/O。应在 `Closing` 第一次触发时取消本次关闭、防止重复进入，执行上述异步退出，完成后再次 Close；或由应用显式管理 Shutdown。不要用 `.Wait()` / `.Result` 阻塞 UI。

## 可选原生软件呈现候选

`SoftwareViewportPresenter` 是显式选择的 `HwndHost`，示例通过 `--native-software` 启动；默认入口仍承载普通 `SpreadsheetVisual`。呈现层接收尚未进入其他可视树的 `SpreadsheetVisual`，独占它的重绘调度；宿主只将 presenter 放入布局，不能再把同一个 sheet 放入另一处布局或交给第二个 presenter。数据源、选区、视口及业务事件仍使用该 sheet 的接口。

`WheelRowsRequested` 是滚动请求，宿主仍须更新滚动条并调用 `SetViewport`。示例保留这条路径，同时由 sheet 的尺寸通知更新滚动范围。业务编辑器是窗口中的独立控件；不能从这个示例推导原生子窗口支持普通 WPF 的任意透明覆盖、层叠、旋转或内嵌编辑。

原生呈现层拥有 DIB、内存 DC、软件光栅缓冲和原生窗口映射。宿主在 UI 线程完成自身会话的停止与排空后，在 finally 中调用 presenter 的 `Dispose()`，即使会话清理抛出异常也必须执行。Dispose 解除事件、取消挂起绘制、释放原生资源和数据引用；不要在退出过程中继续向旧 sheet 发更新。明确重用 sheet 时应重新绑定数据或重新进入普通 WPF Loaded，再按正常所有权使用。具体关闭顺序见[实际宿主](../samples/LightExcel.WpfHost/Program.cs)。

当前 3602 使用最多五个像素面，单面 ≤64MiB、保留像素合计 ≤128MiB，无法容纳小光栅面时回退完整绘制；绘图快照每个可见行至多一份，另有四个公共面的快照槽，原命令重建或释放时失效。完整混合像素、释放和重新挂载检查通过，1000 个场景的实际/参考哈希与 F4E9 完全相同；实际桌面滚轮/点击 P95 33.224/33.612ms，通过原门槛；千次冷态退出点值 +1.129MiB、中位差 +1.260MiB，通过原 8MiB 限制。**最终 30 分钟记录因显示模式强制枚举无效中止，完整资格仍待完成，不能继承 CF90/F4E9 证明。** 原生绘制准备完成不等于屏幕上已出现该画面；普通 WPF `RenderTargetBitmap` 也不能用来证明 `HwndHost` 的实际屏幕内容。结果及限制见[本次验收单](真实呈现及输入响应验收单-20261025.md)。

## 边界及未完成范围

分页选项分别限制驻留页数、估算字节、并发、文本长度和标题预算。估算不是进程 Private Bytes，也不能限制 provider 私自申请的内存；provider 的取消和解析预算仍需履约。控件不持有历史视口的无界请求队列。

控件当前暴露选择和命中接口，业务编辑及提交由宿主提供。用户将全部长测统一为 30 分钟，8/24 小时不再列为必测项。当前本机软件窗口配置已联合通过资源预算、实际全进程分配率、千次原生窗口归属退出及十倍真实分页门禁；完整结论见[交付清单](内存与资源稳定交付清单-20261024.md)。真实屏幕响应、最低目标设备或默认硬件不由这些结果推导。

新接入示例位于 [`samples/LightExcel.WpfHost`](../samples/LightExcel.WpfHost/README.md)。关闭先取消本次 Closing、异步排空业务会话与日志，再将第二次 Close 排到 Dispatcher；即使 await 同步完成，也不会递归进入正在关闭的窗口。业务已引用独立 `LightExcel.Core` 类库；`LightExcel.Benchmark.dll` 是保留的兼容程序集名称，不代表仍依赖实验室。绑定示例和 SQL 示例另接入可选 DataExchange 文件模块，支持 XLSX／显式 CSV 导入导出，不等于完整 Excel 功能验收。
