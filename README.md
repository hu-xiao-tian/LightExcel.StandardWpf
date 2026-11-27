# LightExcel.StandardWpf

LightExcel 的 WPF 示例项目。项目使用 MVVM 将 SQLite 中的模拟订单绑定到表格，演示多级表头、排序与筛选、分页加载和 Excel 导入导出。

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
