# context.md — 项目术语表

> 领域词汇的精确定义。AI 在对话中应使用这些术语，避免近义词。
> 链接索引见 [AGENTS.md](../../AGENTS.md#参考)。

## 层（Layer）

**Foundation** — 零依赖基础层。提供 InputNormalizer、ElementWiseMapper、OutputWrapper、ExcelEmpty/ExcelError 哨兵，以及 FilterUtils、ArrayOperations、ComparisonUtils、DictOperations 等共享工具。被 Analytics 和 DataToolkit 引用。
_Avoid_: 基础层、工具层、Utils

**Analytics** — 统计/回归/线性代数/工艺参数反解/物理化学/实验设计。依赖 MathNet.Numerics 5.0.0 + Foundation。产出 Analytics-AddIn-{tfm}-packed.xll（tfm = net48/net8.0，含 -64 变体）。
_Avoid_: 统计模块、分析层

**DataToolkit** — 字符串/日期/正则/JSON/XML/SQL/文件/数组/字典/透视/范围导出。依赖 Foundation（net48 额外依赖 System.Data.SQLite.Core + System.Text.Json）。产出 DataToolkit-AddIn-{tfm}-packed.xll（tfm = net48/net8.0，含 -64 变体）。
_Avoid_: 工具层、工具箱

## 调用链角色

**UDF** — 用户定义函数，`[ExcelFunction]` 特性标记的 `public static object` 方法。在 Excel 中以 `=CATEGORY.NAME()` 形式调用。每个 UDF 约 5 行，仅做分发与适配。
_Avoid_: 函数、方法（太泛）

**Core** — 纯逻辑 `internal static class`，无任何 Excel-DNA 引用。被 UDF 调用。每个 Core 类含多个 `internal static` 方法。
_Avoid_: 核心层、业务层

**MapOver** — 保持输入形状的元素级映射（null/error/empty 透传）。`ElementWiseMapper.MapOver<TIn, TOut>`。
**MapOverFlat** — 强转一维数组的映射（标量输入也返回 `object[]`）。`ElementWiseMapper.MapOverFlat<TIn, TOut>`。
**MapOverMulti** — 多参数广播映射（尺寸不匹配返回 `ExcelError.Value`）。`ElementWiseMapper.MapOverMulti<T1, T2, TOut>`。
_Avoid_: 映射器、包装器（太泛）

**V()** — `AnalyticsHelpers.PrepV(object)` — 将输入展平为一维 `double[]`。尺寸不匹配或 null → `NaN`（非 ExcelError）。仅统计分析 UDF 使用。
**M()** — `AnalyticsHelpers.PrepM(object)` — 将输入转为 `double[,]` 矩阵。
**D()** — `InputNormalizer.NormalizeTo2D(object)` — 将输入转为 `object[,]`。

**OutputWrapper.WrapError** — 统一异常 → `#VALUE!` 的防护边界。所有 UDF 的最外层包装。

## 数据流术语

**哨兵（Sentinel）** — InputNormalizer 五层防护体系（L1-L5），详见 [excel-dna-project.md §哨兵契约](../../skills/excel-dna-project.md#哨兵契约-l1-l5)。核心原则：不可转换值返回类型零值哨兵（`double`→`NaN`、`long`→0、`int`→0、`bool`→`false`、`DateTime`→`MinValue`、`string`→`""`），不抛异常。

**表头行契约（hasHeaders）** — 所有接受 `object[,]` 的 Core 方法必须含 `bool hasHeaders = true`。默认跳过第一行（表头），数据从 `r=1` 开始。例外（纯结构变换，不解释表头语义）：`Transpose`、`SelectColumns`、`SelectRows`、`CrossJoin`、`Flatten2D`、`Count`、`Keys`、`Values`、`ToDoubleMatrix`（与 AGENTS.md §4 及 pre-commit check-6 豁免名单一致）。

**ExcelEmpty** — Foundation 层定义的 Excel 空单元格哨兵，`ExcelEmpty.Value`。
**ExcelError** — Foundation 层定义的 Excel 错误哨兵，携带 `Code`（如 2042=`#N/A`）。MapOver 层遇到 ExcelError 透传不处理。
**OLE date** — OLE 自动化日期格式，纪元为 1899-12-30。Excel 日期序列号的内部表示。

**报告表（Report Table）** — 回归函数（OLS/WLS/Ridge/ANOVA1）返回的 `object[rows, cols]` 纵向表格。col0 = 字段名，col1.. = 值或数组展开。用 `INDEX(report, row, col)` 提取单个值。

## 算法与模式术语

**R7 分位数算法** — STATS.PERCENTILE / IQR / SUMMARY 使用的分位数插值算法，对标 Excel `PERCENTILE.INC` 和 Python scipy 默认。

**参数化查询** — SQL.* 函数使用 SQLite 参数化 INSERT（`@p0, @p1, ...`）插入数据，防止注入。列名经字母数字消毒。SQL 语句本身不可参数化。

**正则超时** — 所有 REGEX.* 函数内置 5 秒 `TimeSpan` 超时，防止 ReDoS 攻击。

**沙箱（Sandbox）** — FS.* 函数可选的文件系统访问限制，通过 `FileSystemCore.SandboxRoot` 配置。越界访问返回 `#VALUE!`。

**广播（Broadcast）** — MapOverMulti 的标量→数组自动扩展行为。标量参数广播到数组尺寸，等长数组逐元素配对。

**反解（Inverse Solve）** — SOLVE.* 的核心能力：给定输出目标，在可调参数边界内寻找使前向模型预测≈目标的可调参数组合。`SOLVE.INVERSE` 返回推荐表；目标不可达时返回边界上最接近解并标 `不可达`（见 [ADR-0007](../adr/0007-solve-module-and-bounded-search.md)）。
_Avoid_: 逆运算、逆向拟合

**请求行（Request Row）** — SOLVE 数据表中可调列（`Variable*`）留空或非数值、至少一个输出目标有值的行。可内嵌 `data`，也可用独立 request 表（同表头）。历史行相反：可调列与输出列全为数值。
_Avoid_: 待求解行、预测行

**可达性（Reachability）** — SOLVE 在参数边界内均匀采样 2000 点，用历史拟合模型计算输出区间；目标落入区间（相对容差 1e-9×量级）或优化器已达成（最大偏差 σ≤1e-6）即为 `可达`，否则 `不可达`。Core 数值接口：0=可达、1=不可达。
_Avoid_: 可行性、可解性

**最大偏差σ（Max Deviation σ）** — SOLVE.INVERSE 每请求行的目标达成度：`maxⱼ |预测ⱼ−目标ⱼ| / 历史输出标准差ⱼ`（标准差为 0 取 1；仅统计有目标的输出）。≈0 = 精确命中。
_Avoid_: 误差、残差（易与回归残差混淆）

**表头前缀角色（Header Prefix Role）** — SOLVE 数据表的列角色约定：`Incoming*`/`来料*`（已知条件）、`Variable*`/`可调*`/`变量*`（可调参数）、`Fixed*`/`固定*`（固定参数，留空取历史中位数）、`Output*`/`输出*`（输出/目标）。前缀不区分大小写，未识别列忽略。
_Avoid_: 列名配置、角色参数

**速率模型（rate）** — SOLVE 的时间外推模型（ADR-0008）：`OutputZx(t) = IncomingZx − t·g(可调/来料/固定)`，g 为线性模型；配对来料与时间列受约束（系数 1 与 −t），可对 t≠历史值预测/反解。由 `model="rate"` 显式启用；`auto` 将其作为候选之一。
_Avoid_: 时间模型、衰减模型

**输出-来料配对（Output-Incoming Pairing）** — rate 模型按去掉角色前缀后的后缀匹配（OrdinalIgnoreCase）：`OutputZ1`↔`IncomingZ1`、`输出收率`↔`来料收率`。配对缺失时显式 rate 报错。
_Avoid_: 关联列、映射列

**时间列（Time Column）** — rate 模型中唯一表头以 `Time`（忽略大小写）或 `时间` 结尾的特征列。列角色决定是否可调：`VariableTime`/`可调时间` 请求空白时参与寻优（bounds 可放宽），`FixedTime` 为条件（留空取历史中位数）。
_Avoid_: 时长列、周期列

## 构建术语

**TFM** — Target Framework Moniker，即 `net8.0` / `net8.0-windows` / `net48`。
_Avoid_: 目标框架、框架版本

**.xll** — Excel-DNA 打包产物，Excel 加载项文件。命名格式 `{Module}-AddIn-{tfm}[-64]-packed.xll`（如 Analytics-AddIn-net48-packed.xll）。文件名含 `64` 表示 64 位版本。
**.dna** — Excel-DNA 清单文件（XML），声明 RuntimeVersion 和 ExternalLibrary。`.dna.tpl` 是多目标模板。
**ExcelDnaPack** — 将 .dna + DLL 打包为单一 .xll 的 MSBuild 任务。

## 测试术语

**交叉验证（Cross-Validation）** — Python（numpy/scipy/sklearn）与 C# 实现逐项对照，精度 1e-10。覆盖 STATS/REGRESS/LINALG/PHYCHEM/DOE/SOLVE。
**CrossVal** — `dotnet test --filter "CrossVal"` 运行的交叉验证测试子集。
**verify-manual.py** — Python 脚本，验证 user-manual.md 中全部 UDF 示例与源码行为一致。
**verify-docs.sh** — verify-docs.ps1 的 POSIX 包装器（19 项文档一致性检查，唯一实现为 PowerShell 版本）。

## 平台术语

**IntelliSense 框架隔离** — net48 启用 ExcelDna.IntelliSense（参数提示），net8.0 禁用（Excel-DNA Issue #343：`ExcelSynchronizationContext.Post()` 内部空引用）。代码隔离：`#if NET48` 条件编译。
**双 TFM** — 项目同时编译 net48 和 net8.0-windows 两套目标，产出两套 .xll。