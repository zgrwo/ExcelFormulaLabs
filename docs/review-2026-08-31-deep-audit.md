# ExcelFormulaLabs 全面深度审查报告

> 审查日期：2026-08-31 ｜ 基准版本：**v2.2.3**（`ea153dc`）
> 覆盖范围：`src/`（6,919 行 / 51 文件）、`tests/`（10,343 行 / 48 文件）、`scripts/`（17 文件）、`.github/`（12 文件）、`rules/` + 顶层文档
> 审查维度：架构 / 算法 / 实现 / 数值 / 稳定性 / 测试 / 脚本 CI / 文档
> 结论：**发现 4 项 P0、20 项 P1、若干 P2。** 已独立实测复核，非转述。

## 审查方法说明

本报告的所有 P0/P1 结论均经过**独立验证**，而非依赖静态阅读：

| 验证手段 | 用于确认 |
|---|---|
| 负向注入（README 计数 236→XXX 后重跑门禁） | 门禁是否真实拦截 |
| MathNet 5.0.0 独立工程 3000 样本实测 | LU 置换矩阵约定 |
| 复刻算法计时（n=2万~20万） | 排序复杂度退化 |
| `dotnet build` 反复重建 | 多 TFM 构建竞态 |
| Python 脚本解析 | 交叉验证真实对照比例 |
| 逐文件源码比对 | 文档/实现签名一致性 |

仓库在审查全过程中保持 `git status` 干净，未修改任何既有文件。

---

# 一、P0 — 会导致错误结果、崩溃或验证失效

## P0-1　OLS 用正规方程 + 显式矩阵求逆：条件数平方，静默返回错误的系数与全部推断统计量

**位置**：`src/Analytics/RegressionCore.cs:51, 80-85, 99-101`

```csharp
var XtX = matX.TransposeThisAndMultiply(matX);   // :51  cond(X'X) = cond(X)²
try { beta = XtX.Solve(Xty); }                    // :54
var XtXInv = XtX.Inverse();                       // :80  显式求逆（比 Solve 更差）
for (int j = 0; j < p; j++)                       // :81-85 守卫只查 NaN/Inf
    if (double.IsNaN(XtXInv[j, j]) || double.IsInfinity(XtXInv[j, j])) throw ...
double varJ = sigma2 * XtXInv[j, j];              // :100 → se → t → p
```

**为什么错**：正规方程把设计矩阵条件数**平方**。在 `double` 精度（ε≈2.2e-16）下，cond(X) > 1e8 时结果已不可信，而 `XtXInv` 的对角线守卫只在完全发散（NaN/Inf）时触发——**在"错得离谱但仍是有限值"的区间里完全失明**。

实测对照（MathNet 5.0.0 独立工程）：

| 输入 | cond(X) | 系数误差 ‖b−b_true‖ | QR 参考误差 | `XtXInv` 守卫 |
|---|---|---|---|---|
| Hilbert 8×8，b_true=全 1 | 1.5e10 | **6.07（>200%）** | 6.1e-7 | **不触发** |
| Hilbert 12×12 | ∞ | **26.9** | 8.5e-1 | **不触发** |
| 6 次多项式趋势 x=1..100, n=100 | 2.3e12 | 2.67e-7 | 2.98e-11 | **不触发** |
| 8 次多项式趋势 x=1..100 | 2.2e16 | 1.38e-5 | 4.02e-11 | **不触发** |

**最危险的一点**：Hilbert 8×8 场景下残差 ‖r‖=4.9e-9、R²≈1，**用户看到的是一份拟合得"完美"的回归报表**，但 coefficients / std_errors / t_stats / p_values 全是错的。而 `REGRESS.OLS` 的报表主体恰恰就是这四项；`DOE.ANALYZE` 的 Effect/t/p 走同一条路径，同样受影响。

注意第 63-64 行有 2026-08-29 的修复注释，把 TSS 从 `y'y−(Σy)²/n` 改成了中心化两遍形式——**R² 的灾难性抵消修好了，但 X'X 的条件数问题没修**。

**建议**：
1. `FitOLSCore` 改用 `matX.QR(QRMethod.Thin).Solve(vecY)`（宽矩阵退化时再用 SVD）；
2. `(X'X)⁻¹` 对角线改由 `R⁻¹` 求：`var Rinv = qr.R.Inverse(); var xtxInv = Rinv.TransposeThisAndMultiply(Rinv);`（R 良态，避免二次平方）；
3. 加 cond(X) 诊断守卫，超限时抛出可诊断的错误而非静默返回。

---

## P0-2　SQL 查询无行数上限，且递归 CTE 可绕过全部安全过滤 → 不可捕获 OOM → Excel 进程崩溃

**位置**：`src/DataToolkit/SqlCore.cs:24-35（过滤）, 63-68（读取循环）**

```csharp
// 三重过滤：只挡写操作，完全不挡计算量
if (!SelectOnly.IsMatch(sql)) throw ...;          // ^\s*(?:SELECT|WITH)\s
if (ForbiddenKeyword.IsMatch(sql)) throw ...;     // INSERT|UPDATE|DELETE|...|PRAGMA
if (sql.IndexOf(';') >= 0) throw ...;
...
while (reader.Read()) { ... rows.Add(row); }     // :65  rows 无上界
var result = new object[rows.Count, cols];       // :67  再整体复制 → 峰值 2× 内存
```

**为什么错**：

1. **输出量无守卫**：以下语句三条检查全部通过，然后触发 OOM：
   - `SELECT * FROM data a, data b`（100k 行 → 1e10 行）
   - `WITH RECURSIVE x(n) AS (SELECT 1 UNION ALL SELECT n+1 FROM x) SELECT count(*) FROM x`（无限递归）
   - `SELECT randomblob(1000000000)`（直接分配 1GB）
2. **OOM 不可捕获**：`ExceptionFilters.IsCatchable` 按 AGENTS.md 防错原则③ 明确把 OOM 排除在 catch 之外，`WrapError` 不兜底 → 异常穿透 Excel-DNA → **进程级崩溃**，用户未保存的工作簿全部丢失。
3. **防护不一致（这是最该修的理由）**：同一模块的 `PivotCore.cs:68`（maxCells=1e6）、`ArrayCore` CrossJoin 都已加"分配前"守卫，`SqlCore` 是 DataToolkit 里**唯一**漏掉的大输出路径。`CommandTimeout=30` 也救不了——UDF 在 Excel 计算线程同步执行，超时生效前界面已冻结。

**值得肯定**：SQL **标识符**注入防护是扎实的——`Sanitize()` 把非 `[A-Za-z0-9_]` 全部替换为 `_`，表名/列名无法闭合引号。问题只在**计算量与输出量**。

**建议**：
1. 读取循环内加行数 + 耗时双上限（`rows.Count > maxRows` 或 `Stopwatch > 5s` 即抛错）；
2. `ForbiddenKeyword` 加入 `\bRECURSIVE\b`；
3. 直接写入 `object[,]` 并按需 `Array.Resize`，消除 2× 内存峰值。

---

## P0-3　交叉验证闭环有三处系统性假阴性——"Python ↔ C# 交叉验证"这个卖点大面积失效

这是**最严重的质量问题**：项目把 Python 交叉验证当作核心正确性保障（README 宣称覆盖 224/236 UDF），但它对三类错误完全失明。

### (a) C# 返回 `+Inf`、Python 返回 `NaN` → 判定 PASS

`tests/CrossValRunner/ResultSerializer.cs:25`
```csharp
double d => double.IsNaN(d) || double.IsInfinity(d) ? null : d,
```
序列化层把 NaN 和 ±Inf **压成同一个 `null`**；`scripts/verify-manual.py:106-110` 又把 `null` 一律还原为"一定是 NaN"。于是 C#=+Inf 对 Python=NaN 这对**都错且互不相同**的结果会打 PASS。
而 `project-experience.md` 把"NaN/Inf 守卫缺失"列为出现 10+ 次的历史头号缺陷——**交叉验证恰好对这一类完全失明**。

### (b) 四分之三的 `check()` 根本不调用 C#

实测统计（`scripts/verify-manual.py`）：

```
普通 check( 调用行 : 263
cross_check( 调用行:  80     ← 仅占 23%
```

剩下约 77% 的 `check()` 是**纯 Python 自校验**——actual 与 expected 都由 Python/字面量生成，C# 从未参与：

```python
L189: check("STATS.LN", np.log([1,e,e²,e³,e⁴]).tolist(), [0,1,2,3,4])
L188: check("STATS.SQRT", np.sqrt([4,9,16,25,36]).tolist(), [2,3,4,5,6])
L157: check("STATS.COUNT", len(data), 20)
L364: check("PHYCHEM.MOLWT(CaCO3)", 40.078+12.011+3*15.999, 100.086)
```

这些在报告里以 `OK` 打印并计入 PASS，**营造出"已验证"的假象**。把 C# 侧 `STATS.LN` 改坏，一条都不会 FAIL。
（`verify-manual.py:395-400` 有 2026-08-29 的修复注释，承认旧版"字面量 vs 字面量什么都验证不了"，但"修正版"只是把 `50.0` 换成 `100.0/2.0`——**仍是纯算术恒等式**。）

### (c) manifest 的 `tolerance` 字段是死数据，`summary` 无人消费

- `grep tolerance scripts/verify-manual.py` → **0 次命中**。96 条 manifest 全部声明了 `tolerance`（其中 22 条为 `0`，若真被采用浮点必然全 FAIL），但 Python 侧在每个调用点各自硬编码。
- `CrossValRunner/Program.cs:45-53` 计算了 `Total/Ok/Error`，Python 侧**零次读取**。manifest 中 `LINALG.MATMUL`、`PHYCHEM.MOLWT_CaCO3` 从未被任何 `cross_check` 引用——即使 C# 侧抛异常，脚本照常 `exit 0`。

**建议**：
1. `ResultSerializer` 用带标签对象区分 `{"__nan__":true}` / `{"__inf__":1|-1}`；
2. 把 263 条拆成两个命名空间：`check_manual()`（校验手册示例）/ `cross_check()`（校验 C#），**分别汇报**，禁止混计；
3. `cross_check` 改为读 `ref["tolerance"]`；`load_csharp_results()` 后立即断言 `summary.error == 0`；
4. 门禁升级为语义检查：统计含 C# 引用的 `check` 比例，低于阈值即 FAIL。

---

## P0-4　文档计数门禁漏掉中文：中文 README 计数漂移 236→XXX 仍全绿通过

**位置**：`scripts/verify-docs.ps1:307`

```powershell
foreach ($m in [regex]::Matches($text, '(\d+)\s+UDF')) {
```

**为什么错**：中文文档一律写作「236 **个** UDF」，中间隔着「个」，正则 `(\d+)\s+UDF` **永远匹配不上**；英文文档写作「236 UDF」反而被覆盖。中英双 README 的门禁强度不对称。

**负向注入实测**（本审查执行，已还原）：

```
注入 README.md:     224/236 个 UDF → 224/XXX 个 UDF
注入 README.en.md:  224/236 UDF    → 224/XXX UDF
重跑 verify-docs:
  [FAIL] Prose UDF counts: README.en.md: 'XXX UDF'
  === Pass: 22  Fail: 1 ===        ← 只抓到英文，中文漏网
```

（补充：门禁退出码传播是正确的，`Fail:1` 时 `exit 1`。问题纯粹在正则覆盖范围。）

**建议**：正则改为 `(\d+)\s*(?:个)?\s*UDF`，并补 `(\d+)/(\d+)` 形式的覆盖声明检查（分子分母都验）。在 `tests/scripts/test_verify_docs.ps1` 增加"中文 README 计数漂移"场景。

---

# 二、P1 — 明显缺陷

## P1-1　LU 置换矩阵约定：实现与全部文档相反（`A = P·L·U` vs 文档写 `PA = LU`）

**位置**：实现 `src/Analytics/LinalgCore.cs:163-166`；文档 `LinalgUdf.cs:45`、`rules/api-reference.md:87-89`、`rules/user-manual.md:1023`

```csharp
// perm[i] = row index of original A that ends up at row i of the permuted matrix.
for (int i = 0; i < A.RowCount; i++) P[i, perm[i]] = 1.0;
```

UDF 说明写的是 `"LU decomposition permutation matrix P. PA = LU."`。

**独立实测**（MathNet 5.0.0，3000 个随机含零矩阵，n=2..6）：

```
P*A == L*U  成立 1414/3000 (47%)
A == P*L*U  成立 3000/3000 (100%)
非对合置换   1586/3000 (53%)
```

具体反例 `A = [[0,2,1],[1,0,3],[4,5,6]]`，perm=[1,2,0]：
```
||P*A − L*U||_F = 10.0995     ← 按文档使用，错
||A − P*L*U||_F = 0.0000      ← 实现的真实约定
```

即：**实现是正确的 scipy/MATLAB `A = P·L·U` 约定，三处文档写错了**。当置换不是对合时（53% 的情况）`PA = LU` 失效，按文档使用的用户会拿到错误的行序。

**注意**：`tests/Analytics.Tests/LinalgCoreTests.cs:49-60` 断言的是 `A == P·L·U`（与实现一致、与文档相反），所以测试全绿但文档漂移无人发现。

**建议**：保持实现，把三处文档统一改为 `A = P*L*U`；补一条**负向测试**（用 perm=[1,2,0] 的矩阵断言 `P·A ≠ L·U`）锁死约定，防文档再次漂移。

---

## P1-2　快速排序在全等值输入下退化为 O(n²)：20 万元素耗时 5.3 秒

**位置**：`src/Foundation/ArrayOperations.cs:76-92`（`Partition`，Lomuto 分区，无 3-way 处理）

```csharp
int i = lo;
for (int j = lo; j < hi; j++) {
    int cmp = CompareElements(arr[j], arr[hi], mode);
    bool shouldSwap = ascending ? cmp < 0 : cmp > 0;
    if (shouldSwap) { Swap(arr, i, j); i++; }
}
Swap(arr, i, hi);
return i;
```

全等值（或大量重复值）时 `cmp == 0` 恒成立 → `i` 不动 → `pivot == lo` → 每次只推进 1 个元素却做了一次完整 O(n) 扫描。这是 Lomuto 分区的经典退化。

**独立实测**（复刻该算法，Release，.NET 8）：

| n | 全等值 | 全随机 |
|---|---|---|
| 20,000 | 97 ms | 0 ms |
| 50,000 | 351 ms | 2 ms |
| 100,000 | **1,356 ms** | 5 ms |
| 200,000 | **5,312 ms** | 12 ms |

**影响面**：`ARR.SORT / SORTASC / SORTDESC / SORTNUM / SORTTEXT` 全部走这条路径。而对"状态列""部门列""布尔列""评分列"这类**高重复数据**排序是日常默认场景，不是病态输入。5 秒冻结在交互式 Excel 里等同于挂死。

**建议**：`Partition` 改为 3-way（Dutch national flag）分区，把 `cmp == 0` 的元素集中到中间一次跳过；外层返回 `(i, i+eq)` 跳过等值段。

---

## P1-3　`ARR.INDEXOF` / `ARR.CONTAINS` 的浮点容差分支是死代码

**位置**：`src/Foundation/ArrayOperations.cs:191` + `src/DataToolkit/ArrayCore.cs:13`

```csharp
bool isFloat = typeof(T) == typeof(double) || typeof(T) == typeof(float);   // :191
...
if (isFloat) { ... if (Math.Abs(dA - dB) < tolerance) return i; }
else if (array[i]!.Equals(value)) return i;    // ← 实际永远走这里
```
```csharp
internal static long IndexOf(object[] a, object v) => ArrayOperations.IndexOf(a, v);  // ArrayCore:13
```

调用方用 `object[]` → `T = object` → `isFloat` 恒为 `false` → **1e-12 容差分支永不执行**，退化成装箱 `Equals` 精确比较。

后果是**静默返回错误结果**（不报错）：
- `ARR.INDEXOF` 在 `{0.1+0.2}` 里查 `0.3` → `0.30000000000000004.Equals(0.3)` = false → 返回 **-1**
- `ARR.CONTAINS(A1:A10, A1*3)` 对任何浮点累积值返回 **FALSE**
- 整型 `1` 装箱 vs 浮点 `1.0` 装箱 `Equals` 也是 false → 混合类型区域查找失败

**建议**：`ArrayOperations.IndexOf` 的判定改为运行时探测（`a[i] is double or float`），或 `ArrayCore.IndexOf` 显式走 `double` 泛型路径。

---

## P1-4　`STATS.CORRMATRIX`：零方差判据用绝对阈值，且 NaN/Inf 会复活"对角线假 1.0"

**位置**：`src/Analytics/StatsCore.cs:194-200`

```csharp
sds[j] = Math.Sqrt(ss / (rows - 1));            // :194  ss 可溢出为 Inf，或含 NaN 时为 NaN
if (sds[i] < 1e-15) { ... r[i,j] = r[j,i] = NaN; }
else r[i, i] = 1.0;                              // :200  NaN/Inf 都走 else 分支！
```

**为什么错**：第 175-177 行的注释专门说明"rows<2 提前返回以避免假 1.0，因为 NaN 比较恒为 false 会产生对角线上虚假的 1.0"——**但同一类缺陷经 NaN/Inf 路径在 rows≥2 时完全存活**：

- 溢出路径：输入 `{{1e308,1},{1e308,2},{1e308,3}}`（全部有限，Excel 单元格可输入）→ `sum=Inf → mean=Inf → ss=Inf → sd=Inf`；`Inf < 1e-15` 为 false → `r[0,0] = 1.0`，而 `r[0,1] = NaN`。**对角线 1.0、非对角 NaN 的自相矛盾矩阵**，下游无法察觉。
- NaN 路径：列中含任一 NaN → `sd = NaN` → `NaN < 1e-15` 为 false → 同样得到假 1.0。
- 绝对阈值路径：相关系数是尺度不变量，列数据整体在 1e-16 量级时 `sd` 恒 < 1e-15 → 整行整列被误判为常量 → 返回全 NaN（真值应为 ±1 附近）。

**建议**：判据改为 `if (!double.IsFinite(sds[i]) || !(sds[i] > 0))`；并在 `means/sds` 循环后做 `double.IsFinite` 检查，非有限直接按 `RegressionCore` 的风格显式抛错。

---

## P1-5　一族"零方差绝对阈值"在小量纲数据上产生错误结论

**位置**：`StatsCore.cs:223, 240, 255`；`RegressionCore.cs:325, 371`；`LinalgCore.cs:231`

| 位置 | 阈值 | 影响 |
|---|---|---|
| `StatsCore.cs:223` TTEST1 | `va < 1e-15` | 常量分支 |
| `StatsCore.cs:240` TTEST2 | `va+vb < 1e-15` | 同上 |
| `StatsCore.cs:255` ZSCORE | `sd < 1e-15` | 同上 |
| `RegressionCore.cs:325` ANOVA1 | `Abs(ssW) < 1e-15` | 误判为"各组内完全相同" |
| `RegressionCore.cs:371` FACTORIMP | `sd < 1e-12` | 同上 |
| `LinalgCore.cs:231` 对称性判定 | `1e-8` 绝对 | 1e9 量级矩阵 ULP≈1.2e-7 > 1e-8，误判非对称 |

实测反例：`{1e-9, 2e-9, 3e-9}` 的样本方差 = 1.0e-18 < 1e-15 → 命中"零方差"分支。
- `=STATS.TTEST1({1e-9;2e-9;3e-9}, 1.5e-9)`：返回 **NaN**，真值 `t=0.8660254, p=0.4781`。**结论完全反转**。
- `REGRESS.ANOVA1({1e-9,2e-9,3e-9}, {4e-9,5e-9,6e-9})`：抛"All observations within each group are effectively identical"——两组均值差 3e-9、组内完全分离，**报错与事实相反**。

浓度（ppm/ppb）、位移（nm）、电流（µA）等小量纲数据是 Excel 常态，不是病态输入。

**建议**：统一改为相对判据 `va <= 0 || !IsFinite(va)`，或按数据 ULP 尺度缩放。这类"绝对阈值"缺陷是同一设计错误的六个实例，应一次性统一修。

---

## P1-6　`LINALG.RANK` 默认容差是绝对奇异值阈值，小尺度矩阵一律判为 0 秩

**位置**：`src/Analytics/LinalgUdf.cs:75-77`

```csharp
[ExcelFunction(Name = "LINALG.RANK", Description = "... default 1e-10.")]
... (long)LinalgCore.Rank(M(d), tol==null||tol is ExcelMissing ? 1e-10 : InputNormalizer.ToDouble(tol));
```

实测 `diag(s, s)` 的真实秩恒为 2：

| s | RANK（默认 1e-10） | RANK（相对容差） |
|---|---|---|
| 1.0 | 2 | 2 |
| 1e-6 | 2 | 2 |
| **1e-11** | **0** | 2 |
| **1e-13** | **0** | 2 |

讽刺的是**用户显式传 `0` 反而得到正确结果**（走 `LinalgCore.cs:251-254` 的相对分支 `S.Max()*max(r,c)*1e-16`）——默认路径比显式路径更差。文档没错，是**默认值选错了**：numpy `matrix_rank` 用的就是相对判据。

**建议**：默认值改为 `0`（由 Core 走相对分支），保留用户显式传绝对阈值的语义。

---

## P1-7　田口 2 水平设计把因子分配到交互列，分辨率从 V 退化到 III

**位置**：`src/Analytics/DoeCore.cs:500-525`（`Build2Level`）+ `:262-272`（顺序取前 k 列）

`Build2Level` 按 A, B, **AB**, C, **AC**, BC, ABC… 的标准序产出列，然后顺序取前 k 列。实测第 3 列恒等于 `col1 XOR col2`（L4/L8/L16/L32 全部成立）。

于是 `=DOE.PLAN(5,2,0,2,"TAGUCHI")` → L16，5 个因子落在列 A, B, **AB**, C, **AC**：
- 定义关系 I=123 与 I=145 → **分辨率 III**
- 因子 3 的主效应与因子 1×2 交互**完全别名**；因子 5 与 1×4 别名
- `DOE.ANALYZE` 默认 "2way" 时，"AB" 列与 "C" 主效应列完全共线 → `FitOLS` 抛 rank deficient

而标准 L16 五因子分配（列 1,2,4,8,15）是**分辨率 V**。3 水平分支（`Build3Level`）是标准用法，无此问题。

**建议**：2 水平按"非交互列优先"分配（L8→1,2,4,7；L16→1,2,4,8,15），或给列打 `isMainEffect` 标记并优先取用。

---

## P1-8　`DOE.ANALYZE / ANOVA / PARETO` 展开项数无守卫 → 分配 OOM

**位置**：`src/Analytics/DoeAnalysisCore.cs:127-181`

```csharp
var Xe = new double[n, p];      // :174  p = k + C(k,2) + C(k,3) (+k)
```

`FitOLS` 的 `df<=0` 检查在**分配之后**才执行。实测项数：

| k（因子数） | maxOrder=3 项数 | n=1000 时 Xe 内存 |
|---|---|---|
| 100 | 166,750 | **1.33 GB** |
| 1000 | 1.67e8 | 1333 GB |

`DOE.PLAN` 允许到 `MaxFactors=1000`，因此 `=DOE.ANALYZE(100因子设计, y)` 是合法调用链 → 1.33 GB 分配 → OOM → Excel 崩溃。
`DoeCore` 已有 `MaxRuns/MaxCells/MaxFactors` 三道守卫，但 **`DoeAnalysisCore` 是零守卫**。

**建议**：`ExpandTerms` 开头加与 `DoeCore` 同风格的守卫（先算 `p`，用 long + 除法防溢出），`p > 5000` 或 `p > MaxCells/n` 时抛错。

---

## P1-9　多 TFM 并行构建存在文件锁竞态：`dotnet build` 间歇性失败

**位置**：`src/Analytics/Analytics.csproj:80-93`、`src/DataToolkit/DataToolkit.csproj:95-110`

```xml
<Target Name="GenerateDnaFromTemplate" BeforeTargets="ExcelDnaBuild">
  <ItemGroup><_StaleDna Include="DataToolkit-AddIn*.dna" /></ItemGroup>
  <Delete Files="@(_GenDna)" />          <!-- 通配删除，跨 TFM 互相踩踏 -->
  <Copy Condition="'$(TargetFramework)'=='net8.0-windows" ... />
  <Copy Condition="'$(TargetFramework)'=='net48'" ... />
</Target>
```

**为什么错**：生成的 `.dna` 用**项目目录相对路径**（而非 TFM 隔离的 obj 路径），而 `dotnet build` 默认并行构建两个 TFM。net48 内建刚生成的 `.dna` 会被 net8.0 内建的通配 `Delete` 删掉，反之亦然。

**本审查实测**（`dotnet build`，同一份代码，无改动）：

```
第 1 次：4 个错误   DNA341414865: 文件被另一进程占用
第 2 次：0 个错误
第 3 次：86 行错误（--no-incremental）
第 4 次：20 行错误
第 5 次：6 个错误   MSB3491: obj\Debug\net48\Foundation.csproj.FileListAbsolute.txt 访问被拒绝
```

**间歇性失败，成功率约 50%。** 这直接命中 AGENTS.md 文档化的日常命令 `dotnet restore && dotnet build && dotnet test`。
CI 的 net48 job（ci.yml:107-112）用 `-f net48` 逐项目串行构建，**恰好规避了竞态**——但主 build job（ci.yml:79）与 release-build（ci.yml:176）走的是并行全 TFM 路径，**同一竞态暴露在 CI 上**，是潜在的 flaky CI 源。

**建议**：
1. `.dna` 生成到 `$(IntermediateOutputPath)`（TFM 隔离），不要通配删除项目目录下的文件；或
2. 在 `Directory.Build.props` 设 `<BuildInParallel>false</BuildInParallel>` 串行化内建；
3. 至少在 AGENTS.md 注明"本地构建偶发失败请重试"。

---

## P1-10　`PIVOT.GROUPBY` 的 COUNT 聚合会丢弃非数值行，分组整体消失

**位置**：`src/DataToolkit/PivotCore.cs:54-58`

```csharp
double v = InputNormalizer.ToDouble(data[r, valueCol]);
if (double.IsNaN(v) || double.IsInfinity(v)) continue;   // :55  在 key 收录之前
if (keySet.Add(k)) keyList.Add(k);                        // :56
if (pivotSet.Add(p)) pivotList.Add(p);                    // :57
```

对 `agg = "COUNT"`，值列为空/文本的行被**整体丢弃** → 该分组在输出中完全不出现，而不是显示 0；`keyList`/`pivotList` 也只收录带数值的行 → 纯空值分组的行标签/列标签丢失。COUNT 的本意恰恰是统计行数（含空值行）。

**建议**：`COUNT` 单独分支——即使 `v` 非数值也 `cnt[kv]++` 并收录 key/pivot；只有 SUM/AVG/MAX/MIN 才 skip。

---

## P1-11　`ElementWiseMapper` 的 per-cell 隔离承诺失效：转换失败会中止整个数组

**位置**：`src/Foundation/ElementWiseMapper.cs:238, 258, 276-278`

```csharp
private static object MapValue<TInput, TOutput>(object value, Func<TInput, TOutput> mapper)
{
    TInput typed = ConvertValue<TInput>(value);     // ← 在 try 之外
    try { TOutput result = mapper(typed); return (object)result!; }
    catch (Exception ex) when (ExceptionFilters.IsCatchable(ex))
    {
        // Per-cell isolation: a failing cell returns #VALUE! instead of
        // aborting the entire array.                      ← 注释承诺的语义
        return ExcelError.Value;
    }
}
```

`ConvertValue<T>` 在 try **之外**，而它会调用 `InputNormalizer.ToInt32`（对超 int 范围主动抛 `ArgumentException`，见 `InputNormalizer.cs:310-311`）。任一单元格触发 → 异常冲出 `Map2D` 的双层循环 → 用户看到的不是"该格 `#VALUE!`"，而是**整片区域一个 `#VALUE!`**。三处重载都存在同一问题。

**建议**：把 `ConvertValue` 移入 try 内，或新增 `TryConvertValue` 返回 `(bool ok, T value)`。

---

## P1-12　日期函数守卫不完整：`DaysInMonth` 月份静默截断，`WorkdaysBetween` 日期倒置静默返回 0

**位置**：`src/DataToolkit/DateTimeCore.cs:49, 61, 68`

```csharp
// :49  Math.Abs(long.MinValue) 抛 OverflowException
if (Math.Abs(days) > maxWorkdays) throw ...

// :61  只判上界；e < s 时 TotalDays 为负，天然 < maxSpan → 守卫失效 → 静默返回 0
if ((e - s).TotalDays > maxSpan) throw ...

// :68  只校验年份，月份完全无校验
internal static long DaysInMonth(long y, long m) {
    if (y < 1 || y > 9999) throw ...;
    return DateTime.DaysInMonth((int)y, (int)m);   // (int)m unchecked 截断
}
```

- `DT.DIM(2026, 4294967297)`：`(int)4294967297L == 1` → 返回 **31**（1 月的天数），而不是报错。项目未启用 `CheckForOverflowUnderflow`。
- 同文件 `Easter`(:63)、`IsLeapYear`(:67) 都做了完整入参校验，这里只做了一半。
- `Math.Abs(long.MinValue)` 实测抛 `OverflowException`（误导性异常，而非业务校验错误）。

**建议**：补 `m ∈ [1,12]` 校验；`WorkdaysBetween` 改用 `Math.Abs` 并对 `e < s` 显式处理；`AddWorkdays` 先判 `long.MinValue`。

---

## P1-13　`STR.LEVENSHTEIN` 无长度守卫：O(n·m) 可致分钟级冻结

**位置**：`src/DataToolkit/StringCore.cs:99-101`

Excel 单元格上限 32767 字符 → 最坏 32767² ≈ **1.07e9 次内层循环**。同一文件里 `GuardPadLength`(:46)、`RandomString`(:164)、`FormatValue`(:129) 都做了显式防护，`:44` 的注释还专门写了 "uncatchable OOM" 的理由——唯独 Levenshtein 漏了。

**建议**：`if ((long)na * nb > 25_000_000) throw new ArgumentException(...)`，或实现 Ukkonen 带状优化。

---

## P1-14　文档版本头停在 2.2.1，实际 2.2.3，且**无任何门禁覆盖**

```
rules/specification.md:3      → 版本：v2.2.1
rules/user-manual.md:3        → **版本**：2.2.1
docs/cross-validation.md:1    → ... v2.2.1
src/Directory.Build.props:3   → <Version>2.2.3</Version>   ← 差 2 个版本
```

`project-experience.md:120` 把它列为强制同步项，但 v2.2.2、v2.2.3 两次发版都没同步；verify-docs 的 23 条检查中**没有一条**校验版本头。更糟的是 `CHANGELOG.md:36` 明确声称"版本头 2.1.0→2.2.1（specification/user-manual/cross-validation）"——CHANGELOG 声称的事没兑现，而 `project-experience.md:119` 恰好警告过这一点。

**建议**：新增检查：三份文档版本头 == `<Version>`。

---

## P1-15　CHANGELOG 缺 `[2.2.3]` 版本链接行，`[Unreleased]` 悬空

```
9:  ## [2.2.3] - 2026-08-29     ← 章节存在
260:[2.2.2]: .../compare/v2.1.1...v2.2.2
261:[Unreleased]: .../compare/v2.2.2...HEAD   ← 章节已不存在，悬空引用
                                              ← 无 [2.2.3] 链接行
```

`project-experience.md:127` 发版清单第 2 条明确要求「`## [X.Y.Z]` + **版本链接行**」。verify-docs 检查 10（`:138`）只做 `if ($changelog -notmatch "## [$ver]")`，**不校验链接行**。

**建议**：检查 10 增加「`## [X.Y.Z]` 与 `^\[X.Y.Z\]:` 必须成对」断言；删除陈旧的 `[Unreleased]` 链接行。

---

## P1-16　覆盖率 `224` 是硬编码字面量，与自身 `section()` 声明（221）自相矛盾

**位置**：`scripts/verify-manual.py:961`

```python
udf_count = (34 + 19 + 7 + 16 + 34 + 25 + 9 + 22 + 8 + 8 + 4 + 3 + 22 + 9 + 4)
```

写死的算术常量，**不从实际 `check()` 调用推导**。而同一文件 15 处 `section()` 声明的计数合计是 **221**（DOE 段声明 1，常量里却按 4 算），README 又声称 224。**221 / 224 / 224 三个数字同时在库**。删掉半个 section，它照样打印 224。

这正是 `project-experience.md §五` 记录过的"散文计数漂移"反模式（曾导致 232 陈旧计数全绿通过）。

**建议**：从 `csharp_results()` 实际命中的 id 集合反推；README/CHANGELOG 引用同一派生值。

---

## P1-17　`verify-all.ps1` 漏掉 AGENTS.md 要求的第 ① 步 verify-docs

`AGENTS.md:161` 定义全量验证 5 步 = ① verify-docs ② dotnet test ③ CrossVal ④ verify-manual.py ⑤ Release build。
实测 `grep -c verify-docs scripts/verify-all.ps1` → **0**。实现是 ① Build ② Unit Tests ③ CrossVal ④ Pre-commit ⑤ Release Build，并把 AGENTS 的 ②③ 合并成了一步。

**建议**：在 Step 1 前插入 verify-docs，或修订 AGENTS.md 的 5 步描述与实际对齐。

---

## P1-18　pre-commit 的 `hasHeaders` 契约检查只扫 `*Core.cs`，`AnalyticsHelpers` 漏网

**位置**：`scripts/pre-commit-check.ps1:153, 171, 204`（全部 `-Filter "*Core.cs"`）

```csharp
// src/Analytics/AnalyticsHelpers.cs:10
internal static double[,] ToDoubleMatrix(object[,] data)   // ← 无 hasHeaders
```

AGENTS.md §4 红线要求所有接受 `object[,]` 的 Core 方法必须含 `bool hasHeaders = true`。`AnalyticsHelpers.cs` 不被门禁覆盖 → 契约漏洞。（`DoeCore/StatsCore` 等真正带 `*Core.cs` 后缀的文件确已全部合规。）

**建议**：把扫描范围改为排除 bin/obj 的全部 `.cs`，或把 `AnalyticsHelpers.cs` 显式登记豁免。

---

## P1-19　`STR.FMT` 依赖 `CurrentCulture`，测试套件无固定 CultureInfo

```csharp
// src/DataToolkit/StringCore.cs:141-147
if (value is double d) return string.Format(fs, d);   // 无 InvariantCulture
```
```csharp
// tests/DataToolkit.Tests/StringUdfTests.cs:406-408
StringUdf.UDF_STR_FMT(123.456, "N2").Should().Be("123.46");
StringUdf.UDF_STR_FMT(0.25, "P0").Should().Be("25%");
```

de-DE 下 `"N2"`→`"123,46"`、`"P0"`→`"25 %"`、`"C"`→`"1.234,50 €"`，三条全 FAIL。而**全部 48 个测试文件中 `CultureInfo` 出现 0 次**——本地/CI 结果取决于机器 locale。
（对照：`DateTimeCore.cs:35` 的 `WeekdayName` 正确用了 `InvariantCulture`，说明团队知道这个问题，只是没覆盖到这里。）

**建议**：`FormatValue` 统一用 `InvariantCulture`（UDF 输出应可移植），并补一条负向测试：切到 `de-DE` 后断言输出不变。

---

## P1-20　异步 UDF 在后台线程做 COM 交互，违反 Excel-DNA 异步契约

**位置**：`src/Analytics/LinalgAsyncUdf.cs:28-29, 66-67`；`RegressionAsyncUdf.cs`（同模式）

```csharp
ExcelAsyncUtil.Run("LINALG.SVD_U_ASYNC", d, () => { ... M(d) ... });
```

`M(d)` 在 lambda 内 → 在线程池线程执行，而 `AnalyticsHelpers.PrepM → InputNormalizer.NormalizeTo2D`（`InputNormalizer.cs:57-62`）会走 `TryExtractComRangeValue`（`Marshal.IsComObject` + dynamic COM 派发）。Excel-DNA 异步契约要求委托保持纯计算、**不得跨线程触碰 COM**——这是导致 Excel 随机崩溃的经典模式。

附带：`ExcelAsyncUtil.Run` 的 parameters 参与 RTD topic 构造，把整个 `object[,]`（可达上万元素）作为 key 成员存在长度/唯一性风险。

**建议**：把 `M(d)/V(d)` 提到 `ExcelAsyncUtil.Run` 之外，在调用线程完成转换，只把 `double[,]` 交给后台；key 改用紧凑形式（维度 + 内容哈希）。

---

# 三、P2 — 改善建议（节选）

| # | 位置 | 问题 | 建议 |
|---|---|---|---|
| P2-1 | `scripts/pre-commit-check.ps1:183-186` | 检查 5 的守卫判定用**未去注释的全文** `$content`——文件头写一句 `// ArgumentException 用于参数校验` 即可让整个文件的 NaN/Inf 守卫检查失效；且 `ArgumentException` 与 NaN 守卫无因果关系，是过宽的豁免口 | 改用已剥离注释的 `$code`；移除 `ArgumentException` 豁免 |
| P2-2 | `scripts/pre-commit-check.ps1:70,122,204` | 未排除 `bin/obj`，把 24 个生成的 `obj/**.cs` 纳入扫描；与 `verify-docs.ps1:59` 的过滤口径不一致 | 统一加 `-notmatch '\\(bin|obj)\\'` |
| P2-3 | `scripts/run-affected-tests.ps1:76` | 正则 `(Core\|Udf\|Helpers\|AsyncUdf)` —— `LinalgAsyncUdf.cs` 回溯时 `\w+`="LinalgAsync" + `Udf` 先命中，`"AsyncUdf"` 分支**永远进不去**，生成错误类名 | 改为 `(Core\|AsyncUdf\|Udf\|Helpers)` |
| P2-4 | `scripts/patch-xll-version.ps1:44-47` | XLL 不存在时 `exit 0`，MSBuild 认为步骤通过；仅 release.yml 兜底，ci.yml release-build 无产物断言 | 改为 `exit 1` 或加 `-AllowMissing` 开关 |
| P2-5 | `tests/scripts/test_verify_docs.ps1:71-80` | CI 无 `.qoder` 时场景跳过却**计为 PASS**，汇总显示"Pass: 7 Fail: 0" | 分离 Pass/Skip 计数器 |
| P2-6 | `tests/scripts/run-tests.ps1:16` | 硬编码 `powershell`（5.1），而 release.yml 全用 pwsh 7；`project-experience.md C2` 记录的 pwsh7 语义差异无法在自测阶段暴露 | 优先 `pwsh`，或 CI 加 pwsh matrix |
| P2-7 | `scripts/test-xll.ps1:6` | 硬编码个人绝对路径 `D:\Workspace\zgrwo\VBA\...`，且末尾无 `exit $fail` → 自动化调用恒得 0 | 默认值置空强制校验；补退出码 |
| P2-8 | `InputNormalizer.cs:278/291` | `ToLong` 边界守卫有 2⁶³ 漏洞：`(double)long.MaxValue == 9223372036854775808.0`，比较恒为 false → 守卫绕过，`(long)` 得 `long.MinValue`（net8 饱和 / net48 未定义，双 TFM 行为不一致） | 改用严格小于 2⁶³ 的字面量比较 |
| P2-9 | `ElementWiseMapper.cs:111-112, 148-149` 等 7 处 | 返回 Foundation `ExcelEmpty`，而同文件 `:187-190` 注释明确说 Foundation.ExcelEmpty 不在 Excel-DNA 封送白名单内、真实 Excel 中渲染为 `#NUM!`。同类位置还有 `OutputWrapper.cs:59/67`、`PivotCore.cs:82`、`SqlCore.cs:67`、`JsonXmlCore.cs:39`、`DictOperations.cs:69` | 统一回归验证这 7 处的真实渲染结果 |
| P2-10 | `PivotCore.cs:17-22, 141` | SUM/AVG 朴素累加无精度补偿（`project-experience.md` 已记录"灾难性抵消 2 次"） | 改 Neumaier 补偿求和 |
| P2-11 | `StatsCore.cs:77/82` | `Product(1e300, 1e300, 1e-300)` → `Inf → NaN`（真值 1e300），顺序依赖溢出 | 按绝对值排序或做对数量级预检 |
| P2-12 | `PhyChemCore.cs:214-231, 242` | 溢出时直接返回 `±Inf`，与 StatsCore 的 "Infinity capped to NaN" 模块约定不一致 | 加 `IsInfinity → NaN` |
| P2-13 | `ArrayOperations.cs:202-204` vs `ComparisonUtils.cs:89` | 两处容差与 Infinity 语义不统一：`IndexOf` 未处理 Infinity（同号 Inf 相减得 NaN → 永远找不到）；绝对 epsilon 在 1e12 量级恒真、1e-6 量级过松 | 复用 `ValuesEqual` 的 Infinity 分支；改相对容差 |
| P2-14 | `PivotCore.cs:100-105` | `valueCols` 为空数组时静默产出 0 行，用户无法区分"参数传错"与"无数据" | `valueCols.Length == 0` 时抛错 |
| P2-15 | `FileSystemCore.cs:281-293` | `GetTempFileName` 在沙箱路径留下 0 字节文件且从不清理，`EndSession` 不处理；无存在性冲突重试 | 循环直到 `!File.Exists`；`EndSession` 清理清单 |
| P2-16 | `ArrayCore.cs:12/26-28` | `SafeKey` 用 `GetHashCode()`（.NET 进程内随机化 → 结果不可复现）；depth>20 主动抛异常导致 `ARR.UNIQUE` 整批失败；`Intersect`/`Except` 每元素调用 3 次 | 复用一次结果；Object 分支改稳定序列化 |
| P2-17 | `ElementWiseMapper.cs:65-66, 375-394` | Rank≥2 强类型数组塌缩成单个 NaN；1×1 与 n×1 广播时抛不知所云的 "Cannot reshape" 而非广播 | `NormalizeTo1D` 补 Rank≥2 分支；维度校验跳过总元素数==1 的输入 |
| P2-18 | `StringCore.cs:192-198` | `NthIdx` 空分隔符 + n > len+1 时 `ArgumentOutOfRangeException` | 提前分流空分隔符 |
| P2-19 | `StringCoreTests.cs:158` | `Soundex("Robert").Should().Be(Soundex("Rupert"))` —— expected 由被测实现自己生成，改成 `return "X000"` 照样 PASS | 断言硬编码期望值 `"R163"` |
| P2-20 | `LinalgUdfTests.cs:15-34` 等 | 只断言 `NotBeNull`（`double[,]` 不会返回 null，永不失败）；`BeGreaterThan(0)` 类宽断言（条件数按定义恒≥1）；`Skewness` 容差 0.1 对 1.55 = 6.5% 相对误差 | 改精确断言或删除冗余 |
| P2-21 | `ArrayOperationsTests.cs:49,50,170` | 零断言测试 | 改为 `act.Should().NotThrow()` |
| P2-22 | `FileSystemCoreTests.cs:258-264` | 10 处 sandbox 测试中 9 处有 `finally` 复位，唯独此条漏了；共享静态状态 | 补 finally |
| P2-23 | `FileSystemCoreTests.cs:407-443` | 依赖 `cmd.exe` + `mklink /J` 权限，受限 CI 下静默失败；spawn 外部进程 | 失败时 Skip 并说明原因，或加 Trait 过滤 |
| P2-24 | `.pre-commit-config.yaml:45` | 称"16 项"，实际 18 项（AGENTS.md:162 / verify-docs.ps1:4） | 改为 18 项 |
| P2-25 | `docs/cross-validation.md:5` | 声称 Python 3.12，CI（ci.yml:140 / release.yml:74）实为 3.11 | 核对后修正 + 加门禁 |
| P2-26 | `.github/dependabot.yml:23-29` | `pyDOE2` 无版本上界，违反 `project-experience.md` D2 铁律（其余依赖都有上界） | 补 `pyDOE2>=1.3,<2` |
| P2-27 | `scripts/update_excel_arguments.py` | 无 `main()`/无 `except`/无 `sys.exit`，正则不匹配时静默 no-op | 加改动计数 + 零改动时 `sys.exit(1)` |
| P2-28 | `scripts/validate-commit-msg.sh:43` | `wc -m` 在 C locale 下按字节计数，中文标题会在 <72 字符处误报过长 | 改用 `${#subject}` 或固定 `LC_ALL=C.UTF-8` |
| P2-29 | `verify-docs.ps1:50-54` | `Check-Skip` 把 SKIP 计为 `$script:pass++`，"Pass: 23" 含跳过项 | SKIP 单列 |
| P2-30 | `sync-qoder-skills.ps1:66,103` | 硬编码 `skills\$name.md` 反斜杠，Linux/pwsh 下不转换 | 改用 `Join-Path` |
| P2-31 | `release.yml:28-33` | Setup .NET 未开 `cache: true`（ci.yml 5 个 job 全开） | 补齐 |
| P2-32 | `rules/specification.md:34, 85` | `STR.* | ~34` 用近似符（实际精确 34）；"2,444 单元测试（[Fact]/[Theory] 实测计数）"——`[Theory]` 实为 **0** | 去掉 `~`；改为"2,444 个 [Fact]" |
| P2-33 | `RegressionCore.cs:374` | `Trace.WriteLine` 在因子循环内，1000 列 = 1000 次同步 Trace（UDF 热路径） | 移除或改为诊断字段 |
| P2-34 | `LinalgCore.cs:22-69` | 分解缓存按条目数（32）限流而非内存：2000×2000 SVD 单条 ≈64MB，32 条 ≈2GB | 按累计元素数限流 |
| P2-35 | `LinalgCore.cs:257` | `NormFrobenius` 中间平方溢出：`[[1e200,1e200]]` → Inf（真值 1.41e200 可表示） | 尺度化归一 |
| P2-36 | `DoeCore.cs:338-345` | RSM `total` 加法绕过守卫，k=31 时溢出为负（当前因上游先抛错而不可达，属潜伏破口） | 先判 `nf > MaxRuns` 再算 total |
| P2-37 | `FileSystemUdfTests` 等 | 非矩形/锯齿数组（`object[][]`）零测试，全 48 文件 0 次出现 | 补 `NormalizeTo2D(jagged)` 行为测试 |

---

# 四、做得好的地方（供保持，勿在重构中破坏）

审查不只是挑错。以下维度经实测确认**质量高于同类项目平均水平**，且多数是"曾被修过"的地方，重构时应作为回归守卫保留：

1. **架构分层干净**（红线 1 全面达标）：`grep -rn "ExcelDna" src/*/*Core.cs src/Foundation/*.cs` 仅命中 `InputNormalizer.cs:195/211/223` 的**字符串 FullName 反射探测**（无程序集引用）。无跨层/反向依赖，`#if NET48` 仅用于内部实现（IntelliSense、IsoWeek polyfill、SQLite 提供程序别名、IsExternalInit）。
2. **裸 catch 为零**：`grep -rn "catch\s*{" src/ --include="*.cs"` 返回空；全部 catch 均带 `when (ExceptionFilters.IsCatchable(ex))`，过滤器正确排除 OOM/StackOverflow/AccessViolation。
3. **ReDoS 防护完备**：RegexCore / FilterUtils / SqlCore / StringCore / PhyChem 全部带 5s Timeout，RegexCore 有 `MaxPatternLength=10000`，FilterUtils 有缓存上限 64。
4. **XXE 防护完备**：`JsonXmlCore.cs:71-76` 三件套（`DtdProcessing.Prohibit` + `XmlResolver=null` + `MaxCharactersFromEntities=0`）。
5. **SQL 标识符注入防护扎实**：`Sanitize()` 把非 `[A-Za-z0-9_]` 全替换为 `_`，无法闭合引号。（语句级计算量问题见 P0-2。）
6. **沙箱路径遍历防护有效**：实测 4 类逃逸模式（根路径 / UNC / `\\?\` / 驱动器相对）均被 .NET 拒绝；`..` 段被 `GuardSearchPattern` 显式拦截；重解析点在 `NormalizePath` 与 `DeleteFolderRecursive` 两处都有处理。
7. **无 DataTable/DataSet 资源泄漏**：源码中完全不存在这两个类型；`FileStream`/`XmlReader`/`JsonDocument`/`SHA256`/`SqliteConnection` 等全部 `using`。
8. **签名一致性 236/236 全匹配**：独立脚本全量比对 `api-reference.md` 参数列 ↔ 源码 `[ExcelArgument(Name=...)]`，**0 处不一致**（含顺序与可选标记）。
9. **数值算法选型的正确决策**：方差/协方差委托 MathNet 的两遍算法（**非** `E[x²]−E[x]²`，与两遍逐位一致）；n 与 n-1 的区分与文档一致；分位数 `QuantileCustom(R7)` 与 numpy 'linear' 逐点完全一致；主元选择全部委托 MathNet LU（无自实现无主元消元）；无自实现的幂迭代/Newton/梯度下降。
10. **TSS 灾难性抵消已修**：`RegressionCore.cs:63-64` 从 `y'y−(Σy)²/n` 改为中心化两遍形式，注释完整记录了原因。（但 X'X 条件数问题未修，见 P0-1。）
11. **CI 安全配置正确**：无 `continue-on-error`、无 `pull_request_target`；`permissions` 最小化为 `contents: read`；`nuget.config` 的 `packageSourceMapping` 已把 `ExcelFormulaLabs.*` 限定到 github 源。
12. **双 TFM 覆盖到位**：ci.yml 有独立的 net8.0 与 net48 job，release-build 也双 TFM 构建 + 测试。
13. **`AsyncUdfTests.cs:93-127` 的元数据测试质量很高**：用反射校验 12 个 `*_ASYNC` 的注册契约，并**反向比对** api-reference 的清单（双向一致）。
14. **历史缺陷修复完整**：`MathNet.Solve` 奇异矩阵不抛异常的守卫经实测有效（返回 NaN 被 `LinalgCore.cs:186-188` 拦截）；`FitOLSCore` 的 `±Inf` 对角线守卫有效；DOE 生成侧 `MaxRuns/MaxCells/MaxFactors` 三道守卫实测有效。

---

# 五、修复优先级建议

## 第一批：静默错误结果（用户已经在用错的数字）

| 序 | 问题 | 理由 |
|---|---|---|
| 1 | **P0-1** OLS 正规方程 → QR/SVD | 旗舰回归功能的 coefficients/se/t/p 全错，且报表看起来完美 |
| 2 | **P1-1** LU 文档约定修正 | 一行文档改动，但影响所有按文档使用 `LINALG.LU_P` 的用户 |
| 3 | **P1-5** 零方差绝对阈值族（6 处） | 小量纲数据结论反转，同一设计错误一次性修 |
| 4 | **P1-4** CorrMatrix NaN/Inf 假 1.0 | 已知缺陷的同类复活路径 |
| 5 | **P1-3** IndexOf 容差死代码 | 静默返回 -1 / FALSE，与文档契约冲突 |
| 6 | **P1-6** RANK 默认容差改 0 | 一个字符的改动，修复小尺度矩阵判 0 秩 |
| 7 | **P1-10** Pivot COUNT 语义 | COUNT 的本意就是统计行数 |

> 每一条都应先写**复现测试确认为 FAIL**，再修，再确认 PASS。P0-1 建议固化 Hilbert 8 + 6 次多项式两个反例；P1-1 固化 perm=[1,2,0] 的 3×3 负向测试。

## 第二批：崩溃与可用性

| 序 | 问题 | 理由 |
|---|---|---|
| 8 | **P0-2** SqlCore 行数上限 + RECURSIVE 拦截 | 唯一不可捕获的 OOM 路径 → Excel 进程崩溃 |
| 9 | **P1-8** DoeAnalysisCore 项数守卫 | 1.33GB 分配，同为不可捕获 OOM |
| 10 | **P1-2** 排序 3-way 分区 | 5.3 秒冻结，且是高重复数据的默认场景 |
| 11 | **P1-9** 多 TFM 构建竞态 | 日常命令 50% 失败率，CI 也暴露 |
| 12 | **P1-20** 异步 UDF 跨线程 COM | Excel 随机崩溃的经典模式 |
| 13 | **P1-13** Levenshtein 长度守卫 | 分钟级冻结 |

## 第三批：验证体系可信度（这是项目质量的根基）

| 序 | 问题 | 理由 |
|---|---|---|
| 14 | **P0-3(a)** ResultSerializer 区分 NaN/Inf | 恢复对头号历史缺陷的检测能力 |
| 15 | **P0-3(b)** 263 条 check 拆分为 manual / cross 双通道 | 消除 77% 的假阳性 "OK" |
| 16 | **P0-3(c)** 启用 manifest tolerance + 断言 summary.error==0 | 让 96 条 manifest 真正成为契约 |
| 17 | **P0-4** verify-docs 正则改 `(\d+)\s*(?:个)?\s*UDF` | 一个正则的改动，恢复中文文档门禁 |
| 18 | **P1-16 / P1-14 / P1-15** 覆盖率派生化 + 版本头门禁 + CHANGELOG 链接行 | 三项都是"文档声称 vs 现实"的漂移 |
| 19 | **P1-19** CultureInfo | 消除 locale 依赖 |
| 20 | **P1-18** hasHeaders 门禁扩围 | 契约漏洞 |

## 第四批：覆盖率补齐

- Dispatcher 补注册：`StringCore`（25 个零覆盖）+ `DateTimeCore`（21 个）+ `ArrayCore`（12 个）——这三个类纯确定性、无浮点歧义，注册成本最低，可把交叉验证覆盖从 37% 提到 ~75%。
- 补 `DOE.PARETO` 的 UDF 层测试（唯一一个同步 UDF 完全无 UDF 层测试）。
- 补 11 个 `*_ASYNC` 的运行时行为测试（当前仅 `SVD_U_ASYNC` 一条，其余只验证"存在"）。
- 补锯齿数组（`object[][]`）的 `NormalizeTo2D` 行为测试——全 48 文件 0 次出现。

---

## 附：审查执行记录

- 负向注入测试：README.md / README.en.md 注入 236→XXX → 观察门禁 → **`git checkout` 还原，已确认 `git status` 干净**
- 临时验证工程：`/tmp/lucheck`（LU 约定）、`/tmp/sortbench`（排序退化），均在仓库外，未污染仓库
- 构建竞态测试：`dotnet build` 反复执行 5 次，捕获间歇失败
- **全程未修改仓库任何既有文件**；本报告为新增文件 `docs/review-2026-08-31-deep-audit.md`
