# 审查后续修复实施计划（批 1 + 批 2）

> **For agentic workers:** REQUIRED SUB-SKILL: 用 `subagent-driven-development`（推荐）或 `executing-plans` 逐任务实施。步骤用 `- [ ]` 复选框跟踪。

**Goal:** 修掉 v2.5.1 Max level 审查报告中「验证体系可信度」类缺口（E-1 / G-4 / E-3 / G-2 / G-3）与 `verify-pack` 截断检测，以及 4 个 P3 产品缺陷（D-5 / D-6 / C-3 / C-6）。

**Architecture:** 两类改动。①**验证体系**：让「声称」与「实测」双向对账（分母断言、反向遍历、白名单化 + 剩余即 FAIL、计数正则归一化），并让特殊值标签回归在**全部**消费通道可见。②**产品缺陷**：沿用本仓库既有策略——**仅在主路径失效时启用回退**，常规量纲逐位不变。

**Tech Stack:** C# (net48 + net8.0-windows) / xUnit + FluentAssertions / PowerShell 5.1 门禁脚本 / Python 交叉验证（numpy/scipy/sqlite3） / Excel-DNA。

**Spec:**
- `logs/reports/release/review-2026-10-10-max-level-release-2.5.1.md`（27 条 finding，P0×0 / P1×1 / P2×10 / P3×16）
- `logs/reports/release/review-2026-10-10-fixes-and-manual-audit.md`（已完成修复与实例核对）

## Global Constraints

- **只审查/修复，不引入新依赖**：新增 NuGet 必须双 TFM 可用；禁止单框架依赖。
- **Public 签名不变**：UDF 参数/返回值、`[ExcelFunction]` 声明一律不动（本计划全部为内部实现与脚本改动）。
- **`src/` 内禁止裸 `catch {}`**，`catch when` 必须排除 OOM/StackOverflow/AccessViolation（用既有 `ExceptionFilters.IsCatchable`）。
- **数值三律**：判据必须相对（与数据同尺度）；NaN/Inf/溢出三路径都要显式守卫；NaN 与任何值比较恒 false，守卫必须先判 NaN。
- **常规量纲逐位不变**：所有数值回退只在主路径非有限/退化时启用。
- **`*.ps1` 必须 CRLF + UTF-8 BOM**（`.gitattributes` + 治理自测场景 [6] 强制）；其余文本 LF。
- **每个缺陷必须「先注入复现 → 修 → 复测」**，并把复现用例转正为回归守卫。
- **收尾必须全绿**：`dotnet build`（0 警告 0 错误）、`dotnet test`（双 TFM）、`verify-docs`、`verify-udfgen`、`pre-commit-check`、`check-test-quality`、`tests/scripts/run-tests.ps1`、`verify-manual.py`、`coverage.ps1`。
- **可再生产物**：构建前若见 `src/**/*.dna`（非 `.tpl`）或体积异常的 `*-packed.xll`，先删再建（失败 pack 会留下截断产物并让后续 pack 持续失败）。

---

## 文件结构（改动映射）

| 文件 | 职责 | 涉及任务 |
| :--- | :--- | :--- |
| `src/Analytics/RegressionCore.cs` | D-5：条件数估计纳入既有异常包装 | T1 |
| `tests/CrossValRunner/Program.cs` | E-2：固定 InvariantCulture | T2 |
| `tests/scripts/test_governance_tools.ps1` | T2 的守卫：4 个宿主都必须固定 culture | T2 |
| `scripts/verify-manual.py` | E-1（三通道 null/NaN）、E-3（白名单）、C-6（矩阵类型守卫） | T3 / T5 / T8 |
| `scripts/verify-docs.ps1` | G-4（分母断言+反向）、G-2/G-3（计数归一化）、G-1（补救提示） | T4 / T9 / T7 |
| `tests/scripts/test_verify_docs.ps1` | G-2/G-3/G-4 的负向注入回归守卫；T7 的 fixture 清理 | T4 / T7 / T9 |
| `scripts/verify-pack.ps1` | 截断产物检测（取代 100 KB 下限） | T6 |
| `tests/scripts/test_governance_tools.ps1` | T6 的负向注入守卫 | T6 |
| `src/DataToolkit/JsonXmlCore.cs` | C-3：`XmlXPath` 结果条数上限 | T10 |
| `tests/DataToolkit.Tests/JsonXmlPivotCoreTests.cs` | C-3 回归守卫 | T10 |
| `src/Analytics/StatsCore.cs` | D-6：`HarmonicMean` maxAbs 预缩放 | T11 |
| `tests/Analytics.Tests/StatsCoreTests.cs` | D-5 / D-6 回归守卫 | T1 / T11 |

---

## Task 1: D-5 — 条件数估计纳入异常包装

**Files:**
- Modify: `src/Analytics/RegressionCore.cs:168`（`FitOLSCore` 内 `double condEst = R.ConditionNumber();`）
- Test: `tests/Analytics.Tests/RegressionCoreTests.cs`

**Interfaces:**
- Consumes: 既有 `ExceptionFilters.IsCatchable(Exception)`、`ErrorMsg.Get(string)`
- Produces: 无新 API；仅把 `NonConvergenceException` 转为 `ArgumentException`

- [ ] **Step 1: 写失败测试**

在 `tests/Analytics.Tests/RegressionCoreTests.cs` 末尾（`FactorImportance_tiny_column_not_constant` 之后）追加：

```csharp
    // 2026-10-10 审查 D-5：R.ConditionNumber() 曾落在既有 try/catch **之外**，
    // 列量级 ≥1e160 时它内部的托管 SVD 不收敛并抛 MathNet NonConvergenceException
    // （非 ArgumentException），用户只拿到裸 #VALUE!，而同族其它拒绝路径都给 cond 实测值
    // 与修复建议。修复后必须统一为 ArgumentException。
    [Fact]
    public void FitOLS_condition_number_nonconvergence_wrapped_as_ArgumentException()
    {
        var X = new double[5, 1] { { 1e160 }, { 2e160 }, { 3e160 }, { 4e160 }, { 5e160 } };
        var y = new double[] { 1, 2, 3, 4, 5 };
        var act = () => RegressionCore.FitOLS(X, y);
        act.Should().Throw<ArgumentException>()
           .WithMessage("*condition number*");   // 与同族守卫文案一致
    }
```

- [ ] **Step 2: 跑测试确认 FAIL**

Run: `dotnet test tests/Analytics.Tests/Analytics.Tests.csproj -c Debug -f net8.0-windows -m:1 --filter FitOLS_condition_number_nonconvergence_wrapped_as_ArgumentException`
Expected: FAIL —— 实际抛 `MathNet.Numerics.NonConvergenceException`（不是 `ArgumentException`）

- [ ] **Step 3: 实现**

`src/Analytics/RegressionCore.cs` 第 168 行原为：

```csharp
            double condEst = R.ConditionNumber();
```

改为（接在该行上方已有的 `// ... guard threshold 1e14` 注释块位置，保持原地）：

```csharp
            // 条件数估计必须与 QR 求解同处一个异常包装内：R.ConditionNumber() 内部走托管
            // SVD，列量级 ≥1e160 时不收敛并抛 MathNet NonConvergenceException——它**不是**
            // ArgumentException，会绕过下方所有参数化拒绝路径，用户只得到裸 #VALUE!
            // （2026-10-10 审查 D-5）。同族 LinalgCore.ConditionNumber/Solve/Rank 均先按
            // MaxAbs 归一化再分解，此处保持不归一化（归一化会改变既有 cond 数值与拒绝阈值），
            // 仅把异常类型对齐。
            double condEst;
            try { condEst = R.ConditionNumber(); }
            catch (Exception ex) when (ExceptionFilters.IsCatchable(ex))
            {
                throw new ArgumentException(
                    ErrorMsg.Get("REGRESS_RankDeficient") + " (condition number estimation failed to converge)", ex);
            }
```

- [ ] **Step 4: 跑测试确认 PASS**

Run: 同 Step 2
Expected: PASS

- [ ] **Step 5: 回归整模块**

Run: `dotnet test tests/Analytics.Tests/Analytics.Tests.csproj -c Debug -f net8.0-windows -m:1`
Expected: 全绿（原 997 + 1 = 998）

---

## Task 2: E-2 — CrossValRunner 固定 InvariantCulture

**Files:**
- Modify: `tests/CrossValRunner/Program.cs`（顶部）
- Test: `tests/scripts/test_governance_tools.ps1`（新增场景）

**Interfaces:**
- Consumes: 无
- Produces: 无新 API；`CrossValRunner` 进程内 culture 固定

- [ ] **Step 1: 写失败守卫（治理自测新增场景）**

在 `tests/scripts/test_governance_tools.ps1` 的场景 `[6] 编码不变量` 之后追加：

```powershell
# --- [7] 测试宿主 culture 固定（2026-10-10 审查 E-2）---
# 背景：3 个 xUnit 工程都有 TestCultureSetup.cs 以 [ModuleInitializer] 固定 InvariantCulture，
# 但 Python↔C# 通道的 C# 半边（tests/CrossValRunner）没有任何固定 → tr-TR/de-DE 机器上
# 字符串/日期格式化用例会产生"环境归因"的间歇失败。此场景为 4 个宿主全部纳入守卫。
Write-Host ""
Write-Host "=== [7] 测试宿主 culture 固定（E-2）==="
$cultureHosts = @(
    @{ Name = 'Foundation.Tests';  Path = 'tests/Foundation.Tests/TestCultureSetup.cs' },
    @{ Name = 'Analytics.Tests';   Path = 'tests/Analytics.Tests/TestCultureSetup.cs' },
    @{ Name = 'DataToolkit.Tests'; Path = 'tests/DataToolkit.Tests/TestCultureSetup.cs' },
    @{ Name = 'CrossValRunner';    Path = 'tests/CrossValRunner/Program.cs' }
)
$cultureBad = @()
foreach ($h in $cultureHosts) {
    $p = Join-Path $repo $h.Path
    if (-not (Test-Path $p)) { $cultureBad += "$($h.Name): 文件缺失 $($h.Path)"; continue }
    $txt = [System.IO.File]::ReadAllText($p, [System.Text.Encoding]::UTF8)
    if ($txt -notmatch 'DefaultThreadCurrentCulture\s*=\s*CultureInfo\.InvariantCulture') {
        $cultureBad += "$($h.Name): 未固定 DefaultThreadCurrentCulture"
    }
}
if ($cultureBad.Count -eq 0) {
    Write-Host "  [PASS] 4 个测试宿主均固定 InvariantCulture"
} else {
    Write-Host "  [FAIL] culture 未固定：$($cultureBad -join '; ')" -ForegroundColor Red
    $script:failCount++
}
```

> `$repo` 变量名与该脚本既有约定一致；若脚本内使用的是其它名字（如 `$repoRoot`），沿用既有名字。

- [ ] **Step 2: 跑守卫确认 FAIL**

Run: `powershell -NoProfile -ExecutionPolicy Bypass -File tests/scripts/test_governance_tools.ps1`
Expected: `[FAIL] culture 未固定：CrossValRunner: 未固定 DefaultThreadCurrentCulture`

- [ ] **Step 3: 实现**

`tests/CrossValRunner/Program.cs` 文件最前面（在现有 `using System.Text.Json;` 之前）插入：

```csharp
using System.Globalization;

// 2026-10-10 审查 E-2：本宿主是 Python↔C# 交叉验证的 C# 半边，而 3 个 xUnit 工程都有
// TestCultureSetup.cs 以 [ModuleInitializer] 固定 InvariantCulture——本进程没有任何固定，
// 于是在 tr-TR/de-DE 等默认 culture 的机器上，字符串型 UDF（STR.FORMAT、DT.* 格式化、
// RANGE.TOJSON/TOCSV）与 ResultSerializer 的输出会偏离，产生"环境归因"的间歇性 FAIL/SKIP。
// 必须在任何被测代码执行前固定（结果序列化也依赖它）。
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
```

- [ ] **Step 4: 跑守卫确认 PASS**

Run: 同 Step 2
Expected: `[PASS] 4 个测试宿主均固定 InvariantCulture`

- [ ] **Step 5: 确认 CrossValRunner 仍可用**

Run: `dotnet build tests/CrossValRunner/CrossValRunner.csproj -c Debug -m:1` 然后 `python scripts/verify-manual.py`
Expected: exit 0，536 项全过

---

## Task 3: E-1 — 特殊值标签回归必须在全部消费通道可见

**Files:**
- Modify: `scripts/verify-manual.py:237-242`（`cross_check` null 分支）、`:335-340`（`cross_vs_csharp` null 分支）、`:1538`（矩阵通道）
- Test: 负向注入 `tests/CrossValRunner/ResultSerializer.cs`（注入后必须 3 通道 FAIL）

**Interfaces:**
- Consumes: 既有 `_contains_null(v)`（`verify-manual.py:64-68`）
- Produces: 无新公共接口

- [ ] **Step 1: 注入复现（必须先做，作为"缺陷存在"的证据）**

临时把 `tests/CrossValRunner/ResultSerializer.cs` 中 NaN 的序列化改为 `null`（**找 `__nan__` 的分支**），即把 `NaN` 标记输出改成 `null`，然后：

Run: `dotnet build tests/CrossValRunner/CrossValRunner.csproj -c Debug -m:1` 然后 `python scripts/verify-manual.py`
Expected（修复前）：**exit 0 全绿**（这正是缺陷：标签退回 null 时 3 条通道静默放过）

记录真实输出后**先还原** `ResultSerializer.cs`（`git checkout -- tests/CrossValRunner/ResultSerializer.cs`）。

- [ ] **Step 2: 实现（三处统一 FAIL 语义）**

`scripts/verify-manual.py:237-242` 的 `cross_check` 分支，原为：

```python
    # C# 真 null（不是 NaN/Inf）：Python 侧 NaN 视为匹配（向后兼容），否则 FAIL
    if cs_val is None and isinstance(python_computed, (float, np.floating)):
        if np.isnan(python_computed):
            PASS += 1; CROSS_PASS += 1; print(f"  OK {name}: NaN (C#=null)")
        else:
            FAIL += 1; print(f"  FAIL {name}: Python={python_computed}, C#=null (NaN)")
        return
```

改为：

```python
    # C# 真 null（不是 NaN/Inf）而 Python 侧期望 NaN = **特殊值标签失配**（2026-10-10 审查 E-1）。
    # 旧实现把二者视为等价（"向后兼容"），使 ResultSerializer 把 {"__nan__":true} 退回 null
    # 的回归在标量/bespoke/矩阵三条通道全部静默通过——只有 check() 的 ndarray 分支
    # （_contains_null，:89）能发现。标签是跨语言契约，缺失即 FAIL。
    if cs_val is None and isinstance(python_computed, (float, np.floating)):
        if np.isnan(python_computed):
            FAIL += 1
            print(f"  FAIL {name}: 特殊值标签失配 —— Python 期望 NaN，C# 发来裸 null（标签丢失）")
        else:
            FAIL += 1; print(f"  FAIL {name}: Python={python_computed}, C#=null")
        return
```

`:335-340` 的 `cross_vs_csharp` 分支做同样改造（把 `PASS += 1; CROSS_PASS += 1` 改为 `FAIL += 1` + 同一句「特殊值标签失配」文案）。

`:1538` 矩阵通道，原为：

```python
            if cv is None and (pv is None or np.isnan(pv)):
                continue
```

改为：

```python
            # 特殊值标签契约：C# 发来裸 null 而 Python 期望 NaN = 标签丢失 → FAIL 并指名单元格
            if cv is None and isinstance(pv, float) and np.isnan(pv):
                FAIL += 1
                print(f"  FAIL {name}: 特殊值标签失配（单元格[{ri}][{ci}]）—— Python 期望 NaN，C# 发来裸 null")
                continue
            if cv is None and pv is None:
                continue
```

- [ ] **Step 3: 复跑注入，确认 3 通道 FAIL**

再次执行 Step 1 的注入（改 `ResultSerializer.cs`）→ 跑 `verify-manual.py`
Expected（修复后）：**exit 非 0**，且输出含**至少 3 条**「特殊值标签失配」FAIL（标量 / bespoke / 矩阵各≥1）

记录真实输出后 **还原** `ResultSerializer.cs`，并确认 `git status` 中该文件干净。

- [ ] **Step 4: 复原后全绿**

Run: `python scripts/verify-manual.py`
Expected: exit 0，536 项全过

---

## Task 4: G-4 — check 17 分母断言 + 反向对账

**Files:**
- Modify: `scripts/verify-docs.ps1:553-561`（检查 17 比对循环之后）
- Test: `tests/scripts/test_verify_docs.ps1`（新增场景）

**Interfaces:**
- Consumes: 既有 `$apiParams`（文档侧）、`$srcParams`（源码侧）、`$codeUdfs`
- Produces: 无

- [ ] **Step 1: 写失败守卫（负向注入场景）**

在 `tests/scripts/test_verify_docs.ps1` 末尾（场景 V 之后）追加：

```powershell
# --- 场景 W（G-4）：api-reference 参数列去掉括号 → 必须 FAIL 并点名 ---
# 旧实现只遍历 $apiParams.Keys（docs→src 单向），且分母无断言：把某行参数列
# `(number1)` 改成 `number1` 后该 UDF 完全退出比对（实测分母 240→239 仍 PASS）。
Write-Host "[W] api-reference 参数列去括号应 FAIL（检查 17 分母断言）"
$fixtureW = Copy-RepoFixture
$apiW = Join-Path $fixtureW "docs\specification\api-reference.md"
$txtW = [System.IO.File]::ReadAllText($apiW, (New-Object System.Text.UTF8Encoding($false)))
$txtW = $txtW -replace '(?m)^(\|\s*`STATS\.MEAN`\s*\|)\s*\(number1\)', '$1 number1'
[System.IO.File]::WriteAllText($apiW, $txtW, (New-Object System.Text.UTF8Encoding($false)))
Run-VerifyDocs $fixtureW "api-reference params" $true
```

- [ ] **Step 2: 跑守卫确认 FAIL（即"门禁未拦住"）**

Run: `powershell -NoProfile -ExecutionPolicy Bypass -File tests/scripts/test_verify_docs.ps1`
Expected: `[FAIL] api-reference params (exit=0)` —— 注入后 verify-docs 仍 exit 0，证明缺口存在

- [ ] **Step 3: 实现**

`scripts/verify-docs.ps1` 检查 17 的比对循环（`foreach ($name in $apiParams.Keys) { ... }`）**之后**、`if ($paramMismatches.Count -eq 0)` **之前**插入：

```powershell
# 分母断言 + 反向对账（2026-10-10 审查 G-4）：提取正则要求参数列以 `(` 起始，文档表体
# 稍改格式（去括号、换列序）即该行不入 $apiParams → 该 UDF **静默退出比对**——实测把
# STATS.MEAN 的参数列改成 `number1` 后分母 240→239 仍 PASS。分母缩小是可见线索，
# 但此前无人断言；故此处显式要求文档侧条目数 == 源码 UDF 数，并补反向遍历。
if ($apiParams.Count -ne $codeUdfs) {
    $paramMismatches += "文档侧参数条目 $($apiParams.Count) != 源码 UDF 数 $codeUdfs（有 UDF 未参与比对）"
}
foreach ($name in $srcParams.Keys) {
    if (-not $apiParams.ContainsKey($name)) { $paramMismatches += "$name 源码有 UDF 而文档参数表无对应行" }
}
```

- [ ] **Step 4: 跑守卫确认 PASS**

Run: 同 Step 2
Expected: `[PASS] api-reference params (exit=1)` —— 注入被拦下

- [ ] **Step 5: 真实仓库仍全绿**

Run: `powershell -NoProfile -File scripts/verify-docs.ps1`
Expected: `Pass: 27  Fail: 0  Skip: 1`

---

## Task 5: E-3 — `[unmapped cross refs]` 白名单化，剩余即 FAIL

**Files:**
- Modify: `scripts/verify-manual.py`（`_cross_unmapped` 计算之后、`print(f"\n{'='*60}")` 之前）

**Interfaces:**
- Consumes: 既有 `_cross_unmapped`（`:1902-1903`）、`check()`
- Produces: 无

- [ ] **Step 1: 记录基线（当前 8 条常亮噪声）**

Run: `python scripts/verify-manual.py` 并抄下 `[unmapped cross refs]` 那一行的全部条目
Expected: 8 条 —— `DICT.FromKeys, DOE.TAGUCHI_L8, REGRESS.R², REGRESS.SSE, SOLVE.SHAREDCV, SOLVE.SHAREDCV_MAE, SOLVE.SHARED_RATE_COEF_CS, SOLVE.SHARED_RATE_INTER_CS`

- [ ] **Step 2: 实现**

在 `scripts/verify-manual.py` 的 `_cross_unmapped` 计算块之后插入：

```python
# 已知非 UDF 标签白名单（2026-10-10 审查 E-3）：这些引用不映射到任何公开 UDF 属**预期**——
# 它们是 Foundation 级对照或字段级合成名。旧实现每轮固定打印同一行却不参与判定 →
# "狼来了"：真正的漏映射（新 UDF 以 bespoke 标签对照却未登记 _ID2UDF）会被淹没，
# 使 216→222 这类对外覆盖数字静默少算。现改为白名单登记 + 其余硬 FAIL。
_NON_UDF_CROSS_LABELS = {
    "DICT.FromKeys",              # Foundation 级对照，无公开 DICT.FROMKEYS UDF
    "DOE.TAGUCHI_L8",             # 与 manifest_id DOE.TaguchiL8 拼写不一致的历史标签
    "REGRESS.R²", "REGRESS.SSE",  # 字段级合成名（上标 ² 不可能匹配 REGRESS.RSQ）
    "SOLVE.SHAREDCV", "SOLVE.SHAREDCV_MAE",
    "SOLVE.SHARED_RATE_COEF_CS", "SOLVE.SHARED_RATE_INTER_CS",
}
_unknown_cross = sorted(_x for _x in _cross_unmapped if _x not in _NON_UDF_CROSS_LABELS)
check("CROSS.unmapped-labels-whitelisted", _unknown_cross, [])
```

（保留既有 `print("[unmapped cross refs] ...")` 诊断行不变。）

- [ ] **Step 3: 跑测试确认 PASS**

Run: `python scripts/verify-manual.py`
Expected: exit 0；新增一行 `OK CROSS.unmapped-labels-whitelisted: []`；计数 536→537

- [ ] **Step 4: 注入新漏映射，确认 FAIL 且指名**

临时在 `scripts/verify-manual.py` 的 JSON 段加一条 `cross_vs_csharp("BOGUS.NEWUDF", 1, "JSON.PARSE")`，跑 `verify-manual.py`
Expected: `FAIL CROSS.unmapped-labels-whitelisted: got ['BOGUS.NEWUDF'], expected []`
随后**删除该注入行**。

---

## Task 6: `verify-pack.ps1` — 检测截断的 packed xll

**Files:**
- Modify: `scripts/verify-pack.ps1:16-36`（第 1 节体积检查）
- Test: `tests/scripts/test_governance_tools.ps1`（新增场景 [8]）

**Interfaces:**
- Consumes: 既有 `$xllFiles`、`$errors`、`$warnings`
- Produces: 无

- [ ] **Step 1: 写失败守卫**

在 `tests/scripts/test_governance_tools.ps1` 的场景 `[7]` 之后追加：

```powershell
# --- [8] verify-pack 必须检出截断产物（2026-10-10）---
# 背景：失败的 ExcelDnaPack 会留下**截断**的 *-packed.xll（实测 1,168,896 与 658,944 字节，
# 正常分别为 3,730,432 / 1,196,544），此后每次 pack 都失败在 EndUpdateResource，
# 错误信息完全指不到真因。旧判据 `$minSize = 100*1024` 对 1.17 MB 的截断产物**照常放行**。
Write-Host ""
Write-Host "=== [8] verify-pack 截断检测 ==="
$fx = Join-Path $env:TEMP ("packfx-" + [guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Path $fx -Force | Out-Null
New-Item -ItemType Directory -Path (Join-Path $fx "x64") -Force | Out-Null
New-Item -ItemType Directory -Path (Join-Path $fx "x86") -Force | Out-Null
# 32 位正常、64 位截断（约为 32 位的 18%）
[System.IO.File]::WriteAllBytes((Join-Path $fx "Analytics-AddIn-net48-packed.xll"),     (New-Object byte[] 1262080))
[System.IO.File]::WriteAllBytes((Join-Path $fx "Analytics-AddIn-net48-64-packed.xll"),  (New-Object byte[] 230000))
$out = & powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $repo "scripts/verify-pack.ps1") `
        -PublishDir $fx -Module Analytics -Tfm net48 2>&1 | Out-String
if ($LASTEXITCODE -ne 0 -and $out -match 'truncat|截断|size mismatch') {
    Write-Host "  [PASS] truncated packed xll detected (exit=$LASTEXITCODE)"
} else {
    Write-Host "  [FAIL] truncated packed xll NOT detected (exit=$LASTEXITCODE)" -ForegroundColor Red
    $script:failCount++
}
Remove-Item $fx -Recurse -Force -ErrorAction SilentlyContinue
```

- [ ] **Step 2: 跑守卫确认 FAIL**

Run: `powershell -NoProfile -ExecutionPolicy Bypass -File tests/scripts/test_governance_tools.ps1`
Expected: `[FAIL] truncated packed xll NOT detected (exit=0)` —— 截断产物被放过

- [ ] **Step 3: 实现**

`scripts/verify-pack.ps1` 第 1 节整体替换为：

```powershell
# 1. packed XLL 存在性 + **完整性**
#    绝对下限（旧判据 100 KB）抓不到**截断**产物：失败的 ExcelDnaPack 会留下部分写入的
#    xll（实测 1,168,896 / 658,944 字节，正常 3,730,432 / 1,196,544），而截断产物随后会让
#    每一次 pack 都失败在 EndUpdateResource（Win32Exception 110）且错误信息指不到真因。
#    判据改为：① PE 头可解析（MZ + PE\0\0）；② 同模块同 TFM 的 32/64 位两个产物体积比
#    在 [0.6, 1.7] 区间内（同源构建实测比值 0.95–1.06，截断时比值会掉到 0.2–0.5）。
$minSize = 256 * 1024
$xllFiles = @(
    "$PublishDir\$Module-AddIn-$tfmSuffix-packed.xll",
    "$PublishDir\$Module-AddIn-$tfmSuffix-64-packed.xll"
)
$sizes = @{}
foreach ($xll in $xllFiles) {
    if (-not (Test-Path $xll)) { $errors += "Missing packed XLL: $xll"; continue }
    $size = (Get-Item $xll).Length
    $sizes[$xll] = $size
    # ① PE 头：MZ(0x5A4D) + e_lfanew 处 PE\0\0
    $ok = $false
    try {
        $fs = [System.IO.File]::OpenRead($xll)
        try {
            $br = New-Object System.IO.BinaryReader($fs)
            if ($br.ReadUInt16() -eq 0x5A4D) {
                $fs.Position = 0x3C
                $peOff = $br.ReadInt32()
                if ($peOff -gt 0 -and $peOff -lt ($size - 4)) {
                    $fs.Position = $peOff
                    $ok = ($br.ReadUInt32() -eq 0x00004550)
                }
            }
        } finally { $fs.Close() }
    } catch { $ok = $false }
    if (-not $ok) { $errors += "$xll is not a valid PE image (truncated or corrupt): $size bytes"; continue }
    if ($size -lt $minSize) { $errors += "$xll size too small: $size bytes (min $minSize)"; continue }
    Write-Host "  [OK] $([System.IO.Path]::GetFileName($xll)) ($([math]::Round($size/1024)) KB)"
}
# ② 32/64 位产物体积比（同源构建比值接近 1；截断会显著偏离）
if ($sizes.Count -eq 2) {
    $vals = @($sizes.Values | Sort-Object)
    $ratio = $vals[0] / $vals[1]
    if ($ratio -lt 0.6 -or $ratio -gt 1.7) {
        $errors += "packed XLL size mismatch between bitness variants (ratio $([math]::Round($ratio,3))) — " +
                   "one of them is likely truncated: $(($sizes.GetEnumerator() | ForEach-Object { "$([System.IO.Path]::GetFileName($_.Key))=$($_.Value)" }) -join ', ')"
    }
}
```

- [ ] **Step 4: 跑守卫确认 PASS**

Run: 同 Step 2
Expected: `[PASS] truncated packed xll detected`

- [ ] **Step 5: 对真实 Release 产物跑一次**

Run: `powershell -NoProfile -File scripts/verify-pack.ps1 -PublishDir src/DataToolkit/bin/Release/net48/publish -Module DataToolkit -Tfm net48`（四个组合各跑一次）
Expected: 全部 `VERIFY-PACK PASSED`（当前 8 个产物体积比均正常）

---

## Task 7: G-1 — check 8 补救提示 + fixture 排除生成物

**Files:**
- Modify: `scripts/verify-docs.ps1:148-158`（检查 8 的 FAIL 消息）、`tests/scripts/test_verify_docs.ps1`（`Copy-RepoFixture`）

**Interfaces:**
- Consumes: 既有 `Copy-RepoFixture`（`test_verify_docs.ps1:34-45`）
- Produces: 无

- [ ] **Step 1: 复现（真实仓库注入残留 .dna）**

Run: `Set-Content src/Analytics/Analytics-AddIn-net48.dna -Value x` 然后 `powershell -File scripts/verify-docs.ps1`
Expected: `[FAIL] No residual .dna: found residual: /src/Analytics/Analytics-AddIn-net48.dna`（消息**不含**任何补救指引）
再跑 `powershell -File tests/scripts/run-tests.ps1`
Expected: `[FAIL] 失败: [powershell] test_verify_docs.ps1`（fixture 复制了该残留 → 基线场景全红）

- [ ] **Step 2: 实现（消息 + fixture）**

`scripts/verify-docs.ps1` 检查 8 的 `else` 分支改为：

```powershell
else {
    $rel = $residual | ForEach-Object { $_.FullName.Substring($RepoRoot.Length) -replace '\\', '/' }
    Check "No residual .dna" ("found residual: $($rel -join ', ') — 这些是 git-ignored 的构建生成物（中断的构建会留下），" +
        "清除后重跑：Get-ChildItem src -Recurse -Filter *.dna -File | Where-Object { `$_.Name -notlike '*.tpl' } | Remove-Item -Force；" +
        "若同时见体积异常的 *-packed.xll 请一并删除（失败的 pack 会留下截断产物）")
}
```

`tests/scripts/test_verify_docs.ps1` 的 `Copy-RepoFixture` 内、`robocopy` 之后、补 `logs` 目录之前插入：

```powershell
    # 2026-10-10 审查 G-1：仓库里 git-ignored 的 *-packed 生成物必须从 fixture 清除，
    # 否则场景 A/H3/I2/K3（要求"全部通过"）会被真实仓库的残留 .dna 污染而假失败——
    # 实测一处残留即让本自测 4 个场景红、跳过预算超限。生成物豁免的**信号**由场景 I1
    # （显式注入 .dna 断言 FAIL）显式覆盖，不依赖偶发残留。
    Get-ChildItem -Path $dst -Recurse -Filter "*.dna" -File -ErrorAction SilentlyContinue |
        Where-Object { $_.Name -notlike "*.tpl" } | Remove-Item -Force -ErrorAction SilentlyContinue
```

- [ ] **Step 3: 复测（残留仍在的情况下）**

Run: `powershell -File tests/scripts/run-tests.ps1`
Expected: **全绿**（fixture 已自清残留；场景 I1 仍能验证"残留应 FAIL"的信号）

- [ ] **Step 4: 清理残留并确认 verify-docs 的新消息**

Run: 删除 `src/Analytics/Analytics-AddIn-net48.dna`；`powershell -File scripts/verify-docs.ps1`
Expected: `Pass: 27  Fail: 0  Skip: 1`

---

## Task 8: C-6 — CrossVal 矩阵通道遇非数值单元格不再崩溃

**Files:**
- Modify: `scripts/verify-manual.py:1538-1546`（矩阵单元格比较循环）

**Interfaces:**
- Consumes: 既有 `cross_check_matrix` 的 `cv` / `pv` / `ri` / `ci`
- Produces: 无

- [ ] **Step 1: 注入复现**

临时把 `scripts/verify-manual.py` 中任一处 `cross_check_matrix(...)` 的**源 manifest id** 改成 `"XML.TOTABLE"`（该矩阵全为字符串），跑：

Run: `python scripts/verify-manual.py`
Expected（修复前）：抛未捕获 `ValueError: could not convert string to float: ...`，**脚本中止、连 RESULTS 摘要都不产出**（计数器完全未动）
记录真实输出后**还原**该处改动。

- [ ] **Step 2: 实现**

矩阵单元格比较处（`if cv is None ...` 分支**之前**）插入类型守卫：

```python
            # 2026-10-10 审查 C-6：矩阵通道此前对单元格值直接 float()，遇非数值（如
            # XML.TOTABLE 这类字符串矩阵）抛未捕获 ValueError → **整个 537 项验证中止且
            # 无摘要输出**，与 cross_vs_csharp 的 R3-22「字段缺失转 FAIL 不崩溃」口径不一致。
            if not isinstance(cv, (int, float, bool)) or not isinstance(pv, (int, float, bool)):
                FAIL += 1
                print(f"  FAIL {name}: 单元格类型不可比 [{ri}][{ci}] C#={cv!r} Python={pv!r}（SKIP 必须致命化但不得崩溃）")
                continue
```

> 注意：`bool` 是 `int` 子类，此处一并接受；字符串/None/字典走到本分支即 FAIL。

- [ ] **Step 3: 复跑注入确认优雅 FAIL**

再次执行 Step 1 的注入 → 跑 `verify-manual.py`
Expected: **exit 非 0**、输出含 `单元格类型不可比`、且**仍打印 RESULTS 摘要**（不再中止）
随后**还原**该处改动，确认 `git diff` 只剩预期改动。

- [ ] **Step 4: 复原后全绿**

Run: `python scripts/verify-manual.py`
Expected: exit 0

---

## Task 9: G-2 / G-3 — 计数扫描容忍 markdown 标记与全角数字

**Files:**
- Modify: `scripts/verify-docs.ps1:456-500`（检查 16）、`:631`（检查 20）
- Test: `tests/scripts/test_verify_docs.ps1`（新增 8 变体场景）

**Interfaces:**
- Consumes: 既有 `$text`（每文件文本）、`$codeUdfs`、`$codeFacts` / `$codeTheories`
- Produces: 无

- [ ] **Step 1: 写失败守卫（8 变体）**

在 `tests/scripts/test_verify_docs.ps1` 的场景 `[W]` 之后追加：

```powershell
# --- 场景 X（G-2）：markdown 标记 / 全角数字不得让检查 16 失明 ---
# 2026-10-10 实测：数字与量词之间插入加粗 / 行内代码 / 斜体标记，或写成全角数字、
# 数字带正号、量词加括号、前置"约"字、倒装加粗 —— 共 8 种写法全部逃逸
# （旧模式要求 \d 与量词/UDF 直接相邻）。逐变体独立注入 + 断言 FAIL。
# 注意：下面 $variants16 的字面量只能放在 .ps1 里——verify-docs 会扫描全部 *.md 的散文计数，
# 若把 `NNN 个 UDF` 这类字面量写进本计划文档，会被检查 16 当成真实计数声称而 FAIL
# （本计划首次提交时即如此，已改为说明）。
Write-Host "[X] 检查 16 计数变体（标记/全角）"
$variants16 = @(
    # 注意：本清单的字面量**只放在 .ps1 里**——verify-docs 会扫描全部 *.md 的散文计数，
    # 若把 `NNN 个 UDF` 这类字面量写进计划文档，会被检查 16 当成真实计数声称（且全角数字
    # 曾让门禁 [int] 转换抛错整脚本中止）。故此处以说明代替字面量。
    # 变体清单：加粗 / 行内代码 / 斜体 / 数字后加号 / 括注量词 / 全角数字 / "约"字前缀 / 倒装加粗
    (加粗), (行内代码), (斜体), (数字+加号), (括注"个"), (全角数字), ("约"+数字), (倒装+"数量："+加粗)
)
foreach ($v in $variants16) {
    $fixtureX = Copy-RepoFixture
    [System.IO.File]::AppendAllText((Join-Path $fixtureX "AGENTS.md"), "`n$v`n", (New-Object System.Text.UTF8Encoding($false)))
    Run-VerifyDocs $fixtureX "Prose UDF counts" $true
}
```

- [ ] **Step 2: 跑守卫确认 FAIL（8 条中有逃逸）**

Run: `powershell -NoProfile -ExecutionPolicy Bypass -File tests/scripts/test_verify_docs.ps1`
Expected: 多条 `[FAIL] Prose UDF counts (exit=0)`（旧正则放行）

- [ ] **Step 3: 实现（先归一化再计数）**

`scripts/verify-docs.ps1` 检查 16 循环内、`foreach ($m in ...)` 之前插入一次归一化，并把后续所有 `$text` 引用改为 `$scan`：

```powershell
    # 2026-10-10 审查 G-2/G-3：计数扫描前先归一化，否则数字与量词/UDF 之间插入 markdown
    # 标记（**加粗**、`代码`、_斜体_）或写成全角数字即整体失配——实测 15 个变体中 8 个逃逸，
    # 而中文文档里 `**240** 个 UDF` 是自然写法。归一化只删格式字符/转半角，不改语义。
    $scan = $text -replace '\*\*', '' -replace '`', '' -replace '_', '' -replace '~~', ''
    $scan = [string]::Join('', ($scan.ToCharArray() | ForEach-Object {
        $c = [int][char]$_
        if ($c -ge 0xFF10 -and $c -le 0xFF19) { [char]($c - 0xFF10 + 0x30) } else { $_ }
    }))
```

随后把该检查块内所有 `[regex]::Matches($text, ...)` 改为 `[regex]::Matches($scan, ...)`（模式 1a/1b/1c/1d、CrossVal 双通道、两种分数形式、模式 2 的区间链）——**共 8 处**。

检查 20（`:631`）同样处理：在其 `$text` 读取后加同样的 `$scan` 归一化，并把 `[regex]::Matches($text, '([\d,]+)\s*个?\s*\[(Fact|Theory)\]')` 改为扫 `$scan`。

- [ ] **Step 4: 跑守卫确认全部 PASS**

Run: 同 Step 2
Expected: 8 条变体全部 `[PASS] ... (exit=1)`

- [ ] **Step 5: 真实仓库仍全绿**

Run: `powershell -NoProfile -File scripts/verify-docs.ps1`
Expected: `Pass: 27  Fail: 0  Skip: 1`

---

## Task 10: C-3 — `XML.XPATH` 结果条数上限

**Files:**
- Modify: `src/DataToolkit/JsonXmlCore.cs:178`（`XmlXPath`）
- Test: `tests/DataToolkit.Tests/JsonXmlPivotCoreTests.cs`

**Interfaces:**
- Consumes: 既有 `ParseXmlSafe`、`ExceptionFilters.IsCatchable`
- Produces: `XmlXPath` 行为新增「超过 `MaxXPathResults` 返回空数组」；签名不变

- [ ] **Step 1: 写失败测试**

在 `tests/DataToolkit.Tests/JsonXmlPivotCoreTests.cs` 的 `JsonCoverageGapTests` 类内追加：

```csharp
        // 2026-10-10 审查 C-3：XML.XPATH 此前直接 XPathSelectElements(...).ToArray()，
        // 无结果条数上限——同族 XmlToTable / JsonToTable 都在分配前拒绝 > 100_000 行。
        // 复现：100_001 个同名元素应被拒绝（返回空数组），100_000 个仍正常返回。
        [Fact] public void XmlXPath_result_count_cap_enforced()
        {
            var big = "<r>" + string.Concat(Enumerable.Repeat("<a>1</a>", 100_001)) + "</r>";
            JsonXmlCore.XmlXPath(big, "//a").Should().BeEmpty("超过 100k 结果应被拒绝");

            var ok = "<r>" + string.Concat(Enumerable.Repeat("<a>1</a>", 100_000)) + "</r>";
            JsonXmlCore.XmlXPath(ok, "//a").Length.Should().Be(100_000);
        }
```

- [ ] **Step 2: 跑测试确认 FAIL**

Run: `dotnet test tests/DataToolkit.Tests/DataToolkit.Tests.csproj -c Debug -f net8.0-windows -m:1 --filter XmlXPath_result_count_cap_enforced`
Expected: FAIL —— 100_001 个元素被照常返回（`BeEmpty` 失败）

- [ ] **Step 3: 实现**

`src/DataToolkit/JsonXmlCore.cs` 中 `XmlXPath` 原为：

```csharp
        internal static string[] XmlXPath(string xml, string xpath)
        { try{var d=ParseXmlSafe(xml);return d.XPathSelectElements(xpath).Select(e=>e.Value).ToArray();}catch(Exception ex) when(ExceptionFilters.IsCatchable(ex)){System.Diagnostics.Debug.WriteLine($"[XmlXPath] Failed: {ex.Message}");return Array.Empty<string>();} }
```

改为：

```csharp
        /// <summary>结果条数上限：与 XmlToTable / JsonToTable 的 100_000 行上限同口径，
        /// 在**物化数组之前**判定（2026-10-10 审查 C-3——此前无上限，直调方可用一条
        /// `//x` 让 XPath 结果无界膨胀）。</summary>
        private const int MaxXPathResults = 100_000;

        internal static string[] XmlXPath(string xml, string xpath)
        {
            try
            {
                var d = ParseXmlSafe(xml);
                var hits = d.XPathSelectElements(xpath);
                int n = 0;
                var list = new System.Collections.Generic.List<string>();
                foreach (var e in hits)
                {
                    if (++n > MaxXPathResults)
                    {
                        System.Diagnostics.Debug.WriteLine($"[XmlXPath] result count exceeds {MaxXPathResults}; rejected.");
                        return Array.Empty<string>();
                    }
                    list.Add(e.Value);
                }
                return list.ToArray();
            }
            catch (Exception ex) when (ExceptionFilters.IsCatchable(ex))
            {
                System.Diagnostics.Debug.WriteLine($"[XmlXPath] Failed: {ex.Message}");
                return Array.Empty<string>();
            }
        }
```

- [ ] **Step 4: 跑测试确认 PASS**

Run: 同 Step 2
Expected: PASS

- [ ] **Step 5: 回归整模块**

Run: `dotnet test tests/DataToolkit.Tests/DataToolkit.Tests.csproj -c Debug -f net8.0-windows -m:1`
Expected: 全绿（1569 + 1 = 1570）

---

## Task 11: D-6 — `HARMEAN` maxAbs 预缩放

**Files:**
- Modify: `src/Analytics/StatsCore.cs:36-45`（`HarmonicMean`）
- Test: `tests/Analytics.Tests/StatsCoreTests.cs`

**Interfaces:**
- Consumes: 既有 `AllEqual(double[])`（本轮已加入 `StatsCore`）
- Produces: 无签名变化

- [ ] **Step 1: 写失败测试**

在 `tests/Analytics.Tests/StatsCoreTests.cs` 的 D-2/D-3 修复区之后追加：

```csharp
    [Fact] public void HarmonicMean_subnormal_scale_recovers_value()
    {
        // 2026-10-10 审查 D-6：HM = n / Σ(1/x)，|x| < 5.6e-309 时 1/x 上溢 +Inf →
        // n/Inf = 0（**有限值**，逃过 IsInfinity 输出封顶）→ 静默返回 0，而真值 1e-310
        // 完全可表示。同族 GeometricMean 走对数域天然免疫（对照用例）。
        StatsCore.HarmonicMean(new[] { 1e-310, 1e-310 }).Should().BeApproximately(1e-310, 1e-320);
        StatsCore.HarmonicMean(new[] { 1e-320, 1e-320 }).Should().BeApproximately(1e-320, 1e-330);
        // 常规量纲逐位不变 + 既有语义不得回归
        StatsCore.HarmonicMean(new[] { 1e-300, 2e-300 }).Should().BeApproximately(1.3333333333333335e-300, 1e-310);
        StatsCore.HarmonicMean(new[] { 1.0, 2.0, 4.0 }).Should().BeApproximately(12.0 / 7.0, 1e-12);
        StatsCore.HarmonicMean(new[] { 0.0, 0.0 }).Should().Be(0.0);
        double.IsNaN(StatsCore.HarmonicMean(new[] { 1.0, -2.0 })).Should().BeTrue("负输入无定义");
        StatsCore.GeometricMean(new[] { 1e-320, 1e-320 }).Should().BeApproximately(1e-320, 1e-330);
    }
```

- [ ] **Step 2: 跑测试确认 FAIL**

Run: `dotnet test tests/Analytics.Tests/Analytics.Tests.csproj -c Debug -f net8.0-windows -m:1 --filter HarmonicMean_subnormal_scale_recovers_value`
Expected: FAIL —— `HarmonicMean([1e-310,1e-310])` 实测 `0`（期望 1e-310）

- [ ] **Step 3: 实现**

`src/Analytics/StatsCore.cs` 的 `HarmonicMean` 原为：

```csharp
        internal static double HarmonicMean(double[] d)
        {
            if (d.Length == 0) return double.NaN;
            // Harmonic mean is undefined for negative input (scipy → nan, Excel → #NUM!).
            // MathNet returns +Inf for [-1,1] (2/0) and a meaningless value for [1,-2,3].
            for (int i = 0; i < d.Length; i++)
                if (d[i] < 0) return double.NaN;
            var r = Statistics.HarmonicMean(d);
            return double.IsInfinity(r) ? double.NaN : r;  // output cap (file convention)
        }
```

改为：

```csharp
        internal static double HarmonicMean(double[] d)
        {
            if (d.Length == 0) return double.NaN;
            // Harmonic mean is undefined for negative input (scipy → nan, Excel → #NUM!).
            // MathNet returns +Inf for [-1,1] (2/0) and a meaningless value for [1,-2,3].
            for (int i = 0; i < d.Length; i++)
                if (d[i] < 0) return double.NaN;
            var r = Statistics.HarmonicMean(d);
            if (double.IsInfinity(r)) return double.NaN;   // output cap (file convention)
            if (double.IsNaN(r)) return double.NaN;
            // 下溢/上溢的中间量（2026-10-10 审查 D-6）：|x| < 1/DBL_MAX ≈ 5.6e-309 时
            // Σ(1/x) 上溢 +Inf → n/Inf = 0（有限值，逃过上面的封顶）→ 静默返回 0，而真值
            // 完全可表示。调和均值对正缩放等变：HM(x) = c·HM(x/c)，取 c = max|x| 后
            // |x/c| ≤ 1 ⇒ 1/(x/c) ≥ 1 不再上溢。仅在结果退化时启用，常规量纲逐位不变。
            if (r != 0.0) return r;
            double c = 0;
            foreach (double x in d) { double a = Math.Abs(x); if (a > c) c = a; }
            if (c == 0.0) return 0.0;                      // 全零数组 → HM = 0
            var scaled = new double[d.Length];
            for (int i = 0; i < d.Length; i++) scaled[i] = d[i] / c;
            double rs = Statistics.HarmonicMean(scaled) * c;
            return double.IsNaN(rs) || double.IsInfinity(rs) ? double.NaN : rs;
        }
```

- [ ] **Step 4: 跑测试确认 PASS**

Run: 同 Step 2
Expected: PASS

- [ ] **Step 5: 回归整模块**

Run: `dotnet test tests/Analytics.Tests/Analytics.Tests.csproj -c Debug -f net8.0-windows -m:1`
Expected: 全绿（998 + 1 = 999）

---

## Task 12: 收尾验证与文档同步

**Files:**
- Modify: `README.md`（verify-manual 计数）、`docs/specification/specification.md`（`[Fact]` 计数）、`AGENTS.md`/`ai-review-prompt.md`（若引用数字变化）、`logs/reports/release/review-2026-10-10-fixes-and-manual-audit.md`（补本轮）

- [ ] **Step 1: 全量构建与测试**

```powershell
$env:MSBUILDDISABLENODEREUSE='1'
dotnet build ExcelFormulaLabs.sln -c Debug -m:1 --nologo -v q
dotnet test  ExcelFormulaLabs.sln -c Debug -m:1 --no-build --nologo -v q
dotnet build ExcelFormulaLabs.sln -c Release -m:1 --nologo -v q
```
Expected: 0 警告 0 错误；测试全绿（用例数 = 3031 + 本轮新增 3）

- [ ] **Step 2: 全部门禁**

```powershell
powershell -NoProfile -File scripts/verify-docs.ps1
powershell -NoProfile -File scripts/verify-udfgen.ps1
powershell -NoProfile -File scripts/pre-commit-check.ps1
powershell -NoProfile -File scripts/check-test-quality.ps1
powershell -NoProfile -File tests/scripts/run-tests.ps1
python scripts/verify-manual.py     ; $LASTEXITCODE
powershell -NoProfile -File scripts/coverage.ps1
```
Expected: 全绿（`verify-manual.py` 计数因 T5 白名单断言 +1 → 537）

- [ ] **Step 3: 同步被门禁点名的计数**

按各门禁 FAIL 输出逐条更新（README 的 `manual-only N / cross-validated M` 与 `X/240`、`specification.md` 的 `[Fact]` 计数），直到 Step 2 全绿。

- [ ] **Step 4: 真机门禁**

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/test-xll.ps1 -Rounds 1
```
Expected: 286/286（本轮未改 UDF 层，数字应不变）

- [ ] **Step 5: 编码与行尾不变量**

```powershell
python -c "import subprocess,os;root=r'D:\Workspace\zgrwo\Harmonization\ExcelAddin函数库';print(subprocess.run(['git','-C',root,'diff','--name-only'],capture_output=True,text=True).stdout)"
```
对每个 `*.ps1`：必须 CRLF + UTF-8 BOM（治理自测场景 [6] 会拦）。对 `.py/.md/.json/.cs`：LF。

- [ ] **Step 6: 追加报告**

在 `logs/reports/release/review-2026-10-10-fixes-and-manual-audit.md` 追加「批 1 + 批 2 修复记录」小节：逐条列「注入复现输出 → 修复 → 复测输出」，以及最终门禁表。

---

## 自审记录

**Spec 覆盖**：审查报告 27 条中，本计划覆盖 E-1 / E-3 / G-2 / G-3 / G-4 / D-5 / D-6 / C-3 / C-6（9 条）+ 本轮新发现的 `verify-pack` 截断检测 / check 8 补救提示 / fixture 生成物豁免（3 条）。
**明确不在本计划内**（需另行决策，理由见报告）：
- **C-4**（正则数组级预算）：统一入口涉及 Foundation `MapOver` 与 UDF 生成物（`StringUdf.g.cs` 不可手改），有跨模块影响面，应走一次架构决策（ADR）而非顺带修。
- **C-2**（`ValidatePath` 尾随空格）：需同时评定"段级 TrimEnd"与"改判最终解析路径"两种方案对既有合法路径的影响。
- **C-5**（pre-commit 检查 5 逐除法点）：需重写为括号平衡/方法体级解析，属门禁重构。
- **D-1..D-4 / C-1**：已在上一轮修复完成。
- **test-xll 接入 CI / 示例工作簿校验**：需基础设施（装 Excel 的 self-hosted runner）与维护者决策。

**类型一致性**：`AllEqual(double[])`（Task 11 依赖）已在本轮 D-2 修复中引入并位于 `StatsCore`；`ExceptionFilters.IsCatchable` 在 `Foundation` 且被 `RegressionCore`/`JsonXmlCore` 既有引用；`check()` / `Run-VerifyDocs` / `Copy-RepoFixture` / `$repo` 均为脚本既有约定名。
