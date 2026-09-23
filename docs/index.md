# ExcelFormulaLabs 用户文档

在 Excel 里直接用 `=STATS.MEAN()`、`=STR.REVERSE()`、`=JSON.PARSE()` 等函数。基于 C# / Excel-DNA 高性能实现，Python 级精度。

## 快速开始

1. 从 [Releases](https://github.com/zgrwo/ExcelFormulaLabs/releases) 下载对应位数的 `.xll`
   （32/64 位 Excel 必须匹配；`net48` 版免安装运行时，`net8.0` 版需 .NET 8 Desktop Runtime）；
2. 一键安装（自动解锁 + 注册到 Excel）：

   ```powershell
   powershell -ExecutionPolicy Bypass -File scripts/install.ps1 .\Analytics-AddIn-net48-64-packed.xll
   ```

3. 打开 [示例工作簿](samples.md)，看到公式结果即安装成功。

## 文档导航

| 文档 | 内容 |
| :--- | :--- |
| [用户手册](user-manual/user-manual.md) | 每个函数的详细示例与结果解读 |
| [API 参考](specification/api-reference.md) | 全部函数签名、参数与错误行为（数字唯一信源） |
| [技术规格](specification/specification.md) | 项目概述、模块清单、质量规格 |
| [术语表](governance/context.md) | 领域术语精确定义 |
| [示例工作簿](samples.md) | 16 个模块的可运行公式示例 |

> 安装、卸载与已知限制详见仓库 [README](https://github.com/zgrwo/ExcelFormulaLabs#readme)。
