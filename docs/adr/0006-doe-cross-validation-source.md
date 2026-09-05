# ADR-0006: DOE 类 UDF 的闭环验证信源

**日期**: 2026-08-26
**状态**: 已确认

## 上下文

新增 `DOE.PLAN`（实验设计矩阵生成，返回二维数组）。它与现有数值类 UDF（STATS/REGRESS/LINALG/PHYCHEM）有本质区别：

- 现有数值类 UDF 返回**连续数值**（均值、p 值、特征值等），用 scipy/numpy/sklearn 独立实现比对即可（AGENTS.md 红线 6：数值类 UDF 必须 `cross_check()`）。
- `DOE.PLAN` 返回**实验计划表**（StandardOrder + RunOrder + 编码因子列），是**组合数学生成**，不是数值推断。scipy/numpy 没有对应函数可对标；唯一语义上等价的独立实现是 `pyDOE2`（Python DOE 库，`fullfact` 生成全因子水平索引）以及 Minitab/JMP 的标准正交表。

若不澄清，`DOE.PLAN` 会落入红线 6「禁止自校验」的模糊地带：既无法用 scipy 对标，又不能用 `check(name, X, X)` 自校验。

## 决策

`DOE.*` 类 UDF（生成设计矩阵）的闭环验证采用**双信源**，替代 scipy 范式：

1. **组合正确性 → pyDOE2 独立实现**：Core 层暴露纯编码矩阵方法（`FullFactorialCoded` / `FractionalCoded` / `RsmCcd` / `RsmBb`），通过 CrossValRunner 与 pyDOE2 逐行比对——`fullfact`（full）、`fracfact`（fractional）、`ccdesign(alpha='rotatable')`（CCD）、`bbdesign`（Box-Behnken）。这构成「Python 独立实现」闭环，消除自校验。
2. **正交性 → 独立正交性检查器（golden reference）**：Taguchi 正交表（L4/L8/L9/L12/L16/L18/L27）的验证不依赖 Minitab 表硬编码（其精确列序无法独立核实），而是用单元测试中的正交性检查器验证数学本质：每列平衡（各水平出现 runs/levels 次）+ 任意两列正交（各水平组合出现 runs/(levelsᵢ·levelsⱼ) 次）+ 运行数正确。正交性是 Taguchi 设计的客观数学性质，构成与 Minitab/JMP 一致的可靠信源。
3. **对齐 Minitab/JMP → 标准顺序 golden reference**：全因子设计的 StandardOrder 用硬编码期望值（如 2² 的 (-1,-1),(+1,-1),(-1,+1),(+1,+1)）对齐 Minitab/JMP；Taguchi 的列序采用标准构造（A,B,AB,C,… 与 GF(3) 主效应+交互），在列置换下与 Minitab 等价（正交性/运行数/水平分布完全一致）。

## 原因

1. DOE 是组合生成，非数值推断，scipy/numpy 无对标函数——强行套用 scipy 范式不可行。
2. pyDOE2 是唯一广泛使用的 Python DOE 库，其 `fullfact` 的输出顺序（第一个因子变化最快）与 Minitab/JMP 标准顺序一致，可直接逐行比对，构成真正的独立实现闭环。
3. 「与 Minitab/JMP 一致」的目标只能通过 golden reference（硬编码标准表）验证，因为 Minitab/JMP 是闭源商业软件，无独立开源实现可对标其默认生成元/随机化顺序。

## 约束

- `DOE.*` 的随机化（RunOrder）**不追求**与 Minitab/JMP 逐单元格一致（它们 RNG 算法未知），只保证**同 seed 自复现**（自实现 `XorShift64`，保证 net48 与 net8.0 同 seed 序列一致）。
- 交叉验证的**对齐基准**是 `randomize=FALSE` 时的 StandardOrder，随机化只做排列正确性断言（不测「不同 seed 结果不同」这类概率性断言）。
- Core 层必须暴露可供 CrossVal 调用的纯编码矩阵方法（`FullFactorialCoded`），不能只暴露含表头的 `PlanFull` 返回 `object[,]`（CrossVal 序列化器不处理 `object[,]`）。

## 影响

- **正面**：DOE 类函数纳入闭环验证，符合红线 6 精神（独立实现比对），避免自校验假阴性。
- **代价**：DOE 验证链路比数值类多一层（pyDOE2 + golden reference 双轨），单元测试需维护 Minitab 标准表 golden 值。
- **需同步**：`docs/specification/api-reference.md`（DOE.* 签名）、`docs/user-manual/user-manual.md`（DOE 示例）、`docs/specification/specification.md`（模块清单 + UDF 总数）、`skills/excel-dna-project.md`（如需要，DOE 编码约定）。

## 演进

- **2026-08-26**: 初始确认，覆盖 P1（`full` 全因子）。后续 P2-P4（taguchi/fractional/rsm）接入时沿用本 ADR 的双信源策略，taguchi/fractional 以 Minitab 标准正交表 golden reference 为主、rsm 以 pyDOE2 `ccdesign`/`bbdesign` 为主。
- **2026-08-26**: P2（`taguchi`）接入，验证策略修正为「正交性数学性质验证」：Taguchi 正交表不依赖 Minitab 表硬编码（其精确列序无法独立核实），改用单元测试中的独立正交性检查器验证（每列平衡 + 两两正交 + 运行数）。pyDOE2 `pbdesign` 因列序等价形式多、无法逐列对齐，不用于 L12 的逐行交叉验证。
- **2026-08-26**: P3（`fractional`）、P4（`rsm` CCD）、`bb`（Box-Behnken）接入，均用 pyDOE2 逐行交叉验证：`fracfact`、`ccdesign(alpha='rotatable')`、`bbdesign`。这些设计的列序/结构由 pyDOE2 独立实现直接对齐，无需 golden reference。
- **2026-08-26**: DOE 分析（`ANALYZE`/`ANOVA`/`PARETO`）接入。它们是数值类 UDF（复用 `RegressionCore.FitOLS`），验证回归 scipy 范式：单元测试用 scipy 独立计算的精确期望值（系数/t/p/SS）硬编码。多因素 ANOVA 复用 `FitOLS` 的 t/p（恒等式 F=t²、SS=MSE×t²，经 scipy 验证 F(1,df) 上尾 p == t 双侧 p）。
