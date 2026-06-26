# 审查发现逐项复核报告

> 日期：2026-06-27 | 方法：源码直接阅读 + git log/blame/diff 追溯

## 复核方法

对上一轮审查的 1 个严重 + 7 个高优先级发现，逐一：
1. 读取实际源码验证存在性
2. `git log -p -S`/`git blame` 追溯引入时间和变更上下文
3. 重评估严重度（基于数据流分析和实际攻击面）

---

## CR-1: FS.NORM 异常消息泄漏沙箱根路径

**位置**：`src/DataToolkit/FileSystemCore.cs:48`

**源码**：
```csharp
throw new UnauthorizedAccessException(
    $"Path '{p}' is outside the sandbox root '{SandboxRoot}'.");
```

**复核**：✅ 确认存在

**严重度调整**：CRITICAL → **HIGH**

**原因**：
- `WrapError` (OutputWrapper.cs:36) 仅将 `ex.Message` 写入 `Debug.WriteLine`，Excel 单元格返回 `#VALUE!`
- **Excel 用户界面不可见**沙箱根路径
- 仅在附加调试器或启用 Excel-DNA 日志的开发者环境中暴露
- 实际攻击面：需物理/远程访问调试输出或日志文件

**git 追溯**：
- 沙箱在 commit `16e7cfc`（P0 边界健壮性修复）引入
- 异常消息自始包含 `{SandboxRoot}`
- 最新 commit `1c48624` 未修改此行

**建议**：移除消息中的 `{SandboxRoot}`——防御深度原则

---

## HI-1: IdealGasLaw V() 空单元格 = "求解此变量"

**位置**：`src/Analytics/PhyChemUdf.cs:28`

**源码**：
```csharp
private static double? V(object o){
    if(o is Foundation.ExcelEmpty||o is ExcelDna.Integration.ExcelEmpty
       ||o==null||o is string s&&s=="*")
        return null;
    var d=InputNormalizer.ToDouble(o);
    return double.IsNaN(d)?null:d;
}
```

**复核**：⚠️ 行为确认，但是**有意设计**而非 bug

**严重度调整**：HIGH → **MEDIUM**

**git diff 追溯**（commit `159cb84`）：
```diff
- private static double? V(object o){
-     if(o is Foundation.ExcelEmpty||o is ExcelDna.Integration.ExcelEmpty||o==null)
-         return null;
-     return InputNormalizer.ToDouble(o);
- }
+ private static double? V(object o){
+     if(o is Foundation.ExcelEmpty||o is ExcelDna.Integration.ExcelEmpty
+        ||o==null||o is string s&&s=="*")
+         return null;
+     var d=InputNormalizer.ToDouble(o);
+     return double.IsNaN(d)?null:d;
+ }
```

**设计分析**：
- **原始**（`159cb84` 前）：空单元格已返回 `null`（= 求解此变量）
- **新增**：`"*"` 字符串作为空单元格的**显式替代**（commit message: "IDEALGAS *约定"）
- IdealGasLaw Core 要求 4 参数中恰好 1 个为 `null`（`missing != 1 → return NaN`）
- 用户误留空 2+ 参数 → `missing ≥ 2 → NaN`（被正确拒绝）
- **这是 UX 设计**：用户把待求解变量的单元格留空（或填 `*`）

**剩余问题**：仅 `[ExcelFunction]` Description 提及 `"*"`，参数级未说明

---

## HI-2: LINALG.IDENTITY 无尺寸上限

**位置**：`src/Analytics/LinalgUdf.cs:81` → `LinalgCore.cs:227-228`

**源码**：
```csharp
// UDF 层
=> OutputWrapper.WrapError(() => LinalgCore.Identity((int)InputNormalizer.ToLong(n)));

// Core 层
internal static double[,] Identity(int n) =>
    Matrix<double>.Build.DenseIdentity(n).ToArray();
```

**复核**：✅ 确认——**无任何尺寸验证**

**严重度**：维持 **HIGH**

**影响**：
- `OutOfMemoryException` 在 WrapError 过滤器中**被显式排除**（`ex is not OutOfMemoryException`）
- n=1,000,000 → 10^12 元素 → OOM → Excel **进程崩溃**
- 对比 `ARR.RANGE` 有 100K 上限，`LINALG.IDENTITY` 完全没有

**git 追溯**：自方法引入以来即无守卫；多次审查未覆盖此问题

**建议修复**：`if (n < 1 || n > 10000) throw new ArgumentException(...)`

---

## HI-3: FactorImportance 遇零方差列崩溃

**位置**：`src/Analytics/RegressionCore.cs:238-246`

**源码**：
```csharp
if (sd < 1e-12)
{
    Trace.WriteLine($"[FactorImportance] Column {j} has zero variance...");
    sd = 1;  // ← 防止除零，但副作用：全列变为 0
}
for (int i = 0; i < n; i++) Xs[i, j] = (X[i, j] - mean) / sd;
// ...
var result = FitOLS(Xs, y);  // Xs 含全零列 → XtX 奇异
```

**复核**：✅ 确认——`sd=1` 的副作用未被处理

**严重度**：维持 **HIGH**

**根因**：程序员意图是"防除零"（`sd=1`），但未认识到全零列会使后续 `FitOLS` 中的 `XtX.Solve()` 抛出 `SingularMatrixException`

**git 追溯**：
- `Trace.WriteLine` 警告在 commit `ffaee34` 或更早添加
- 全零列→奇异矩阵的因果链从未被修复
- skill.md 提到 "MathNet Solve 奇异矩阵可能不抛异常 → RegressionCore 已加显式 guard"——但该 guard 仅覆盖 tss=0 和 df≤0，不覆盖 FactorImportance 引入的共线性

---

## HI-4: FitOLS 缺 XtX 奇异性检测

**位置**：`src/Analytics/RegressionCore.cs:33`

**源码**：
```csharp
var beta = XtX.Solve(Xty);  // 无前置奇异性检查
```

**已有守卫**（lines 39-46）：
- `tss < 1e-15` → 常数响应变量 y
- `df <= 0` → n ≤ p（欠定）

**复核**：✅ 确认——缺失共线性守卫（n > p 但列线性相关，如 `X = [[1,2],[1,2],[1,2]]`）

**严重度**：维持 **HIGH**

**git 追溯**：skill.md 历史修复记载 "MathNet Solve 奇异矩阵可能不抛异常 → RegressionCore 已加显式 guard"，但该 guard 实为 tss=0 和 df≤0 两项，非共线性检测

---

## HI-5: net48 下 ATTACH DATABASE 未禁用

**位置**：`src/DataToolkit/SqlCore.cs:26`

**源码**：
```csharp
using var cmd = conn.CreateCommand();
cmd.CommandText = sql;  // 用户 SQL 直接执行
```

**复核**：✅ 确认

**严重度调整**：HIGH → **MEDIUM**

**双 TFM 差异**：

| TFM | 提供者 | ATTACH | 多语句 |
|-----|--------|:-----:|:-----:|
| net48 | `System.Data.SQLite` | ✅ 允许 | ✅ |
| net8.0 | `Microsoft.Data.Sqlite` | ❌ 禁用 | 有限 |

**攻击面重评估**：
- 数据库为 `:memory:`——仅含从 Excel 区域导入的数据
- 攻击者外泄的是**自己能访问的数据**（数据来自其 Excel 区域）
- README 已声明「请在可信输入上使用」
- net8.0（推荐运行时）默认安全
- 实际威胁：恶意共享工作簿作者可嵌入 ATTACH 语句外泄数据到可预测位置——但作者已有更简单的数据外泄方式

**建议**：net48 下添加 `SQLITE_DBCONFIG_DEFENSIVE` 或连接字符串限制

---

## HI-6: ARR.FILL 无分配上限

**位置**：`src/DataToolkit/ArrayUdf.cs:31`

**源码**：
```csharp
// ARR.FILL — 无上限
long c=InputNormalizer.ToLong(n);
var r=new object[c];
for(int i=0;i<c;i++)r[i]=v;
return r;

// ARR.RANGE (同文件 line 32) — 有 100K 上限
if(n>100000)throw new ArgumentException(
    $"ARR.RANGE would generate {n} elements; maximum is 100000.");
```

**复核**：✅ 确认——**与 ARR.RANGE 的不一致是同一 commit 的遗漏**

**严重度**：维持 **HIGH**

**git diff 追溯**（commit `7242a00`）：
```
fix: 全面审查修复 — 架构契约 + 静默损坏阻断 + AVE统一 + 去重改善
...
- ARR.RANGE: >100k静默截断→ArgumentException
```
- 同一 commit 为 ARR.RANGE 加了 100K 限制
- **ARR.FILL 被遗忘**——审查时未注意到需要同样的守卫

**影响**：与 HI-2 相同——超大输入导致 OOM（WrapError 不捕获）

---

## HI-7: Easter 缺年份范围验证

**位置**：`src/DataToolkit/DateTimeCore.cs:47`

**源码**：
```csharp
internal static DateTime Easter(long y)
{
    // ... 公历算法计算 mo, da ...
    return new DateTime((int)y, (int)mo, (int)da);  // 无 y 范围检查
}
```

**复核**：✅ 确认

**严重度调整**：HIGH → **MEDIUM**

**原因**：
- 无效年份（<1 或 >9999）抛 `ArgumentOutOfRangeException`
- 此异常类型**会被** WrapError 正常捕获（不在排除列表中）
- 用户看到 `#VALUE!`——不如 HI-2 严重（HI-2 会导致进程崩溃）

---

## 中优先级发现快速复核

| # | 确认 | 说明 |
|---|:--:|------|
| ME-1 RegexCache Clear() 竞态 | ✅ | 64+ 并发正则模式才可能触发（极低概率） |
| ME-2 DecompCache LRU O(n) | ✅ | 8 条目（MaxEntries=8），O(8)=O(1) 可忽略 |
| ME-3 DecompCache (T)existing 转型 | ✅ | 键前缀约定（svd:/qr:/lu:）确保类型安全 |
| ME-5 GasToSTP `(pAtm / 1.0)` | ✅ | 数学恒等，死代码 |
| ME-6 Sum/Product Inf→NaN | ✅ | 有意设计——Excel 无法显示 Infinity |
| ME-7 decimal→double 缺 Infinity 守卫 | ✅ | decimal 值 > 1e308 几乎不可能来自 Excel |
| ME-8 FormatValue MinValue 字符串 | ✅ | 对空单元格返回 "0001-01-01" |
| ME-10 PivotUdf 缺 hasHeaders | ✅ | API 不完整但非 bug |
| DO-1 README 测试数 | ✅ | 声明 2,790 → 实际 3,998 |

---

## 复核后严重度重分类

| 最终定级 | 数量 | 项目 |
|:-------|:--:|------|
| **HIGH** | 4 | CR-1 (信息泄漏，降1级), HI-2 (IDENTITY OOM), HI-3 (FactorImportance 崩溃), HI-4 (FitOLS 共线性), HI-6 (ARR.FILL OOM) |
| **MEDIUM** | 3 | HI-1 (有意设计), HI-5 (低风险), HI-7 (不崩溃) |
| **MEDIUM** | ~14 | 原中优先级发现 |

## 关键结论

1. **HI-6 (ARR.FILL) 是审查遗漏的直接证据**：commit `7242a00` 为 ARR.RANGE 加了 100K 上限但遗忘 ARR.FILL
2. **HI-1 (V() 空单元格) 是有意 UX 设计**：留空=求解 是原始设计，`"*"` 是后续添加的显式替代
3. **CR-1 (信息泄漏) 影响面极窄**：WrapError 已将消息限制在 Debug 输出，Excel 用户不可见
4. **HI-3 和 HI-4 是系统性盲区**：多次审查均覆盖 NaN/Inf/df 守卫，但未覆盖 FactorImportance 内部引入的共线性
5. **4 个 HIGH 问题应优先修复**，其中 HI-6 最简单（+1 行守卫），HI-3 最复杂（需重构 FactorImportance）
