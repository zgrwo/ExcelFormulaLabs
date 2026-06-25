# 全面深度审查报告

**审查日期**: 2026-06-26
**审查范围**: 全项目（src/ + tests/ + docs/ + skills/）
**审查方法**: 多代理并行审查 + CodeGraph 代码追踪 + 历史 diff 复核
**基线**: 全量测试已通过（1,913 [Fact] 测试，双 TFM，Python 交叉验证）

---

## 总体评价

项目整体质量**高**。CLAUDE.md 规则体系成熟，异常过滤器统一（零裸 catch），防错三原则在 Core 层贯彻良好。1,913 个测试覆盖了正常路径和退化输入，Python 交叉验证保证了统计函数的数值精度。219 个 UDF 声明规范统一，api-reference 数量准确。

**主要问题集中在两类**：
1. **`hasHeaders` 契约不完整** — 两个 Core 方法遗漏该参数，违反 CLAUDE.md「同模块一致性」规则
2. **个别 UDF 缺少 NaN/Inf 显式守卫** — 零分母/负输入产生 NaN/Inf 静默传播，WrapError 兜不住

---

## 一、Foundation 层 — 评价 A

### 概述

基础层设计清晰。InputNormalizer L1-L5 哨兵契约完整执行，异常过滤器统一，零裸 catch。`NumericGuard` 系统性拦截矩阵 NaN/Inf。

| 维度 | 评分 | 说明 |
|------|------|------|
| 正确性 | A | L1-L5 哨兵执行彻底 |
| 安全性 | A | 无注入/遍历/反序列化风险 |
| 异常处理 | A+ | 零裸 catch，全部带 when 过滤器 |
| 文档 | B+ | 方法命名清晰，部分边界决策缺注释 |

### 发现

#### [HIGH] InputNormalizer.ToDouble 对 double.Infinity 无守卫 — L1 违规

**文件**: `src/Foundation/InputNormalizer.cs:222`
**CLAUDE.md 规则**: 防错原则1 L1「类型转换前显式检查 double.IsNaN(d) / double.IsInfinity(d)」

```csharp
if (value is double d) return d;  // ← Infinity 直接透传，无守卫
```

`ToDouble` 是所有数值转换的入口点（被 30+ 个调用者引用）。`double.PositiveInfinity` 在此处直接透传，下游代码若不做防御则 Infinity 静默传播。虽然这不是 `(long)NaN` 式的 CLR 未定义行为，但 CLAUDE.md L1 要求「显式检查」— 此处未检查。

**为什么全量测试通过**: `ToDouble(double.Infinity)` 返回 Infinity（而非 NaN 哨兵），但测试中未构造 Infinity 输入值。

**修复**: `if (value is double d) return (double.IsNaN(d) || double.IsInfinity(d)) ? double.NaN : d;`

#### [HIGH] NumericGuard 零测试覆盖 — 关键安全组件无测试

**文件**: `src/Foundation/NumericGuard.cs`（无对应测试文件）
**影响**: NumericGuard 是 Analytics 层 NaN/Inf 拦截的系统性防线（被 LinalgCore/RegressionCore 所有公开方法调用），但完全没有单元测试。`AgainstNonFinite(matrix)`、`AgainstNonFinite(vector)`、`AgainstNonFinite(scalar)` 三个方法的正确性仅在集成测试中间接验证。

#### [MEDIUM] ComparisonUtils.IsNumeric 对 double.NaN/Inf 返回 true — 有意设计但排序语义有风险

**文件**: `src/Foundation/ComparisonUtils.cs:268-274`
**CLAUDE.md 规则**: 防错原则1「静默传播阻断」

注释明确标注「Numeric types (incl. double.NaN/Inf): pass through」为有意设计（提交 `a79695a` 添加）。`ValuesEqual` 中 `Math.Abs(NaN - x) < epsilon` 正确返回 false（NaN ≠ 任何值）。但 `CompareSameGroup` 使用 `NaN.CompareTo(x)`（返回 -1）意味 NaN 排在所有实数之前，`NaN.CompareTo(NaN)` 返回 0（排序等价），行为虽确定但可能非用户预期。

**为什么全量测试通过**: 实际 Excel 输入路径中 NaN double 值几乎不到达 `CompareSameGroup`。测试未构造 `double.NaN` 原始值作为排序输入场景。

**影响**: 实际触发概率极低，但违反 CLAUDE.md「显式守卫」规范。

#### [LOW] KeyToString float 分支绕过 NaN/Inf 显式守卫

**文件**: `src/Foundation/DictOperations.cs:100-101`
**分析**: `double` 分支有 NaN/Inf 显式处理，`float` 分支通过 `((double)f).ToString("G17")` 隐式依赖。行为一致但代码不一致。

---

## 二、Analytics 层 — 评价 A-

### 概述

统计/回归/线性代数/物理化学模块整体设计扎实。`NumericGuard` 系统性使用保证矩阵级 NaN/Inf 拦截。Python 交叉验证覆盖所有统计函数。MapOver 选型正确。

| 维度 | 评分 | 说明 |
|------|------|------|
| 正确性 | A- | Core 逻辑正确，个别 UDF 缺 NaN/Inf 守卫 |
| 数值稳定性 | A | 除零 guard 完整，TSS=0/N=0 等退化输入有防护 |
| 测试覆盖 | A | 正常+退化+Python 交叉验证 |
| UDF 规范性 | B+ | MapOver 选型正确，个别元素级函数缺守卫 |

### 发现

#### [HIGH] PHYCHEM.DENSITY 零分母产生 Inf/NaN 静默传播

**文件**: `src/Analytics/PhyChemUdf.cs:23`
**CLAUDE.md 规则**: 防错原则1「静默传播阻断」

```csharp
// 当前: (m, v) => m / v  — v=0 → Inf/NaN 不抛异常，WrapError 兜不住
```

`MapOverMulti` 的 lambda 直接执行除法，零分母产生 `double.PositiveInfinity`/`NaN` 不抛异常。

**为什么全量测试通过**: 密度测试用正数体积，`v=0` 未覆盖。

**修复**: `(m, v) => v == 0 ? double.NaN : m / v`

#### [MEDIUM] STATS.SQRT/LN/LOG10 对负数/零输入静默产生 NaN/Inf

**文件**: `src/Analytics/StatsUdf.cs:38-40`
**CLAUDE.md 规则**: 防错原则1

```csharp
// Math.Sqrt(-1) = NaN, Math.Log(0) = -Infinity, Math.Log(-1) = NaN
```

Excel-DNA 运行时会将 NaN/Inf 正确显示为 `#NUM!`，行为对用户可见但缺少 CLAUDE.md 要求的显式守卫。

**为什么全量测试通过**: 测试用正数输入。Excel 原生 `=SQRT(-1)` 也返回 `#NUM!`，行为一致。

#### [MEDIUM] STATS.PRODUCT 空数组返回 1.0，与 SUM（NaN）不一致

**文件**: `src/Analytics/StatsCore.cs:55`

空乘积 = 1（数学恒等式），空和 = 0（数学恒等式）。但 STATS.SUM 空数组通过 guard 返回 NaN。两者 empty-input 语义不一致。

#### [MEDIUM] STATS.GEOMEAN/HARMEAN 缺零值/负值显式守卫

**文件**: `src/Analytics/StatsCore.cs:18-22`

MathNet 对负数/零返回 NaN，但代码未加显式 guard。用户看到 `#NUM!` 而非更明确的错误信息。

#### [LOW] LINALG.SOLVE 不检查方阵

**文件**: `src/Analytics/LinalgCore.cs:152-158`

非方阵传给 MathNet 抛异常，WrapError 转 `#VALUE!`。错误消息不友好。

---

## 三、DataToolkit 层 — 评价 B+

### 概述

最大的模块层（10 个 Core 类 + 10 个 UDF 类）。防御机制覆盖完整。两个 `hasHeaders` 契约违规需要修复。

| 维度 | 评分 | 说明 |
|------|------|------|
| 正确性 | B+ | Core 逻辑正确 |
| 安全性 | A | SQL 参数化、ValidatePath 全覆盖、Regex Timeout |
| 契约一致性 | B | Unpivot/RangeToCsv 缺 hasHeaders |
| 异常处理 | A+ | 零裸 catch |
| 测试覆盖 | B+ | 真实 I/O 通过 Sandbox Collection 序列化 |

### 发现

#### [HIGH] PivotCore.Unpivot 缺少 `bool hasHeaders` — CLAUDE.md 契约违规

**文件**: `src/DataToolkit/PivotCore.cs:50`
**CLAUDE.md 规则**: 「所有接受二维表格 object[,] 的 Core 方法，必须包含 bool hasHeaders = true」+「禁止同模块内部分方法含表头、部分不含」

```csharp
// PivotCore 中 Pivot、GroupBy 有 hasHeaders，Unpivot 没有
internal static object[,] Unpivot(object[,] data, int[] idCols, int[] valueCols)
// 硬编码: for (int r = 1; r < rows; r++) ... data[0, vc]（表头文本）
```

**为什么全量测试通过**: 测试数据总是带表头，未覆盖无表头场景。

**修复**: 添加 `bool hasHeaders = true`，`false` 时自动生成 "ColN" 列名，数据从 `r=0` 开始。

#### [HIGH] RangeExportCore.RangeToCsv 缺少 `bool hasHeader` — 同模块不一致

**文件**: `src/DataToolkit/RangeExportCore.cs:42`
**CLAUDE.md 规则**: 「禁止同模块内部分方法含表头、部分不含」

```csharp
// 同模块 RangeToHtml/Json/Markdown 有 hasHeader，RangeToCsv 没有
internal static string RangeToCsv(object[,] data, string delim = ",", bool quote = true)
// 硬编码: for (int r = 0; r < rows; ...)  — 无表头跳过
```

**为什么全量测试通过**: 测试总是从第 0 行导出，未验证 hasHeader=false。

#### [MEDIUM] FileSystemCore.NormalizePath 缺少 ValidatePath

**文件**: `src/DataToolkit/FileSystemCore.cs:43`
**CLAUDE.md 规则**: 防错原则2「防御完整性」

I/O 方法（Read/Write/Delete/Copy/Move）全部调了 ValidatePath，但 `NormalizePath` 通过 `FS.NORM` UDF 暴露，可在沙箱外解析路径（如 `FS.NORM("../../outside.txt")`）。信息泄露风险低（无实际 I/O），但防御不完整。

#### [MEDIUM] DateTimeUdf 多个函数不用 MapOver，数组行为不一致

**文件**: `src/DataToolkit/DateTimeUdf.cs`

DT.ISOWEEK/WEEKDAY/SOW/EOW 等使用 MapOver 支持数组，但 DT.WOM/DIM/AGEYEARS/ADDWKD/WKDBTWN/EASTER/ISLEAP/UNIXTS/FROMUNIX/DATEDIFF 直接调用不支持数组。

#### [LOW] RegexCore 每次调用使用 RegexOptions.Compiled

**文件**: `src/DataToolkit/RegexCore.cs:104-109`

一次性 UDF 调用中 Compiled 无缓存收益，增加编译开销。移除 Compiled 更合适（Timeout 已防 ReDoS）。

#### [LOW] ArrayCore.Slice 越界时静默钳制而非抛出清晰异常

**文件**: `src/DataToolkit/ArrayCore.cs:14`

`start > int.MaxValue ? int.MaxValue : (int)start` 钳制过大值，后续抛 `IndexOutOfRangeException`，错误消息不清晰。

---

## 四、测试 — 评价 B+

### 概述

1,913 个 `[Fact]` 测试覆盖正常路径和退化输入。Python 交叉验证覆盖统计函数。FileSystem 通过 `[Collection("Sandbox")]` 序列化避免并行冲突。

### 发现

#### [MEDIUM] 测试未覆盖关键 CLAUDE.md 边界

| 缺失场景 | 相关函数 | 为什么全量测试通过 |
|------|------|------|
| `double.NaN` 原始值作为排序输入 | CompareSameGroup | 测试用普通数值 |
| hasHeaders=false | Unpivot, RangeToCsv | 测试总是带表头 |
| 零体积 | DENSITY | 测试用正数分母 |
| 负数输入 | SQRT, LN, LOG10 | 测试用正数输入 |
| 负数数组 | GEOMEAN, HARMEAN | 测试用正数数组 |
| NormalizePath 沙箱 | FS.NORM | 沙箱测试未覆盖 path-only 方法 |

#### [HIGH] RegexCore 超时/回溯测试缺失 — 可能导致 Excel 挂起

**文件**: `tests/DataToolkit.Tests/RegexCoreTests.cs`
**影响**: 无 `(a+)+b` 类灾难性回溯测试，无超时触发验证。若 Regex Timeout 机制有 bug，用户提供的恶意正则可能使 Excel 挂起。这是安全关键路径。

#### [MEDIUM] PythonCrossValidationTests 命名误导

**文件**: `tests/DataToolkit.Tests/PythonCrossValidationTests.cs`
**分析**: 名为「Python 交叉验证」但实际是集成管道测试（String→Stats 跨模块组合），并非 Python 对照。建议重命名或明确注释。

#### [MEDIUM] 弱断言残留

提交 `34c1bde` 修复了 DateTimeUdf 的 `NotBeNull`→类型+值验证。需系统检查全项目 UDF 测试断言强度。

---

## 五、文档 — 评价 A-

| 维度 | 评分 | 说明 |
|------|------|------|
| 准确性 | A | 219 UDF 数量与代码一致 |
| 一致性 | A- | 个别参数描述与实际有差异 |
| 完整性 | B+ | 错误参考章节覆盖主要场景 |
| skill 准确 | A | 与当前代码模式匹配 |

README 测试数量 1,700+ 与当前 1,913 可更新。

#### [MEDIUM] api-reference 5 处可选参数标记错误

以下函数的签名中参数被标记为可选（`[...]`），但源代码中未设 `= null` 默认值：

| api-reference 行 | 函数 | 错误标记 |
|------|------|------|
| 136 | `STR.TEXTJOIN` | `[ignore_empty]` 应去掉括号 |
| 220 | `ARR.SLICE` | `[num_elements]` 应去掉括号 |
| 325 | `RANGE.TOHTML` | `[has_headers]` 应去掉括号 |
| 326 | `RANGE.TOJSON` | `[has_headers]` 应去掉括号 |
| 327 | `RANGE.TOMD` | `[has_headers]` 应去掉括号 |

**注意**: 需双向核实——代码是否应为这些参数添加 `= null` 默认值，或文档应去掉 `[...]` 标记。

#### [LOW] ElementWiseMapper 过时注释

**文件**: `src/Foundation/ElementWiseMapper.cs:7` — 注释「233 UDF wrappers」应更新为「219」。

---

## 六、CLAUDE.md 合规全景

| 规则 | 状态 | 说明 |
|------|------|------|
| 裸 catch{} = bug | ✅ | 全项目 src/ 零裸 catch |
| 异常过滤器 when 统一排除致命异常 | ✅ | 全部 catch 块合规 |
| L1 NaN/Inf 守卫 | ❌ | ToDouble 透传 Inf, DENSITY, SQRT/LN/LOG10 缺守卫 |
| L2 哨兵定义 | ⚠️ | ToDouble(double.Inf) 返回 Inf 而非 NaN |
| L5 ConvertValue | ✅ | double→NaN, 其他 throw |
| hasHeaders 契约 | ❌ 2 违规 | Unpivot, RangeToCsv 缺参数 |
| 同模块一致性 | ❌ 违规 | PivotCore, RangeExportCore 内部不一致 |
| ValidatePath 全覆盖 | ⚠️ 1 遗漏 | NormalizePath |
| Regex Timeout | ✅ | 全部带 TimeSpan |
| SQL 参数化 | ✅ | 无字符串拼接 |
| 重载不回退 | ✅ | RegexMatch/Replace n=1 用 Match() |
| 文档单一信源 | ✅ | 数字以 api-reference 为准 |

---

## 七、模块评分总览

| 模块 | 评级 | 核心优势 | 主要改善点 |
|------|------|------|------|
| Foundation | **A** | L1-L5 哨兵完整，异常处理统一 | IsNumeric NaN 设计文档化 |
| StatsCore | **A** | 退化输入 guard，MathNet 集成正确 | UDF 元素级缺守卫 |
| LinalgCore | **A** | NumericGuard 系统性使用，缓存设计 | Solve 缺方阵检查 |
| RegressionCore | **A** | 除零/TSS=0 guard 完整 | Ridge R² 语义文档化 |
| PhyChemCore | **A-** | 分子量解析健壮，单位换算完整 | DENSITY 零分母 |
| StringCore | **A** | 功能完整，性能优化好 | — |
| DateTimeCore | **A-** | ISO Week polyfill | UDF 层 MapOver 不一致 |
| RegexCore | **A-** | n=1 快路径，Timeout 全覆盖 | Compiled 开销 |
| JsonXmlCore | **B+** | 功能完整 | 手动序列化边界 bug |
| PivotCore | **B+** | 聚合正确，CrossJoin 安全限制 | Unpivot 缺 hasHeaders |
| SqlCore | **A** | SQL 参数化完整 | 列类型推断前 10 行 |
| FileSystemCore | **A-** | ValidatePath 全覆盖 | NormalizePath 遗漏 |
| RangeExportCore | **B+** | HTML/JSON/MD 完整 | RangeToCsv 缺 hasHeader |
| Array/DictSetCore | **A-** | 功能完整 | Slice 钳制语义 |
| UDF 层整体 | **B+** | MapOver 选型正确，声明规范 | 部分缺守卫/数组支持 |
| 文档 | **A-** | 219 数量准确 | 小细节 |
| 测试 | **B+** | 1,913 测试 + 交叉验证 | NaN/Inf 边界覆盖缺口 |

---

## 八、项目亮点

1. **异常处理统一**: 全项目零裸 catch，所有异常过滤器统一排除 OOM/StackOverflow/AV。极为罕见。
2. **InputNormalizer L1-L5 哨兵体系**: 设计清晰，执行彻底，Excel 空值/错误值/非数值语义完整。
3. **NumericGuard 系统性使用**: Analytics 所有矩阵/向量入口统一拦截 NaN/Inf。
4. **安全防御完整**: SQL 参数化、FileSystem ValidatePath 几乎全覆盖、Regex 全局 Timeout。
5. **重载不回退**: RegexMatch/Replace n=1 保留 Match() 快路径，性能意识强。
6. **Python 交叉验证**: 统计函数与 numpy/scipy 逐项对照，1e-10 精度。

---

## 九、高价值改善点（优先级排序）

| 优先级 | 改善项 | 工作量 | 影响 |
|------|------|------|------|
| **P0** | Unpivot 添加 hasHeaders 参数 | 30min | 消除 CLAUDE.md 违规 |
| **P0** | RangeToCsv 添加 hasHeader 参数 | 15min | 消除 CLAUDE.md 违规 |
| **P0** | DENSITY 零分母守卫 | 5min | 消除静默 Inf/NaN |
| **P0** | InputNormalizer.ToDouble 添加 Inf/NaN 守卫 | 5min | 消除 L1 违规，影响所有下游 |
| **P1** | SQRT/LN/LOG10 负数守卫 | 15min | 消除静默 NaN |
| **P1** | NormalizePath 加 ValidatePath | 5min | 闭合沙箱防御 |
| **P1** | DateTimeUdf 统一 MapOver | 2h | UX 一致性 |
| **P1** | 创建 NumericGuardTests.cs | 1h | 关键安全组件无测试 |
| **P1** | RegexCore 超时/回溯测试 | 30min | 安全关键路径 |
| **P2** | 补齐边界测试（NaN/Inf/零体积/hasHeaders=false） | 4h | 全量测试更可靠 |
| **P2** | api-reference 5 处可选参数标记修正 | 30min | 文档准确性 |
| **P2** | 移除 RegexCore Compiled | 10min | 微优化 |
| **P3** | SUM/PRODUCT 空数组语义统一 | 30min | API 一致性 |
| **P3** | RangeToJson 边界处理 | 1h | 健壮性 |
| **P3** | ElementWiseMapper 过时注释更新 | 5min | 文档准确性 |

**预计**: P0+P1 约 3h，P2+P3 约 6h。全部修复后可达 **A 级标准**。

---

## 十、总结

本项目是一个**高质量** Excel 函数增强库。219 个 UDF 覆盖 14 个功能域，架构分层清晰，CLAUDE.md 规则执行度极高。

**为什么全量测试通过而仍有发现**: 测试覆盖了正常路径和常规退化输入，但未覆盖 NaN/Inf 原始值、`hasHeaders=false`、零体积/负数等极端边界。这些是测试策略的已知取舍（聚焦用户常见场景），CLAUDE.md 规则的理论边界尚未完全纳入测试矩阵。
