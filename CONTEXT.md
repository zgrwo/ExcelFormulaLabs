# CONTEXT.md — 项目术语表

领域词汇的精确定义。AI 在对话中应使用这些术语，避免近义词。

## 层（Layer）

**Foundation** — 零依赖基础层。提供 InputNormalizer、ElementWiseMapper、OutputWrapper、ExcelEmpty/ExcelError 哨兵。被 Analytics 和 DataToolkit 引用。
_Avoid_: 基础层、工具层、Utils

**Analytics** — 统计/回归/线性代数/物理化学。依赖 MathNet.Numerics 5.0 + Foundation。产出 Analytics.xll。
_Avoid_: 统计模块、分析层

**DataToolkit** — 字符串/日期/正则/JSON/XML/SQL/文件/数组/字典/透视/范围导出。依赖 Foundation（net48 额外依赖 System.Data.SQLite.Core + System.Text.Json）。产出 DataToolkit.xll。
_Avoid_: 工具层、工具箱

## 调用链角色

**UDF** — 用户定义函数，`[ExcelFunction]` 特性标记的 `public static object` 方法。在 Excel 中以 `=CATEGORY.NAME()` 形式调用。每个 UDF 约 5 行。
_Avoid_: 函数、方法（太泛）

**Core** — 纯逻辑 `internal static class`，无任何 Excel-DNA 引用。被 UDF 调用。每个 Core 类含多个 `internal static` 方法。
_Avoid_: 核心层、业务层

**MapOver** — 保持输入形状的元素级映射（null/error/empty 透传）。
**MapOverFlat** — 强转一维数组的映射（标量输入也返回 `object[]`）。
**MapOverMulti** — 多参数广播映射（尺寸不匹配返回 ExcelError）。
**V()** — 仅统计分析 UDF 使用，内部调用 InputNormalizer + PrepV。尺寸不匹配或 null→NaN（非 ExcelError）。
_Avoid_: 映射器、包装器（太泛）

**OutputWrapper.WrapError** — 统一异常 → `#VALUE!` 的防护边界。所有 UDF 的最外层。

## 构建术语

**TFM** — Target Framework Moniker，即 `net8.0` / `net8.0-windows` / `net48`。
_Avoid_: 目标框架、框架版本

**.xll** — Excel-DNA 打包产物，Excel 加载项文件。分 Analytics-AddIn-packed.xll 和 DataToolkit-AddIn-packed.xll。
**.dna** — Excel-DNA 清单文件（XML），声明 RuntimeVersion 和 ExternalLibrary。`.dna.tpl` 是多目标模板。
**ExcelDnaPack** — 将 .dna + DLL 打包为单一 .xll 的 MSBuild 任务。

## 数据术语

**ExcelEmpty** — Foundation 层定义的 Excel 空单元格哨兵，`ExcelEmpty.Value`。
**ExcelError** — Foundation 层定义的 Excel 错误哨兵，携带 `Code`（如 2042=#N/A）。
**OLE date** — OLE 自动化日期格式，纪元为 1899-12-30。
