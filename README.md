# LightExcel.StandardWpf

`LightExcel.StandardWpf` 是 `LightExcel.Core` 和 `LightExcel.DataExchange` 两个 NuGet 包的 WPF 演示项目。

- `LightExcel.Core` 提供 `SpreadsheetVisual` 表格控件，以及多级表头、排序、筛选和分页等功能。[NuGet 包](https://www.nuget.org/packages/LightExcel.Core/)
- `LightExcel.DataExchange` 提供流式 XLSX 和 CSV 导入导出，支持导出当前视图或全部源数据，以及在 XLSX 中保留多级表头和单元格合并。[NuGet 包](https://www.nuget.org/packages/LightExcel.DataExchange/)

## 演示内容

- 使用 MVVM 将 SQLite 模拟订单绑定到 `SpreadsheetVisual`，体验多级表头、排序、筛选和分页。
- 从 Excel 导入订单，导出当前筛选和排序后的视图，或导出全部源数据。
- 将合并视图导出为 XLSX，保留多级表头和数据区合并。

## 快速开始

需要 Windows x64 和 .NET 10 SDK。

1. 在 Visual Studio 打开 `LightExcel.StandardWpf.slnx`，还原 NuGet 包并生成项目。
2. 下载并解压基础示例库。将 `corrugated.db` 放入 `LightExcel.StandardWpf.exe` 同级的 `data` 文件夹。如果文件夹不存在，先新建它。
3. 启动程序，点击「加载数据库订单」。如果要运行 200 万行压测，将 `corrugated-2m.db` 也放到同一个 `data` 文件夹，重新启动程序后打开「全字段订单 · 100 列」。

## 数据库下载

- **基础示例库**包含 10 万行订单，启动程序需要它。[下载 `corrugated.zip`（约 14 MiB）](https://github.com/hu-xiao-tian/LightExcel.StandardWpf/releases/download/test-data-corrugated-2m/corrugated.zip)
- **可选压测库**包含 200 万行订单，用于大容量数据演示。[下载 `corrugated-2m.zip`（约 283 MiB）](https://github.com/hu-xiao-tian/LightExcel.StandardWpf/releases/download/test-data-corrugated-2m/corrugated-2m.zip)

## 文档

- 查看如何把 LightExcel 加入自己的 WPF 项目。[接入说明](LightExcel接入说明.md)
- 查阅表格功能、属性和用法。[使用手册](LightExcel使用手册.md)

本项目采用 Apache 2.0 许可证。[查看许可证](LICENSE)
