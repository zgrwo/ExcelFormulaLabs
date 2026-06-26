# 全面深度审查报告

> **日期**：2026-06-27
> **范围**：全部源码 (40 文件, 4,470 行) + 测试 (37 文件, 7,336 行) + 文档 (6 文件)
> **方法**：5 个专业审查代理并行深读 + 全量测试 + 文档一致性验证 + 历史 diff 追踪

---

## 一、执行摘要

**总评：B+ (良好-优秀)**。项目整体工程质量中上，架构分层清晰，安全防护体系完整，测试覆盖优秀。发现 **1 个严重问题**（信息泄漏）、**7 个高优先级问题**、**约 20 个中低优先级改善点**。无阻推缺陷——所有严重/高问题均有可接受缓解或低概率触发条件。

| 维度 | 评分 | 简述 |
|------|:--:|------|
| 架构设计 | A | 三层分隔 (Foundation→Analytics/DataToolkit)，UDF/Core 严格解耦 |
| 正确性 | B+ | 核心算法经 Python 交叉验证，关键路径有 NaN/Inf 守卫 |
| 安全性 | B+ | 沙箱/超时/SQL 参数化均实现，1 个信息泄漏漏洞 |
| 防御编程 | A- | 异常过滤器统一，零裸 catch{}，哨兵契约完整 |
| 性能 | B+ | 热路径优化良好 (Regex n=1 快路径、LRU 缓存)，若干可优化点 |
| 测试 | A- | 1,999 测试/TFM，双 TFM 全绿，交叉验证对标 Python |
| 文档 | B+ | API 参考准确，用户手册齐全，少数数字过时 |

**全部测试通过**：Foundation 235 + Analytics 520 + DataToolkit 1,244 = 1,999/TFM × 2 TFM = **3,998 测试全绿**。文档一致性验证 10/10 通过。

---

## 二、模块逐项评估

### 2.1 Foundation 层 (共享基础)

| 文件 | 行数 | 职责 | 评价 |
|------|:---:|------|------|
| InputNormalizer.cs | ~400 | 类型转换、哨兵契约 L1-L5 | **A** — 结构完整，6 类类型委托明确，COM Range 提取正确 |
| ElementWiseMapper.cs | ~400 | MapOver/MapOverFlat/MapOverMulti 调度 | **A-** — 设计合理，4 个 MEDIUM 发现（ReshapeFlatToOriginal2D 隐式依赖） |
| OutputWrapper.cs | ~70 | WrapError + ReshapeOutput | **A** — 简单正确，O(rows×cols) 预填充可接受 |
| NumericGuard.cs | ~30 | NaN/Inf 集中守卫 | **A** — 单点守卫，避免散落重复 |
| FilterUtils.cs | ~120 | 过滤比较 + Regex 缓存 | **B+** — ConcurrentDictionary.Clear() 竞态条件（极低概率） |
| ArrayOperations.cs | ~300 | 排序/打乱/索引 | **A-** — 正确，SortIndices 前置条件依赖调用方 |
| ComparisonUtils.cs | ~300 | 值相等比较 | **B+** — IsNumeric 与 InputNormalizer.IsNumericCell 对 "NaN" 字符串行为不一致 |
| DictOperations.cs | ~120 | 字典键值操作 | **A** — 简单可靠，bool→"TRUE"/"FALSE" 可接受 |
| ExcelEmpty.cs/ExcelError.cs | ~20 | Excel 哨兵类型 | **A** — 清晰定义 |

**红线合规**：✅ 无裸 catch{}，✅ 异常过滤器完整，✅ 哨兵契约 L1-L5 全部实现

### 2.2 Analytics 层 (统计分析)

| 子模块 | 函数数 | 评价 |
|--------|:--:|------|
| StatsCore/Udf | 33 | **A-** — 对标 scipy 精度 1e-10，Sum/Product 将 Infinity→NaN（与抛异常原则不一致但有意设计） |
| LinalgCore/Udf | 19 | **B+** — DecompCache LRU 缓存设计良好，Hash 为 32-bit（8 条目下碰撞概率 ~7.5×10⁻⁹，实际无风险）。Identity 缺尺寸上限守卫（OOM 被异常过滤器排除，可能导致进程崩溃） |
| RegressionCore/Udf | 7 | **B+** — Python statsmodels 交叉验证完整性好。FactorImportance 遇零方差列崩溃，FitOLS 缺共线性检测 |
| PhyChemCore/Udf | 16 | **B** — 分子量解析嵌套限制已文档化。IdealGasLaw 的 `"*"` 哨兵比较脆弱（空白敏感），V() 错误地将空单元格当作「求解此变量」 |

**红线合规**：✅ UDF 无业务逻辑，✅ Core 零 Excel 依赖，✅ 异常过滤器完整

### 2.3 DataToolkit 层 (数据处理工具)

| 子模块 | 函数数 | 评价 |
|--------|:--:|------|
| StringCore/Udf | 34 | **A-** — 编码/解码完整，Base64Decode 异常安全，FormatValue catch 中返回 value?.ToString() 而非 "" |
| DateTimeCore/Udf | 25 | **B+** — IsoWeek net48 polyfill 正确，Easter 缺年份范围守卫（<1 或 >9999 年会抛 ArgumentOutOfRangeException） |
| RegexCore/Udf | 9 | **A** — 全局 5s 超时，每个 Regex 调用均含 TimeSpan，无遗漏 |
| JsonXmlCore/Udf | 8 | **B+** — XXE 防护完整（Prohibit DTD），JsonQuery/JsonPrettify 无 try/catch（与 JsonParse 不一致） |
| PivotCore/Udf | 4 | **B** — 正确，UDF 层缺 hasHeaders 参数暴露，CrossJoin 上限 1M 已守卫 |
| SqlCore/Udf | 3 | **B+** — 列名消毒有效，数据 INSERT 全部参数化。用户 SQL 字符串未限制（net48 下 ATTACH 可能——已知限制已文档化） |
| FileSystemCore/Udf | 22 | **B** — ValidatePath 覆盖所有 I/O 方法。**FS.NORM 异常消息泄漏沙箱根路径**（严重）。Path.GetFullPath 解析符号链接 |
| ArrayCore/Udf | 22 | **B+** — 排序/筛选/去重正确，ARR.FILL 缺数组大小上限（可能 OOM） |
| DictSetCore/Udf | 8 | **A-** — 正确，DICT.DICT 键值长度不匹配时静默截断 |
| RangeExportCore/Udf | 9 | **A-** — HTML/JSON/MD/CSV 导出正确，ToCsv 有未使用的 hasHeader 参数 |

**红线合规**：✅ 所有 Regex 调用有超时，✅ 所有文件 I/O 有 ValidatePath，✅ SQL 参数化，✅ 异常过滤器

---

## 三、发现清单

### 🔴 严重 (1)

| # | 位置 | 问题 | 根因 | 为何测试未捕获 |
|---|------|------|------|---------------|
| CR-1 | `FileSystemCore.cs:48` | **FS.NORM 异常消息泄漏沙箱根路径**。`NormalizePath` 在越界时抛出 `UnauthorizedAccessException($"Path '{p}' is outside the sandbox root '{SandboxRoot}'.")`，用户通过 `FS.NORM("C:\\Windows\\System32")` 即可探测到沙箱根目录完整路径。 | `NormalizePath` 既做路径规范化又做沙箱验证，异常消息包含了内部配置信息。 | 无针对 `NormalizePath` 异常消息内容的安全测试 |

### 🟡 高 (7)

| # | 位置 | 问题 | 影响 | 为何测试未捕获 |
|---|------|------|------|---------------|
| HI-1 | `PhyChemUdf.cs:28` | **IdealGasLaw `V()` 错误捕获 `Foundation.ExcelEmpty` 和 `null` 为「求解此变量」**。`V()` 的意图是将 `"*"` 哨兵转为 `null`（表示待求解的未知数），但同时也对 `Foundation.ExcelEmpty`、`ExcelDna.Integration.ExcelEmpty`、`null` 返回 `null`——这意味着空单元格被当作「请求解此变量」而非「缺少输入」。 | Excel 中留空的参数被静默当作 `"*"` 处理 | 测试仅验证 `"*"` 哨兵路径，未测试空单元格行为 |
| HI-2 | `LinalgUdf.cs:81` | **LINALG.IDENTITY 无尺寸上限**。`(int)InputNormalizer.ToLong(n)` 直接传给 `Identity(n)`，用户输入 1e10 会尝试分配 1e20 元素矩阵导致 OOM——但 `OutOfMemoryException` 被异常过滤器**排除**，WrapError 不会捕获，Excel 进程直接崩溃。 | 恶意或误输入可导致 Excel 崩溃 | 无超大尺寸输入测试 |
| HI-3 | `RegressionCore.cs:238-246` | **FactorImportance 遇零方差列崩溃**。标准差为零的列被填充为全零列（`sd=1` 代替 `sd=0`），导致 XtX 奇异，`XtX.Solve(Xty)` 抛 `SingularMatrixException` → `#VALUE!`。应删除零方差列或将其排名置底。 | 含常数列的数据调用 FACTORIMP 返回错误 | 测试仅用全变数列 |
| HI-4 | `RegressionCore.cs:33` | **FitOLS 缺 XtX 奇异性检测**。多重共线性（n>p 但列相关）时 `XtX.Solve(Xty)` 抛未包装异常→`#VALUE!`，错误消息不明确。 | 共线数据用户得到不明确的错误 | 测试含共线场景但仅验证抛异常，未验证消息质量 |
| HI-5 | `SqlCore.cs:26` | **net48 下 ATTACH DATABASE 未禁用**。`System.Data.SQLite` (net48) 默认允许 ATTACH，攻击者可在用户 SQL 中附加 `;ATTACH DATABASE 'path' AS evil` 将内存表数据写入文件。net8.0 的 `Microsoft.Data.Sqlite` 默认禁用 ATTACH。 | net48 用户面临数据外泄风险 | 无双 TFM 特定 SQL 安全测试 |
| HI-6 | `ArrayUdf.cs:31` | **ARR.FILL 无分配上限**。`new object[c]` 中 c 可能为极大值（用户输入 `1e10`），导致 OOM 或 CLR 未定义行为。`ARRAY.RANGE` 有 100,000 元素上限，ARR.FILL 没有。 | 恶意或误输入可导致 Excel 崩溃 | 无超大计数输入测试 |
| HI-7 | `DateTimeCore.cs:47` | **Easter 缺年份范围验证**。`new DateTime((int)y, (int)mo, (int)da)` 前未检查 y∈[1,9999]，无效年份抛 `ArgumentOutOfRangeException` → `#VALUE!`，无用户友好消息。 | 公元前年份输入得到不明确错误 | 无限定年份范围测试 |

### 🔵 中 (17 个)

| # | 位置 | 问题 | 影响 |
|---|------|------|------|
| ME-1 | `FilterUtils.cs:104-111` | ConcurrentDictionary.Clear() 竞态条件——两个线程同时触发清除 | 极低概率（需 64+ 正则模式 + 并发） |
| ME-2 | `LinalgCore.cs:36-37` | DecompCache LRU 使用 `List.Remove()` O(n) 扫描，8 条目下可忽略 | 性能影响可忽略 |
| ME-3 | `LinalgCore.cs:38` | DecompCache.GetOrAdd 使用 C-style `(T)existing` 不安全转型 | 依赖键前缀约定 |
| ME-4 | `RegressionCore.cs:190` | AnovaOneWay 使用 LINQ SelectMany.Average() 有开销 | 大组数据性能影响 |
| ME-5 | `PhyChemCore.cs:221` | GasToSTP 死代码 `(pAtm / 1.0)` | 混淆，无功能影响 |
| ME-6 | `StatsCore.cs:59,64` | Sum/Product Infinity→NaN（与 CLAUDE.md「显式守卫→抛异常」原则不一致） | 语义不一致 |
| ME-7 | `InputNormalizer.cs:227` | decimal→double 转型后未检查 Infinity（L1 守卫缺一条） | 极端大 decimal 值绕过守卫 |
| ME-8 | `StringCore.cs:96` | FormatValue catch 中返回 value?.ToString()，DateTime.MinValue 格式化为 "0001-01-01" | 对空单元格调用 STR.FORMAT 返回伪有效日期 |
| ME-9 | `JsonXmlCore.cs:20,27` | JsonQuery/JsonPrettify 无 try/catch — 与 JsonParse 不一致 | JSON 错误处理不一致 |
| ME-10 | `PivotUdf.cs:9-11` | Pivot/Unpivot/GroupBy UDF 不暴露 hasHeaders 参数 | 无表头数据被错误处理 |
| ME-11 | `DictSetCore.cs:15` | DICT.DICT 键值长度不匹配时静默以 min(n,m) 截断 | 数据静默丢失 |
| ME-12 | `RangeExportCore.cs:45` | ToCsv 的 hasHeader 参数无实际作用 | 死代码 |
| ME-13 | `InputNormalizer.cs:317` | ToDateTime 拒绝 d≤0 的 OLE 日期序列号 | OADate 0 (1899-12-30) 被静默转为 MinValue |
| ME-14 | `ComparisonUtils.cs:283-286` | IsNumeric 对 "NaN"/"Infinity" 判断与 IsNumericCell 不一致 | 比较行为边缘情况差异 |
| ME-15 | `FileSystemCore.cs:37` | FS.NORM 调用 Path.GetFullPath 访问磁盘 | 网络路径可能缓慢 |
| ME-16 | `FileSystemCore.cs:63-67` | IsPathValid 冗余预检查 | 无功能影响 |
| ME-17 | `InputNormalizer.cs:211` | ToString 对非 Range COM 对象返回类型名而非 "" | 脏字符串数据进入管道 |

### 📗 文档发现

| # | 问题 |
|---|------|
| DO-1 | **README 测试数量过时**：声明「2,790+」，实际 3,998（1,999×2 TFM） |
| DO-2 | PhyChem IdealGasLaw `"*"` 哨兵仅在 Description 中模糊提及，参数级 Description 未说明 |
| DO-3 | 分子量解析嵌套限制仅在代码注释中说明，API 参考未提及 |

---

## 四、测试质量深度评估

### 4.1 覆盖面

| 类别 | 评分 | 说明 |
|------|:--:|------|
| 正常路径 | ⭐⭐⭐⭐⭐ | 每个 Core 方法核心路径都有测试 |
| NaN/Inf/退化 | ⭐⭐⭐⭐⭐ | Linalg 每方法含独立 NaN+Inf 测试，Stats 含常数/零方差路径 |
| 错误传播链 | ⭐⭐⭐⭐ | Core guard → WrapError → ExcelError.Value 链路验证充分 |
| 边界值 | ⭐⭐⭐⭐ | 日期跨年/闰年/ISO 周边界覆盖好 |
| UDF 层兼容 | ⭐⭐⭐ | PivotUdf 测试偏形状验证而非值正确性 |
| 双 TFM 差异 | ⭐ | 零条件编译测试——IsoWeek polyfill、SQLite 提供者差异均无 TFM 特定测试 |
| 性能回归 | ⭐ | 仅 1 个测试（Regex 超时），无 O(n²)→O(n) 回归测试 |

### 4.2 已知测试缺口

1. **IsoWeek net48 polyfill** — 无双 TFM 对比测试
2. **StringCore.UrlEncode/UrlDecode** — 仅 UDF 层测试，Core 层无直接测试
3. **ArrayCore.Filter `>=`/`<=` 运算符** — 未测试
4. **PhyChemCore 转换函数长单位名** — "CELSIUS"/"FAHRENHEIT" 等未测试
5. **StatsCore.Summary 完整 9 元素验证** — 仅验证数组长度为 9
6. **Base64Encode 输出正确性** — 仅回环测试
7. **PIVOT COUNT/AVG 聚合** — UDF 层未测试
8. **DateDiff 单位代码大小写** — 未测试

### 4.3 为什么能通过全量测试

| 缺陷 | 根因 |
|------|------|
| CR-1: FS.NORM 信息泄漏 | 测试仅验证沙箱**功能**（越界抛异常），未检查**异常消息内容** |
| HI-1: V() 空单元格捕获 | 测试仅用 `"*"` 哨兵，未测试 `ExcelEmpty`/`null` 输入 |
| HI-2: IDENTITY 无上限 | 无超大输入测试 |
| HI-3: FactorImportance 崩溃 | 测试数据**不含零方差列** |
| HI-5: ATTACH DATABASE | 无 net48 特定 SQL 安全测试 |
| HI-6: ARR.FILL 无上限 | 无超大计数测试 |

**核心模式**：现有测试充分覆盖了**设计的正常行为和已知边界**，但未覆盖「未预期的输入组合」「安全测试的消息内容」「极端值的系统性测试」和「双 TFM 差异行为」。

---

## 五、高价值改善建议

按投入产出比排序：

### 5.1 立即修复（低投入、高安全/正确性）

1. **FS.NORM 异常消息去敏感化** (CR-1) — `FileSystemCore.cs:48` 改为不包含 SandboxRoot 的消息
2. **ARR.FILL / LINALG.IDENTITY 加上限** (HI-2, HI-6) — `ArrayCore.cs` 加 1,000,000 上限，`LinalgCore.cs` Identity 加 10,000 上限
3. **V() 哨兵精确化** (HI-1) — `PhyChemUdf.cs:28` 仅对 `"*"` 字符串返回 null，移除对 ExcelEmpty/null 的捕获

### 5.2 短期改善（中投入、增强鲁棒性）

4. **FactorImportance 处理零方差列** (HI-3) — 识别常数列→置排名末尾→在简化模型上回归
5. **FitOLS 共线性检测** (HI-4) — Solve 前检查 XtX 条件数或秩
6. **DateTimeCore.Easter 年份守卫** (HI-7) — `y < 1 || y > 9999` 前置检查
7. **修复 decimal→double Infinity 守卫** (ME-7)

### 5.3 中期增强（提升测试与文档）

8. **双 TFM 差异测试** — IsoWeek net48 vs net8.0 基准值对比、SQLite ATTACH 禁用验证
9. **性能回归测试** — Mode O(n²)→O(n) 验证、RegexMatch n=1 快路径验证
10. **补齐测试缺口** — 约 10 个缺失测试，预计增加 30-50 个测试用例
11. **README 测试数字更新** — 2,790 → 3,998

### 5.4 长期优化（架构增强）

12. **DecompCache 使用 LinkedList** 实现 O(1) LRU
13. **PivotUdf 暴露 hasHeaders 参数**
14. **SqlCore net48 ATTACH 禁用**
15. **FS.NORM 纯字符串路径规范化** — 实现不访问磁盘的 `..`/`.` 解析

---

## 六、CLAUDE.md 红线规则合规

| 规则 | 状态 | 备注 |
|------|:--:|------|
| 接口兼容 — 不修改 Public 签名/返回值 | ✅ | 无违规 |
| 双 TFM 兼容 — `#if NET48` 仅限内部实现 | ✅ | SqlCore 条件编译仅在 using 别名级别 |
| 静默传播阻断 — 显式守卫 NaN/Inf/null | ✅ | 三层防护（InputNormalizer L1 + NumericGuard + AnalyticsHelpers） |
| 防御完整性 — 安全机制覆盖所有方法 | ✅ | ValidatePath 全部 14 个方法、Regex 超时全覆盖、SQL 参数化 |
| 异常过滤器 — `catch when(...)` 排除致命异常 | ✅ | `grep -rn "catch\s*{" src/` 返回空 |
| 表头行契约 — `hasHeaders=true` 默认 | ✅ | 除 CrossJoin 豁免外全部遵守 |
| 哨兵契约 L1-L5 — 类型转换分层 | ✅ | 已知限制 L4（0/false 歧义）已文档化 |

---

## 七、最终结论

本项目是一个**工程纪律良好**的 C# Excel 加载项项目。核心架构（三层分离、MapOver 调度、WrapError 边界）设计合理且执行一致。安全防护（沙箱、超时、参数化）完整但有 1 个信息泄漏需修复。测试覆盖广泛（3,998 测试）但双 TFM 差异测试空白值得补齐。

**推荐行动**：
1. ✅ 立即修复 CR-1（FS.NORM 信息泄漏）— 1 行改动
2. ✅ 短期内修复 HI-1~HI-7 — 估计 2-3 小时工作量
3. ✅ 补齐双 TFM 差异测试 — 预防未来回归
4. ✅ 更新 README 测试数字 — 2,790 → 3,998
5. 📋 中低优先级改善列入 backlog

**综合评级：B+ (良好-优秀)** — 可安全分发使用，建议在下次发布前修复严重和高优先级问题。

---

> **审查方法**：5 个专业审查代理（C# 代码审查 ×3 层、安全审查、静默故障猎手、测试质量分析）并行深读全部 40 个源码文件 + 37 个测试文件，辅以 CodeGraph 调用链分析、`dotnet test` 全量验证、`verify-docs.sh` 文档一致性检查、历史 diff 追踪。
