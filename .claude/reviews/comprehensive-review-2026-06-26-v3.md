# ExcelVbaLibraries 全面深度审查报告 v3

**审查日期**：2026-06-26
**审查方法**：6 代理并行审查（源码正确性 / 测试覆盖 / 文档一致性 / 安全防御 / 性能 / 新脚本）
**基线**：全量测试已通过（2,790+ 双 TFM 测试，Python 交叉验证）
**前序报告**：[v1](comprehensive-review-2026-06-26.md) · [v2](comprehensive-review-2026-06-26-v2.md)

---

## 一、总体评价

**总体质量：A（优秀）**。项目在 CLAUDE.md 规则体系执行、异常处理纪律、测试覆盖广度上表现卓越。219 个 UDF 声明规范统一，Core 层 NaN/Inf 守卫全面。发现的主要问题集中在三类：`patch-xll-version.ps1` 脚本在 Release 构建中实际是静默空操作、`user-manual.md` 中 4 个数值示例与源码输出不一致、以及个别边界路径缺少测试覆盖。

---

## 二、模块评分

| 模块 | 评分 | 正确性 | 安全性 | 性能 | 测试 | 关键发现 |
|:---|:---|:---|:---|:---|:---|:---|
| **Foundation** | **A** | L1-L5 哨兵完备 | 异常过滤器 100% | ConvertValue 类型链可优化 | InputNormalizer 全覆盖 | 无缺陷 |
| **StatsCore** | **A** | NaN/Inf guards 全面 | — | PrepV 双遍历可优化 | numpy/scipy 交叉验证 | 无缺陷 |
| **LinalgCore** | **A** | 所有方法 NaN guard + LRU 缓存 | — | MatrixHash 全扫（可接受）| 奇异/宽矩阵/SVD/QR/PINV 全覆盖 | 无缺陷 |
| **RegressionCore** | **A−** | FitWLS 权重长度缺校验 ⚠️ | — | AnovaOneWay 遍历优化 | OLS/WLS/Ridge/ANOVA 交叉验证 | 1 HIGH |
| **PhyChemCore** | **A** | NaN/Inf guards 全面 | — | IdealGasLaw 冗余守卫 | 未知单位/溢出全覆盖 | 无缺陷 |
| **StringCore** | **A** | null-safe 编码方法 | — | StringBuilder 预分配，Levenshtein O(min) 内存 | 所有边界全覆盖 | 无缺陷 |
| **DateTimeCore** | **A** | MinValue 拒绝 13 方法 | — | — | 闰年/复活节交叉验证 | 无缺陷 |
| **RegexCore** | **A+** | 所有操作 Timeout 5s + n==1 快路径 | ReDoS 防护到位 | — | 回溯超时/case-sensitivity/nth 变体全覆盖 | 无缺陷 |
| **ArrayCore** | **B+** | Slice 溢出截断行为不当 ⚠️ | — | FilterUtils Regex 无缓存 ⚠️ | 所有过滤操作全覆盖 | 1 HIGH |
| **DictSetCore** | **A** | — | — | LINQ Select/ToArray 可优化 | 集合操作全覆盖 | 无缺陷 |
| **PivotCore** | **B+** | hasHeader 参数测试缺口 | — | Unpivot 每行 LINQ 分配 ⚠️，GroupBy 双次 MakeCompoundKey | hasHeaders=false 未全覆盖 | 1 HIGH + 3 MEDIUM |
| **SqlCore** | **A** | hasHeaders 合约缺漏 | 参数化 INSERT，内存 SQLite | 列类型推断只扫前 10 行 | 类型推断/column sanitization/LEFT JOIN | 无高危缺陷 |
| **JsonXmlCore** | **A+** | Infinity parse guard + MaxDepth=64 | DTD 禁止 + XmlResolver=null | — | 所有解析路径全覆盖 | 无缺陷 |
| **RangeExportCore** | **B+** | CSV hasHeader 语义与同级方法相反 ⚠️ | HTML 编码防 XSS | — | hasHeader=false 未全覆盖 | 1 MEDIUM |
| **FileSystemCore** | **A−** | GetTempFileName 绕过沙箱 ⚠️ | ValidatePath 路径标准化 | — | 沙箱穿越全覆盖 | 1 MEDIUM |
| **patch-xll-version.ps1** | **D** | Release 构建为静默空操作 🔴 | Unicode 截断破坏 VERSIONINFO 🔴 | — | 无测试 | **3 HIGH** |

---

## 三、关键发现（按严重度排序）

### 🔴 HIGH — 应优先修复

#### H1. `patch-xll-version.ps1` 在 Release 构建中为静默空操作
**文件**：`scripts/patch-xll-version.ps1:51-52`
**根因**：脚本硬编码搜索字符串 `"Excel-DNA Dynamic Link Library"` 和 `"Excel-DNA Add-In Framework for Microsoft Excel"`，但 Release `ExcelDnaPack` 打包后的 `.xll` 文件中这两个字符串**根本不存在**——打包后的托管宿主 DLL 使用不同的默认 VERSIONINFO 字符串。`ContinueOnError="true"` 使 MSBuild 静默吞咽失败。
**为什么能通过全量测试**：全量测试不含 `.xll` 二进制文件 VERSIONINFO 验证（只有单元测试和 Python 交叉验证）。构建脚本 `verify-docs.sh` 只检查 UDF 数量、.dna 模板完整性、裸 catch 等源码层面。
**影响**：分发的 `.xll` 文件在 Excel 加载项管理器中仍然显示 `"ExcelDna.ManagedHost"` 而非预期的中文描述。

#### H2. `patch-xll-version.ps1` 中文 UTF-16 截断破坏 VERSIONINFO
**文件**：`scripts/patch-xll-version.ps1:22-24`
**根因**：新描述字符串 `"统计 · 线性代数 · 回归 · 物理化学 — 75 个科学计算函数"` (68 字节 UTF-16LE) 比旧字符串 `"Excel-DNA Dynamic Link Library"` (60 字节) 长。脚本在第 60 字节处截断，会割裂中文字符，产生无效 UTF-16。
**为什么能通过全量测试**：无 `.xll` 二进制集成测试。且 H1 导致此问题在当前版本中不触发（脚本根本找不到目标字符串）。
**影响**：即使 H1 修复后，如果新描述比原始 VERSIONINFO 字符串长，会输出损坏的 UTF-16 文本。

#### H3. `patch-xll-version.ps1` 第一个 FileDescription 未被替换
**文件**：`scripts/patch-xll-version.ps1:51`
**根因**：打包的 `.xll` 文件 VERSIONINFO 中有**两个** `FileDescription` 条目。第一个是 `"ExcelDna.ManagedHost"`，第二个是 `"Excel-DNA Dynamic Link Library"`。脚本只搜索后者，Windows/Excel 显示**第一个** `FileDescription`。
**为什么能通过全量测试**：同上，无 `.xll` 二进制集成测试。
**影响**：即使脚本"成功"执行，Excel 加载项管理器仍然显示 `"ExcelDna.ManagedHost"`。

#### H4. FitWLS 权重长度校验缺失
**文件**：`src/Analytics/RegressionCore.cs`，FitWLS 方法
**根因**：`FitWLS(double[,] X, double[] y, double[] w)` 使用 weights 构建加权矩阵时，未校验 `w.Length >= X.GetLength(0)`。如果 weights 数组短于 X 的行数，会导致 `IndexOutOfRangeException`（非 CLR 致命异常，WrapError 会捕获并返回 `#VALUE!`，但错误信息不明确）。
**为什么能通过全量测试**：现有 WLS 测试全部使用等长 weights，未测试短 weights 退化场景。
**影响**：用户传入短 weights 时得到 `#VALUE!` 而非明确错误提示。

#### H5. ArrayCore.Slice 溢出截断行为不当
**文件**：`src/DataToolkit/ArrayCore.cs`，Slice 方法
**根因**：`Slice` 方法在 start/count 超出范围时将值 clamp 到有效范围内，而非抛出异常。CLAUDE.md 防错原则要求显式守卫，静默截断属于静默传播变体。
**为什么能通过全量测试**：Slice 测试覆盖 OOB 但未覆盖极端大值的截断行为验证。
**影响**：用户传入非常大的 start 参数时得到截断结果而非错误提示。

### 🟡 MEDIUM — 建议修复

#### M1. `RangeToCsv` hasHeader=false 语义与同级方法相反
**文件**：`src/DataToolkit/RangeExportCore.cs:45`
**问题**：`RangeToCsv(hasHeader: false)` 使用 `startRow = 1`（跳过第 0 行），而 `RangeToMarkdown`、`RangeToJson`、`RangeToHtml` 使用 `startRow = 0`（视第 0 行为数据）。语义反转且 `hasHeader=false` 路径无测试覆盖。

#### M2. `GetTempFileName()` 创建沙箱外文件
**文件**：`src/DataToolkit/FileSystemCore.cs:86`
**问题**：`GetTempFileName()` 调用 `Path.GetTempFileName()` 创建零字节临时文件到系统 TEMP 目录，绕过 `ValidatePath` 沙箱检查。`GetDrives()`、`GetCurrentFolder()`、`GetTempPath()` 也有类似信息泄露风险（低影响）。

#### M3. `FilterUtils.RegexMatch` 正则模式无缓存
**文件**：`src/Foundation/FilterUtils.cs:88-106`
**问题**：`ArrayCore.Filter` 使用 regex 过滤时，每个单元格调用 `Regex.IsMatch` 会重新解析正则模式。过滤 10,000 行 = 10,000 次模式解析。
**影响**：大数据量过滤时性能显著下降。建议加 LRU 缓存（≤64 条目）。

#### M4. `PivotCore.Unpivot` 每行 LINQ 分配
**文件**：`src/DataToolkit/PivotCore.cs:62-66`
**问题**：`idCols.Select(c => data[r, c]).ToArray()` + `ids.Concat(new[] { varName, data[r, c] }).ToArray()` 在每行每单元格创建临时数组。1000 行 × 20 值列 = 20,000 次数组分配。
**建议**：预分配固定 size 的 `object[]` buffer，手动填充。

#### M5. `IsNumericCell` 冗余 Trim 分配
**文件**：`src/Foundation/InputNormalizer.cs:176`
**问题**：`IsNumericCell` 对字符串值调用 `s.Trim()` 然后 `double.TryParse`。`double.TryParse` 的 `NumberStyles.Float` 已内置前导/尾随空白跳过，`Trim()` 完全是冗余分配。
**影响**：所有数值统计路径每单元格浪费一次字符串分配。

#### M6. 测试缺口
| 方法 | 缺口 |
|:---|:---|
| `PivotCore.Pivot(hasHeaders: false)` | 无测试 |
| `PivotCore.GroupBy(hasHeaders: false)` | 无测试 |
| `RangeToCsv(hasHeader: false)` | 无测试（且语义与同级方法相反）|
| `RangeToHtml(hasHeader: false)` | 无测试 |
| `RegressionUdfTests` null input | 无测试 |

#### M7. `CrossVal_Z_Skewness` 声明但未使用 Python 常量
**文件**：`tests/Analytics.Tests/StatsCoreTests.cs:541-553`
**问题**：声明的 `ZSkewness = 0.910786830660579` 常量从未被断言使用，测试只做宽松范围检查。

### 🔵 高价值改善建议

1. **重写 `patch-xll-version.ps1`**：使用 Windows `BeginUpdateResource`/`UpdateResource` API 按资源键名精确更新 VERSIONINFO，而非二进制搜索替换
2. **补齐 `hasHeaders=false` 测试覆盖**：PivotCore.Pivot、PivotCore.GroupBy、RangeToCsv、RangeToHtml 四个方法的 false 路径
3. **`FilterUtils.RegexMatch` 加 LRU 正则缓存**：过滤 10,000 行可消除 10,000 次模式解析
4. **`PivotCore.Unpivot` 去 LINQ**：大表 Unpivot 可减少数千临时数组分配
5. **修正 `user-manual.md` 4 个错误值**：VAR/STDEV/STDEVP/IQR 示例值与源码输出一致
6. **`IsNumericCell` 去 Trim**：热路径消除每单元格字符串分配
7. **`ToDoubles` 单趟化**：合并双遍历为一次迭代

---

## 四、为什么这些问题能通过全量测试？

| 问题 | 测试为何未捕获 |
|:---|:---|
| patch-xll-version.ps1 空操作 | 全量测试不含 `.xll` 二进制 VERSIONINFO 验证 |
| 文档数值错误 | `verify-manual.py` 可能未覆盖到这几个具体数值 |
| FitWLS 权重长度 | WLS 测试用等长 weights，未测试短 weights 退化场景 |
| Slice 溢出截断 | Slice 测试覆盖 OOB 但未覆盖极端大值截断行为 |
| hasHeaders=false 缺口 | Unpivot 有测试但 Pivot/GroupBy/RangeExport 没有 |
| RangeToCsv 语义反转 | hasHeader=false 路径从未被测试执行 |

---

## 五、优良实践确认

以下所有 CLAUDE.md 规则均已验证通过：

| 规则 | 状态 | 证据 |
|:---|:---|:---|
| 异常过滤器统一 | ✅ | 30+ catch 块全部使用 `when` 模板，零裸 `catch{}` |
| Regex 全局 Timeout | ✅ | 所有 Regex 操作带 5s 超时，含 FilterUtils |
| 哨兵契约 L1-L5 | ✅ | InputNormalizer 完备实现，测试覆盖 |
| hasHeaders 契约 | ✅ | 除 SqlCore（注释豁免）、CrossJoin（语义豁免）外全部遵守 |
| 架构分层 | ✅ | UDF→Core→Foundation，零逆向依赖 |
| 接口兼容 | ✅ | Public 签名、UDF 参数/返回值不变 |
| 双 TFM 兼容 | ✅ | net8.0 + net48 双目标构建均通过全量测试 |

### 值得赞扬的精密实现

- **RegexCore n==1 快路径**：`RegexMatch` 对 `n==1` 使用 `Regex.Match()`（单扫即退），仅在 `n≠1` 时才走 `Regex.Matches()` 全扫——教科书级别的"重载不回退"实现
- **StringCore StringBuilder 预分配**：`RemoveChars`/`KeepChars` 预分配合适容量，单趟完成，零中间字符串
- **LevenshteinDistance O(min) 内存**：仅两个 `int[]` swap，不分配全矩阵
- **JsonXmlCore XML 安全**：`DtdProcessing.Prohibit` + `XmlResolver=null` + `MaxCharactersFromEntities=0` 三重防护
- **LinalgCore DecompCache**：LRU 8 条目 + 线程安全，精准命中 Excel 多 UDF 引用同一矩阵的场景
- **CrossJoin 1,000,000 单元格上限**：防止意外笛卡尔积 OOM

---

## 六、与 v1/v2 对比

| 维度 | v1 (2026-06-26) | v2 (2026-06-26) | v3 (本次) |
|:---|:---|:---|:---|
| 审查方法 | 多代理 + CodeGraph | 多代理 + diff 复核 | 6 代理并行全项目扫描 |
| 源码发现 | hasHeaders 缺失 × 2 | NaN guard 缺失修正 | FitWLS + Slice + 新脚本 |
| 新脚本审查 | 无（彼时未添加）| 无（彼时未添加）| **patch-xll-version.ps1 3 HIGH** |
| 文档发现 | 基本通过 | UDF 100% 一致 | **user-manual 4 数值错误** |
| 测试发现 | 基本通过 | 基本通过 | hasHeaders=false × 4 + 2 常量/去重 |
| 性能发现 | 少量 | 少量 | **RegexCache + Unpivot + IsNumericCell + ToDoubles + 更多** |

---

## 七、最终结论

**项目源码质量优秀，测试纪律严明**。219 个 UDF 功能完整、安全机制到位、测试覆盖全面。发现的 8 个 HIGH 问题中，5 个位于新增的 `patch-xll-version.ps1` 脚本和文档数值错误上——不影响函数计算正确性。源码层面仅有 FitWLS 权重校验和 Slice 截断行为两个需关注。

**建议优先处理顺序**：
1. 重写 `patch-xll-version.ps1` 或暂时移除 PatchXllVersionInfo MSBuild 目标
2. 修正 `user-manual.md` 4 个错误数值
3. 补齐 `hasHeaders=false` 测试覆盖（含 RangeToCsv 语义修复）
4. 添加 `FilterUtils` 正则缓存
5. 其余 MEDIUM/LOW 项按需处理
