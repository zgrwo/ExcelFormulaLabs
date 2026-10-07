# DataToolkit `AddIn.cs` 覆盖率跟踪条目（2026-10-07）

> 状态：**[~] 已评估，决定不做（设计扭曲 > 收益）**。本条为跟踪项，不是待办承诺。
> 触发场景：`scripts/coverage.ps1` 汇总时 `ExcelFormulaLabs.DataToolkit.AddIn` 类覆盖率为
> **2/158 行**，是 DataToolkit 行覆盖率的**局部天花板**（该类不计入，模块行率 ≈89%；
> 计入则被 158 行中的 156 行未覆盖拉低）。

---

## 一、评估结论（逐项）

| 成员 | 行数占比 | 无 Excel 宿主可否测 | 结论 |
| :--- | :--- | :--- | :--- |
| `ShouldReportSandboxStatus()` | 2 行 | ✅ 可（纯逻辑） | **已覆盖**：早已抽为 `internal static`，由 `tests/DataToolkit.Tests/FileSystemCoreTests.cs` 的 `Sandbox_empty_string_root_counts_as_disabled_for_visibility` 断言（null→True / `""`→True / 真设防→False，与 `ValidatePath` 同口径） |
| `PreLoadNativeDependencies()` | ~66 行 | ❌ 不可 | 依赖 `ExcelDnaUtil.XllPath`（宿主 API）+ `LoadLibrary` P/Invoke + `%LOCALAPPDATA%` 落盘 |
| `LoadNativeLibrary()` | ~7 行 | ❌ 不可 | 纯 P/Invoke 包装，失败路径只写 `Debug.WriteLine`（无返回值可断言） |
| `AutoOpen()` | ~8 行 | ❌ 不可 | 串起上面两项，并调用 `ExcelAsyncUtil.QueueAsMacro`（Excel 运行时）+ net48 的 `IntelliSenseServer.Install()` |
| `ReportSandboxStatus()` | ~25 行 | ❌ 不可 | 写**真实用户日志文件**（`%LOCALAPPDATA%\...\sandbox-status.log`）+ 排队 COM 状态栏宏 |
| `AutoClose()` | ~13 行 | ⚠️ 勉强可，但**不该** | 见下 |

## 二、为什么不做（两条独立理由）

**理由 1 —— 需要为测试而扭曲设计。** 让 `PreLoadNativeDependencies` / `AutoOpen` /
`ReportSandboxStatus` 可测，必须引入注入接缝（`IExcelHost` 抽象 + `XllPath` 提供者 +
文件系统重定向 + 状态栏通道替身）。这是为一个 158 行的**宿主生命周期胶水类**建抽象层，
直接违背 AGENTS.md 准则 2（不为一成不变的场景建抽象层）——注入接缝的唯一消费者是测试。
按照任务口径：「若不可行或需要为测试而扭曲设计 → 不做」。

**理由 2 —— `AutoClose()` 虽可在无宿主下调用，但断言它会破坏测试隔离。**
net8.0-windows 下该方法体只有 `FilterUtils.ClearRegexCache()` 与
`FileSystemCore.EndSession()`；后者置**进程级全局**标志 `_sessionEnded`（`FileSystemCore.cs:71`），
置位后所有 `FS.*` 抛 `InvalidOperationException`，只能靠 `ResetForTesting()` 复原。
xUnit 默认按 collection 并行跑测试类，任何断言 `AutoClose()` 的用例都会与并行的
FS 测试争用该全局状态 —— 收益（≈2/158 行，对模块行率影响 < 0.1 点）远小于
"制造随机失败的跨类竞态"这一风险。

## 三、复评触发条件

满足**任一**条件时重开本项（届时须先补接缝设计说明，再谈测试）：

1. DataToolkit 行覆盖率余量因其他改动跌至 **< 1 个点**，且穷尽了非 `AddIn` 类缺口；
2. 引入真实的**宿主适配层**（P3「宿主适配程序集」结构性重构批次落地）——该批次本身
   就会把宿主 API 收拢到一个可替身边界上，届时 `PreLoadNativeDependencies` 的提取逻辑
   （嵌入资源探测 → 文件系统优先 → 内容寻址提取 → 失败降级）将自然变成可测纯逻辑；
3. 真实 Excel E2E 进 CI（ADR-0010 复评触发条件）——有宿主即可直接驱动 `AutoOpen`/`AutoClose`。

## 四、明确不做的事

- 不为凑覆盖率给 `AutoOpen`/`AutoClose` 写"调用不抛异常"的空壳用例（无信息断言）；
- 不把宿主 P/Invoke 调用点改写成可注入委托（设计扭曲）；
- **不**为此调低 `scripts/coverage.ps1` 的阈值。
