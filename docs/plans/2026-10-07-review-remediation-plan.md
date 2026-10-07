# 评审缺陷整改计划（2026-10-07）

> 基线：`0fc992d`（main HEAD，落后 tag `v2.4.2` 一个提交）。
> 来源：2026-10-07 深度评审（架构 / 算法 / 实现 / 一致性 / 验证五维 + 真实 Excel 封送实测）。
> 状态标记：`[ ]` 未开始 / `[x]` 已完成（验证通过） / `[~]` 进行中。

---

## 一、验证结论（实施前逐条复核）

所有结论均在 `0fc992d` 基线上以**运行时复现**或**真实 Excel 实测**取得，不接受仅读码判定。
复核用例落入 `tests/Analytics.Tests/ReviewVerificationTests.cs` 与
`tests/DataToolkit.Tests/ReviewVerificationTests.cs`（整改后并入各模块正式测试文件）。

| 编号 | 结论 | 复现证据（实测） | 外部 oracle |
| :--- | :--- | :--- | :--- |
| **A1** | **确认**：`LINALG.DET` 是分解族唯一未做 `MaxAbs` 归一化的入口，静默错值且依赖行列顺序 | `det(diag(1e300,1e300,1e-300,1e-300))` → **NaN**；对角逆序 → **0.0** | `numpy.linalg.det` 两序皆 **1.0**（真值精确为 1） |
| **A2** | **确认**：`FitOLS(addIntercept:false)` 的 R² 用中心化 TSS | `0.923469387755102` | `statsmodels.OLS(y,X)` 无常数项 → **0.9829931972789115**；手算 β=17/14、SSE=0.3571428…、非中心化 TSS=Σy²=21 |
| **A3** | **确认**：同一 `df` 键两种语义，且同表输出 | `FitOLS["df"]`=**2**（n−p，含截距 p=3）；`FitRidge["df"]`=**3**（=p） | 残差自由度定义唯一 |
| **A4** | **口径不一致**（非独有错值）：`AnovaOneWay` 用 LINQ `Average()` 朴素求和，合法有限大值被误拒 | 1e308 量纲两组 → 抛 "Input values are too large in magnitude" | `scipy.stats.f_oneway` **同样返回 NaN**；缩小 1e307 倍后 F=0.06 → 真值有限 |
| **A5** | **库内不一致**：`SolveCore.Median` 偶数长度用 `0.5*(lo+hi)`，±MaxValue 溢出 | `Median([MaxValue,MaxValue])` → **Inf** | `numpy.median` 同样 Inf；但 `StatsCore.Median` 已有凸组合回退 → 库内口径分裂 |
| **A6** | **确认（代码层）**：可达性容差自身可溢出 | `SolveCore.cs:1290` `maxOut−minOut` 在 ±1e308 下 → Inf → `tol=Inf` → `inRange` 恒真 | 纵深防御缺失 |
| **B1** | **确认（真实 Excel）**：省略必选参数显示 `#NUM!` | `=STR.REVERSE()` → `#NUM!`（应 `#VALUE!`）；net48 与 net8 一致 | `ElementWiseMapper.cs:121-125` 已在 `MapOverMulti` 修过同类 |
| **B2** | **口径修正**：文档称空白单元格"通常 #NUM!"，实测 `#VALUE!` | `=STATS.MEAN(<空白>)` → `#VALUE!` | api-reference「空白单元格作为必选数值参数」条 |
| **B3** | **口径说明**：空白单元格经字符串函数显示 `0` | `=STR.REVERSE(<空白>)` → `0` | 手册称"视为空串"；Excel 对空结果即渲染 0 |
| **C1** | **确认（运行时复现）**：`rng` 建在请求行循环之外（`:1194` 在 `:1195` 的 `for` 之前）→ 结果依赖请求行位置 | 两个**完全相同**的请求行 → `3.999999996847408` vs `3.999999997003461` | 与 ADR-0007「确定性」承诺矛盾。注：超范围目标因两端被钳制会掩盖该差异 |
| **C2** | **确认（代码层）**：目标函数是加权和而非字典序，且 `bestU` 按目标函数值选 | `:1250` `1e6·fTarget + 0.02·proximity`；`:1270` 按 `e` 选 | proximity ≤ v ≤ 20 → σ ∈ (1e-6, 6.3e-4) 窗口内可反超；ADR-0007 决策 4 声称"字典序化" |
| **C3** | **推翻（实测不成立）** | 初判"最重的 SOLVE.INVERSE 同步跑在计算线程上 → 分钟到小时级无响应"。实测（n=5000、3 变量、poly、200 请求行、`max_starts=50` 即文档上限）：**358 ms**；auto 模型 1 请求行 113 ms | ADR-0007 的触发条件 "n=5000 同步基准 >3s" **不成立**，无需异步/预算改造 |
| **E11** | **推翻（误判历史记录）** | 初判「236 个函数」为陈旧漂移。复核：`specification.md:108` 位于**历史演化摘要**表（"初始版本 \| 06-22 \| 236 个函数"），`adr/0003:8` 为决策时点描述 | 两者都是**准确的历史记录**；且把「N 个函数」加入 verify-docs 检查 16 词表会对其**误报**——该扩表方案一并撤销 |
| **D1** | **确认（运行时复现）**：`DT.UNIX` 跨时区不一致 | `UnixTimestamp(Unspecified 2024-01-01 00:00)` = **1704038400**；同墙上时刻 Kind=Utc = **1704067200**（差 28800s = UTC+8） | 根因链已证：`FromOADate` → Kind=Unspecified |
| **E1** | **推翻（本地检出过期，非项目缺陷）** | 初判"主干门禁红"：`verify-docs.ps1` → `Pass 25 / Fail 2 / Skip 1`。复核 `git status -sb` → **`main...origin/main [behind 1]`**，`origin/main` = `d3750eb` = tag `v2.4.2`，且 `origin/main..v2.4.2` 为空 | **远端主干是绿的**；FAIL 源自本地未 `git pull`。`git pull --ff-only`（或先提交/暂存本地改动）即消除，无需改 workflow |
| **E2** | **确认**：求和精度一库两制 | `StatsCore.Sum([1e16,1,-1e16])` = **0.0** | `math.fsum` = **1.0**；`PivotCore.cs:53` 已有 Neumaier |
| **A1c/A2b/D1b** | 对照通过 | 常规量纲 det=120；含截距 R²=0.9642857142857143；Unix 已知值 1704067200 | 防止修复引入回归 |

**被推翻/降级的原结论**：

- **A4、A5 不是"本库独有的错值"**——scipy/numpy 在同一输入下同样溢出。真实问题是**库内口径分裂**（同一团队在别处已实现缩放/补偿），因此修复目标是"对齐内部口径 + 改善诊断"，而非"纠正错误答案"。
- **A3 的绝对值归属与子代理报告相反**：`FitOLS` 给的是 `n−p`（含截距 = 2），`FitRidge` 给的是 `p`（= 3）。缺陷（同键反义）成立，但方向需按实测记录。

---

## 二、整改计划

### P0 — 静默错值与可用性（本轮实施）

| # | 项 | 修复方案 | 验收断言 |
| :--- | :--- | :--- | :--- |
| P0-1 | A1 `Determinant` | 与分解族对齐：先 `MaxAbs` 归一化，用 `det(cA)=cⁿ·det(A)` 还原 | `det(diag(1e300,1e300,1e-300,1e-300))≈1`；逆序同值；常规量纲 120 不回归 |
| P0-2 | A3 `FitRidge["df"]` | 改为残差自由度 `n−p`（与 `FitOLS` 同语义）；`p` 另立 `param_count` 键 | `FitOLS["df"] == FitRidge["df"]`（同输入同选项） |
| P0-3 | A2 无截距 R² | `addIntercept=false` 时 TSS 改非中心化 `Σy²`；`adj_r_squared` 同步口径 | `FitOLS(X,y,false)["r_squared"]≈0.9829931972789115`；含截距不回归 |
| P0-4 | D1 `UnixTimestamp` | 显式 `DateTimeKind.Utc`（或改 `DateTimeOffset`），消除隐式本地时区；`FromUnixTimestamp` 同步 | 同墙上时刻不同 Kind 结果一致；已知值 1704067200 |
| P0-5 | C1 SOLVE `rng` | 把 `new XorShift64((ulong)seed)` 移入请求行循环（每行独立同种子） | 两个相同请求行逐位相同（含可达目标夹具） |
| P0-6 | B1 `MapOver` 顶层 null | 单参标量路径不得返回顶层 `null`（Excel 渲染 `#NUM!`）；统一为 `ExcelError.Value` | 真实 Excel：`=STR.REVERSE()` → `#VALUE!` |

### P1 — 数值守卫与语义一致性（本轮实施）

| # | 项 | 修复方案 | 验收断言 |
| :--- | :--- | :--- | :--- |
| P1-1 | A6 可达性容差 | `magnitude` 计算改为不产生 Inf 的等价式（如 `max(|min|,|max|,|max|+|min|)` 或按输出尺度归一） | ±1e308 输出下 `tol` 有限，状态判定不恒为"可达" |
| P1-2 | A5 `SolveCore.Median` | 复用 `StatsCore` 的凸组合中位数（或提取到 Foundation 共用） | `Median([MaxValue,MaxValue])` 有限 |
| P1-3 | A4 `AnovaOneWay` | 复用同文件 `IncrementalMean` + 缩放回退，与 `StatsCore` 口径一致 | 1e308 量纲与 1e307 缩小后 F 一致 |
| P1-4 | E2 `StatsCore.Sum` | 提取 `PivotCore.NeumaierAdd` 到 Foundation 并用于 `Sum`/`CenteredSS`/TSS | `Sum([1e16,1,-1e16]) == 1.0`；`PIVOT.SUM` 不回归 |
| P1-5 | C2 目标函数 | 按 ADR-0007 承诺落地真字典序（先比 `fTarget`，等价集内再用 proximity），并让 `bestU` 以 σ 为准 | 构造窗口用例：σ 更小者必须胜出；ADR 与实现一致 |
| P1-6 | C3 SOLVE 时间预算 | 为 `SOLVE.INVERSE` 加评估次数/墙钟预算（对齐 `SqlCore` 预算模式），超限显式抛错 | 超限抛可读异常（而非无响应）；正常用例不回归 |

### P2 — 文档与库内口径收敛（本轮实施）

| # | 项 | 修复方案 |
| :--- | :--- | :--- |
| P2-1 | B2/B3 文档口径 | api-reference「空白单元格」条与 user-manual 字符串函数空值条按实测修正（`#VALUE!` / 渲染 `0`） |
| P2-2 | E5 `STATS.SIGN` 漂移 | api-reference 返回类型改为实际语义（标量入标量出），或实现改为恒返数组——择一并同步 |
| P2-3 | E11 陈旧数字 | `specification.md`、`adr/0003` 的「236 个函数」对齐 240；补 verify-docs 检查 16 词表「N 个函数」 |
| P2-4 | E4 `IsOmitted` 收敛 | `SolveUdf.cs:65/73`、`SqlCore.cs:299/325`、`AnalyticsHelpers.cs:78-80/101-103` 统一走 `InputNormalizer.IsOmitted` |
| P2-5 | E3 错误目录 | `SolveCore` 77 条硬编码英文异常并入 `ErrorMsg` 资源；加门禁禁止 Core 内联异常字面量 |

### P3 — 结构性重构（后续独立计划，本轮不实施）

UDF 元数据化 + 源生成（消 191 个复制粘贴体）、`SolveCore` 2282 行拆分、宿主适配程序集、
CrossVal 改打 UDF 入口、覆盖率分支门禁、Excel E2E 进 CI、`Category=` 分类。
理由：均属高风险大改动，需独立 ADR 与分批验证，不与本轮缺陷修复混提。

### 流程项

| # | 项 | 处置 |
| :--- | :--- | :--- |
| F-1 | E1 主干 vs tag | **撤销**：复核证明是本地检出落后 `origin/main` 一个提交，非发版回路缺陷；`release-please.yml` 无需改动。用户侧执行 `git pull --ff-only` 即恢复全绿 |
| F-2 | 同类扫描机制 | 每修一处数值守卫，PR 内强制列出同族候选点并逐条勾选（本轮 A1/A4/A5/E2 即该机制的首批产出） |

---

## 三、实施顺序与门禁

1. P0 批（6 项）→ 构建 + 定向测试
2. P1 批（6 项）→ 构建 + 定向测试
3. P2 批（5 项）→ 构建 + 定向测试
4. 全量门禁：`verify-docs.ps1` / `dotnet test`（双 TFM）/ `verify-manual.py` / `pre-commit-check.ps1` / `check-test-quality.ps1` / Release 构建
5. 真实 Excel 复测 B1（封送路径只在真机可验）

**不改的红线**：Core 零 `ExcelDna` 引用、UDF 仅分发、`catch when` 异常过滤器、
Regex 超时、SQL 参数化、表头/哨兵契约、双 TFM 兼容、既有公共签名。

---

## 四、实施状态（2026-10-07 完成）

| 项 | 状态 | 验证方式 |
| :--- | :--- | :--- |
| P0-1 `LINALG.DET` 逐行尺度归一 + 对数域累积 | `[x]` | `tests/Analytics.Tests/ReviewVerificationTests.cs` A1/A1b（NaN→1.0、顺序无关）+ A1c 对照 |
| P0-2 `REGRESS.RIDGE` df = n−p | `[x]` | A3（与 OLS 同值）；api-reference + CHANGELOG 同步 |
| P0-3 无截距 R² 用非中心化 TSS | `[x]` | A2（0.9829931972789115）+ A2b 含截距对照 |
| P0-4 `DT.UNIXTS/FROMUNIX` 墙上时刻语义 | `[x]` | `tests/DataToolkit.Tests/ReviewVerificationTests.cs` D1/D1b/D1c |
| P0-5 SOLVE rng 移入请求行循环 | `[x]` | C1（两行逐位相同）+ C1b 真解对照 |
| P0-6 省略必选参数 → `#VALUE!` | `[x]` | Foundation/DataToolkit 断言更新；**待真实 Excel 复测** |
| P1-1 可达性容差防溢出 | `[x]` | 有限跨度逐位不变；构造用例见 CHANGELOG |
| P1-2 `SolveCore.Median` 复用 `StatsCore.Median` | `[x]` | A5 |
| P1-3 `AnovaOneWay` 公共尺度预归一 | `[x]` | A4 + `AnovaOneWay_extreme_scale_keepsF_andCapsSs`（F=8 与 scipy 一致） |
| P1-4 `StatsCore.Sum` Neumaier | `[x]` | E2（0.0 → 1.0）；溢出回退对照仍绿 |
| P1-5 SOLVE 真字典序选点 | `[x]` | `LexicographicallyBetter` + 既有 SOLVE 用例全绿 |
| P2-1 文档口径（空白单元格 / 元素级返回类型） | `[x]` | api-reference 校正 |
| P2-2 元素级"形状保持"约定显式化 | `[x]` | api-reference STATS 节 |
| P2-3 陈旧数字 | `[x]` **撤销** | 复核为准确历史记录（见上表 E11） |
| P2-4 空/省略判定收敛 | `[x]` | 新增 `InputNormalizer.IsBlankOrErrorCell`；`SolveUdf`/`AnalyticsHelpers`/`SqlCore` 共 5 处收敛 |
| P2-5 `SolveCore` 77 条硬编码异常入 `ErrorMsg` | `[ ]` **顺延** | 纯机械改动且触及大量断言，风险高于收益；并入 P3 结构性重构批次 |
| P3（UDF 元数据化/源生成、SolveCore 拆分、宿主适配程序集、CrossVal 打 UDF 入口、分支覆盖率、Excel E2E 进 CI、`Category=`） | `[ ]` 独立计划 | 需独立 ADR 与分批验证 |

**门禁结果（2026-10-07 收尾实测）**：

| 门禁 | 结果 |
| :--- | :--- |
| `pre-commit-check.ps1` | **6/6 PASS** |
| `check-test-quality.ps1` | **PASS**（2903 方法；0 零断言 / 0 恒真 / 0 存在性） |
| `dotnet test`（双 TFM，6 次运行） | **全绿**：Foundation 438×2 / Analytics 972×2 / DataToolkit 1546×2 |
| `python scripts/verify-manual.py` | **523 passed / 0 failed / 0 skipped，EXIT=0**（manual 235 / cross 288，README 对账一致） |
| Release 构建 + `verify-pack.ps1` | **4/4 PASSED**（Analytics/DataToolkit × net48/net8.0） |
| 真实 Excel 封送复测 | `=STR.REVERSE()` → **`#VALUE!`**（原 `#NUM!`），net48 与 net8 一致 |
| `verify-docs.ps1` | 25 PASS / 2 FAIL / 1 SKIP —— 2 条 FAIL 均为**本地检出落后 `origin/main` 一个提交**所致（见 E1），非代码缺陷 |

**过程中被验证机制抓出的回归（值得记录）**：P0-4 首版一律 `SpecifyKind(Utc)`，把 Kind=Local 的
墙上时刻错误重贴标签；`verify-manual.py` 的 `DT.UNIXTS` 对照立即报 `got 1704067200, expected 1704096000`
（+8h）。根因是 manifest 入参为 `"2024-01-01T00:00:00Z"`，经 Dispatcher 转成 Local。已改为 Kind 分派
并补 `D1d_UnixTimestamp_respectsLocalKind` 回归守卫。同时发现 Python 侧 FROMUNIX oracle 原本用
`datetime.fromtimestamp`（本地时区）——**与 C# 共享同一错误假设**，这正是该缺陷此前对交叉验证不可见的
原因；已一并改为墙上时刻口径。


