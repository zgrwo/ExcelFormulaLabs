---
name: excel-dna-project
description: 本项目编码规范与架构参考 — Foundation/Analytics/DataToolkit 模块职责、MapOver 变体行为差异、UDF 调用链、测试模式。Use when modifying code in this project, understanding module responsibilities, or writing new UDFs.
---

# SKILL.md

> 术语定义见 [context.md](../../docs/context.md)。

## 项目结构

> 完整目录树、文件清单及路径路由见 **[CLAUDE.md § 仓库目录树](../../CLAUDE.md#仓库目录树)**（唯一信源）。以下为架构分层摘要：

```
src/
├── Foundation/      InputNormalizer, ElementWiseMapper, OutputWrapper,
│                    FilterUtils, ArrayOperations, ComparisonUtils, DictOperations,
│                    ExcelEmpty, ExcelError
├── Analytics/       StatsCore, LinalgCore, RegressionCore, PhyChemCore
│                    + StatsUdf, LinalgUdf, RegressionUdf, PhyChemUdf
│                    + Analytics-AddIn (.xll 产出)
└── DataToolkit/     StringCore, DateTimeCore, RegexCore, JsonXmlCore, PivotCore,
                     SqlCore, FileSystemCore, ArrayCore, DictSetCore, RangeExportCore
                     + 各 Udf 类 + DataToolkit-AddIn (.xll 产出)

tests/
├── Foundation.Tests/
├── Analytics.Tests/    Core + UDF 双重测试 + Python 交叉验证
└── DataToolkit.Tests/  Core + UDF 双重测试 + IntegrationPipelineTests

docs/
├── api-reference.md   UDF 签名（数字的唯一信源）
├── user-manual.md     每个 UDF 的详细示例（全函数覆盖）
└── context.md          领域术语表
```

## 架构

> 详见 [CLAUDE.md § 架构分层](../../CLAUDE.md#架构分层)。

### MapOver 选型

| 场景 | 用什么 | 形状 | 错误处理 |
|------|--------|------|----------|
| 单参数，保持输入形状 | `MapOver<TIn,TOut>` | 标量→标量，1D→1D，2D→2D | null/error/empty 透传 |
| 单参数，强制 1D 输出 | `MapOverFlat<TIn,TOut>` | 始终 `object[]` | null/error/empty 透传 |
| 2-3 参数，广播 | `MapOverMulti<T1,T2,TOut>` | 标量广播到数组尺寸 | 尺寸不匹配→`ExcelError.Value` |

部分统计 UDF（CVP/CV/PEAR/SPR/T1/T2）绕过 MapOver，用 `V()`/`M()` 直接调 Core。尺寸不匹配→`NaN`（非 ExcelError），`V(null)`→空数组→`NaN`。

### UDF 声明规范

所有 UDF 遵循统一声明模式。函数覆盖 6 种调度变体，以下是完整模板。

#### 基础模板（单参数 MapOver）

```csharp
[ExcelFunction(Name = "CATEGORY.NAME", Description = "一句话英文说明。")]
public static object UDF_CAT_NAME(
    [ExcelArgument(Name = "param_name", Description = "What this parameter is.")]
    object input
) => OutputWrapper.WrapError(() =>
    ElementWiseMapper.MapOver<TIn, TOut>(input, SomeCore.Method));
```

#### 完整声明要素

| 要素 | 要求 | 示例 |
|------|------|------|
| `[ExcelFunction]` Name | `CATEGORY.NAME` — 大写类别前缀，点号分隔 | `STR.REVERSE`, `LINALG.SVD_U` |
| `[ExcelFunction]` Description | 一句英文，描述函数做什么 | `"Reverse a text string."` |
| 返回类型 | **始终** `object`（Excel 可消费） | — |
| `[ExcelArgument]` Name | `snake_case`，Excel IntelliSense 显示 | `num_chars`, `match_case` |
| `[ExcelArgument]` Description | 英文短语，解释参数含义 | `"Number of characters to generate."` |
| 错误包装 | `OutputWrapper.WrapError(...)` 包裹所有逻辑 | 异常 → `#VALUE!` |
| 方法名 | `UDF_<CATEGORY>_<SHORTNAME>` | `UDF_STR_REV`, `UDF_RX_MATCH` |

#### 可选参数

```csharp
[ExcelArgument(Name = "[param_name]", Description = "...")]
//                  ^                ^
//          开方括号，IntelliSense 视觉提示  闭方括号，IntelliSense 视觉提示
object param = null   // ← 默认值 null，由 InputNormalizer 处理
```

- Name 以 `[` `]` 包裹 → Excel 函数向导显示为可选
- 默认值统一 `= null`（不用 `ExcelMissing` — InputNormalizer 通过反射检测）
- Core 层接收已转换的类型（`long`/`bool`/`string`），不接收 `object`

#### 6 种调度模式

```csharp
// ① 标准 MapOver — 单参数，保持输入形状
=> OutputWrapper.WrapError(() =>
    ElementWiseMapper.MapOver<string, string>(input, StringCore.ReverseString));

// ② MapOverFlat — 单参数，强制 1D 输出
=> OutputWrapper.WrapError(() =>
    ElementWiseMapper.MapOverFlat<string, string>(input, StringCore.SomeMethod));

// ③ MapOverMulti — 2-3 参数广播，尺寸不匹配 → ExcelError.Value
=> OutputWrapper.WrapError(() =>
    ElementWiseMapper.MapOverMulti<string, string, bool>(t, p,
        (text, prefix) => StringCore.StartsWithStr(text, prefix)));

// ④ Analytics: M()/V()/D() — 矩阵/向量/2D 准备 + 直接 Core 调用
private static double[,] M(object d) => AnalyticsHelpers.PrepM(d);
private static double[] V(object d) => AnalyticsHelpers.PrepV(d);
private static object[,] D(object d) => InputNormalizer.NormalizeTo2D(d)!;
// 用法:
=> OutputWrapper.WrapError(() => LinalgCore.Determinant(M(data)));

// ⑤ 标量 UDF（无 MapOver）— 零或极少参数
=> OutputWrapper.WrapError(() => (object)StringCore.UUID());

// ⑥ 自定义调度 — 绕过 MapOver，手动处理数组展开
// 仅 STATS.CVP/COVAR/PEAR/SPR/TTEST1/TTEST2 使用
=> OutputWrapper.WrapError(() => (object)StatsCore.CovarianceP(V(input1), V(input2)));
```

#### 描述编写规范

| 位置 | 语言 | 规范 |
|------|------|------|
| `[ExcelFunction]` Description | **英文** | 动词开头，≤80 字符 |
| `[ExcelArgument]` Description | **英文** | 名词短语，描述参数含义 |
| api-reference.md 说明 | **中文** | 用户面向文档，可包含对标 Excel 函数信息 |

```csharp
// ✅
[ExcelFunction(Name = "STR.REVERSE", Description = "Reverse a text string")]

// ❌ — 描述不应含中文、不应含参数细节
[ExcelFunction(Name = "STR.REVERSE", Description = "反转字符串，逐字符反转")]
```

---

## 预防规则

### 1. 静默传播阻断

WrapError 只捕获**异常**，不捕获 NaN/Inf/null/default!。依赖 IEEE 754 传播 = bug。

```csharp
// ❌ tss=0 → 0/0=NaN → 静默返回
double r2 = 1.0 - sse / tss;

// ✅ 显式 guard → 抛异常 → WrapError → #VALUE!
if (Math.Abs(tss) < 1e-15)
    throw new ArgumentException("Total sum of squares is zero.");
double r2 = 1.0 - sse / tss;
```

```csharp
// ❌ 转换失败返回 default!（null/0），WrapError 不触发
catch { return default!; }

// ✅ 致命异常穿透
catch (Exception ex) when (ex is not OutOfMemoryException
    and not StackOverflowException) { return default!; }
```

```csharp
// ❌ 分母为零 → Infinity 静默返回
return n * r * t / v;

// ✅ 匹配模块风格显式返回 NaN
return v == 0 ? double.NaN : n * r * t / v;
```

### 2. 防御完整性

安全机制必须覆盖模块内**所有**相关方法。新方法 → 对照同模块已有方法补齐防护。

```csharp
// ❌ FileExists/GetFileSize/FolderExists 漏了 ValidatePath
internal static bool FileExists(string p) => File.Exists(p);

// ✅ 对照 ReadTextFile/WriteTextFile/DeleteFile 等补齐
internal static bool FileExists(string p) { ValidatePath(p); return File.Exists(p); }
```

| 机制 | 检查方式 |
|------|----------|
| Sandbox | `grep "ValidatePath"` vs `grep "internal static"` |
| Regex 超时 | 所有 `Regex.Match/Replace/IsMatch` 带 `TimeSpan` |
| SQL 参数化 | 无字符串拼接 SQL |

### 3. 异常过滤器统一

裸 `catch{}` = bug。模板：

```csharp
// 通用
catch (Exception ex) when (ex is not OutOfMemoryException
    and not StackOverflowException)

// COM interop（InputNormalizer.TryExtractComRangeValue）
catch (Exception ex) when (ex is not OutOfMemoryException
    and not StackOverflowException and not AccessViolationException)
```

自检：`grep -rn "catch\s*{" src/ --include="*.cs" | grep -v obj/` → 必须返回空。

### 4. 重载不回退

旧方法 delegate 到新增重载时，默认/常见路径的**性能**必须保持原有水平。新重载增加通用性时，不能把旧方法的优化快路径退化到通用慢路径。

```csharp
// ❌ 旧 RegexMatch 用 Regex.Match() 只扫第一个匹配即返回
//    新版 delegate 到 RegexMatch(i,p,1,ic)，内部用 Regex.Matches() 扫全部匹配
internal static string RegexMatch(string i, string p, bool ic=true)
    => RegexMatch(i, p, 1, ic);  // 若新版内无 n==1 快路径 → O(n) 回退

// ✅ 新版对默认值 n=1 保留 Regex.Match() 优化快路径
internal static string RegexMatch(string i, string p, long n, bool ic=true)
{
    if (n == 1) { var m = Regex.Match(i, p, F(ic), Timeout); return m.Success ? m.Value : ""; }
    var mc = Regex.Matches(i, p, F(ic), Timeout); // 仅 n≠1 走全扫描
    ...
}
```

自检：新增重载时，走一遍默认参数路径，确认底层 API 与旧版一致（`Match()` vs `Matches()`、`O(1)` vs `O(n)`、`foreach` lazy vs `.Count` 全评估）。

---

## 测试

### 全量测试（必须）

```bash
bash scripts/verify-docs.sh                          # ① 文档一致性验证
dotnet test                                         # ② 全量单元测试
dotnet test --filter "CrossVal"  # ③ 交叉验证（Analytics Python 交叉验证）
python scripts/verify-manual.py                      # ④ 手册示例验证（全 UDF × Python）
dotnet build -c Debug && dotnet build -c Release     # ⑤ 双目标打包
```

精度 1e-10。豁免：FS.*（POSIX 差异）、RANGE.*（无标准输出格式），标记 `// No Python ref:`。

### 快捷命令

```bash
dotnet build && dotnet test
dotnet test --filter ClassName
dotnet test --no-build -v q
```

### 测试模式

xUnit `[Fact]` + FluentAssertions 6.12.0。每 Core 方法覆盖：正常路径 + 退化输入（空/null/零/单元素/全等值/边界值）。UDF 层测试覆盖：Core guard → `WrapError` → `ExcelError.Value` 链路。

```csharp
// Core 层
[Fact] public void FitOLS_constant_y_throws()
{
    var act = () => RegressionCore.FitOLS(X, new[] { 5.0, 5, 5 });
    act.Should().Throw<ArgumentException>().WithMessage("*constant*");
}

// UDF 层
[Fact] public void OLS_constant_y_returns_error()
{
    RegressionUdf.UDF_REGRESS_OLS(X, new[] { 5.0, 5, 5 })
        .Should().Be(ExcelError.Value);
}
```

---

## 参考

### InputNormalizer

| 方法 | 行为 |
|------|------|
| `NormalizeTo1D(null)` | `new object[0]` |
| `NormalizeTo2D(null)` | `null` |
| `ToDouble` 非数值 | `NaN` |
| `ToLong` 非数值 | `0` |
| `ToBool` 非数值 | `false` |
| `ToDateTime` 非数值 | `DateTime.MinValue` |
| `ToDateTime` 数值 | OLE 日期（纪元 1899-12-30） |

### 已知限制

- **MathNet 5.0.0**：QR 不支持宽矩阵 m<n → 已用零填充方阵提取子矩阵
- **FileSystem 测试**：依赖真实文件系统，沙箱测试通过 `[Collection("Sandbox")]` 序列化
- **双 TFM 差异**：net48 用 `System.Data.SQLite`，net8.0 用 `Microsoft.Data.Sqlite`；IsoWeek 在 net48 手工 polyfill

### 已解决的限制

- MathNet Solve 奇异矩阵可能不抛异常 → RegressionCore 已加显式 guard
- 全项目裸 catch{} 吞致命异常 → 已统一加 when 过滤器
- FileSystem FileExists/FolderExists 沙箱旁路 → 已补 ValidatePath

### 历史修复

**正确性**：StdevP/CovarianceP MathNet 5.0 破性变更 · LU P 矩阵排列循环 · QR 宽矩阵零填充 · Stats.Mode O(n²)→O(n) · PhyChem LB 常数精度 · DateDiff 闰年 DayOfYear→Month/Day · ValidatePath "." 规范化 · RegressionCore 除零 guard ×5

**安全**：FileSystem 沙箱 FileExists/FolderExists/GetFileSize 补齐 · Regex 全局 Timeout · 全项目 18 处裸 catch → when 过滤器 · IdealGasLaw 零分母 guard

**性能**：StringCore.RandomString ThreadLocal/Random.Shared · SqlCore 列类型推断扫描前 10 行 · RemoveChars StringBuilder 单趟 · RegexMatch/Replace n=1 保留 Match() 快路径（防 delegate→Matches() 回退）

**构建**：多目标 net8.0+net48 · DataToolkit .dna 双模板 · CleanupDnaAfterBuild 防增量污染 · SandboxRoot 并行测试 xUnit Collection 序列化

## 哨兵契约 L1-L5

> 规则声明见 [CLAUDE.md §5](../../CLAUDE.md#5-哨兵契约inputnormalizer-l1-l5)。本表为实施细节。

| 层级 | 职责 | ✅ DO | ❌ DON'T | 违反后果 |
| :--- | :--- | :--- | :--- | :--- |
| **L1 守卫** | 类型转换前 | 显式 `double.IsNaN(d)` / `double.IsInfinity(d)` | 依赖 CLR 未定义行为（如 `(long)NaN`） | 未定义行为 / 静默损坏 |
| **L2 哨兵** | 不可转换值 | 返回类型零值哨兵：`double`→`NaN`、`long`→0、`int`→0、`bool`→`false`、`DateTime`→`MinValue`、`string`→`""` | 抛异常或返回非零值 | 调用方误判 |
| **L3 Excel 信号** | `null`/`DBNull`/`ExcelEmpty`/`ExcelError`/`ExcelMissing` | 返回哨兵（语义：无有效值，跳过） | 抛异常或静默赋值 | Excel 交互异常 |
| **L4 已知取舍** | `long`/`bool` 哨兵（0/false）与真实值不可区分 | 文档说明；调用方前置 `IsNumericCell` | 依赖「0/false 表示错误」语义 | 数据误判 |
| **L5 最终边界** | `ConvertValue<T>` 未知类型 `Convert.ChangeType` 失败 | 已知 6 类委托 `InputNormalizer.ToXxx`；新类型：`double`→`NaN`，其余**必须 `throw`** | `return default(T)` 静默替代 | 脏数据传播 |

## 验证脚本行为一致性

> 规则声明见 [CLAUDE.md §6](../../CLAUDE.md#6-验证脚本行为一致性verify-manualpy--源码)。本表为实施对照。

验证脚本必须模拟与 C# 源码**完全一致的行为**——相同模型参数、相同算法变体、相同转换常量。

| 维度 | ✅ DO | ❌ DON'T | 违反后果 |
| :--- | :--- | :--- | :--- |
| **模型结构** | sklearn `fit_intercept` 与 C# 是否添加截距列一致 | C# 无截距但 sklearn 加了截距 | 回归验证完全无效（已发生：Issue #15） |
| **算法参数** | scipy 参数与 MathNet 语义一致（`bias=False`, `fisher=True`） | 使用 scipy 默认参数而 MathNet 不同 | KURT/SKEW 值偏差 |
| **转换常量** | Python 使用与 C# `PhyChemCore` 相同的转换因子 | Python 用 14.6959 而 C# 用 `101325/6894.76` | PHYCHEM 边界不一致 |
| **空值语义** | Python 空值处理与 C# `InputNormalizer` 哨兵契约一致 | scipy `mode()` 返回首个值但 C# 返回 `NaN` | 边界用例验证失效 |

> **自检**：新增/修改 UDF 后，确认 `verify-manual.py` 中的 Python 调用与 C# 源码参数、模型结构、转换常量逐项一致。
