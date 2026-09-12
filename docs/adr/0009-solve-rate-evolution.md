# ADR-0009: SOLVE rate 演进 — 多项式速率、多时间列、跨输出共享速率

**日期**: 2026-09-12
**状态**: 已确认
**关系**: 兑现 [ADR-0008](0008-solve-rate-model-and-time-extrapolation.md) 演进节"多项式速率/多时间列/共享速率系数（cross-output）未纳入，待后续评估"

## 上下文

ADR-0008 落地了线性速率律 `OutputZx(t) = IncomingZx − t·g(可调/来料/固定)`，但三项能力被明确留给后续评估：

1. **多项式速率**：g 仅线性，强曲率工艺（速率随参数非线性变化）拟合不足；
2. **多时间列**：全局只允许一个以 `Time`/`时间` 结尾的特征列，多工段/多时间维度的表无法表达；
3. **跨输出共享速率系数**：每个输出独立拟合 g，而同一机理下多个响应（如多种杂质去除率）物理上共享同一速率函数，独立拟合会放大噪声、失去 pooling 收益。

三项都只影响 `model` 取值语义与表头约定，不改任何 UDF 签名（红线），也不需要新程序集/新 .xll。

## 决策

1. **多项式速率（`model="rate_poly"`）**：
   - g 采用与 `poly` 相同的二次展开（线性 + 平方 + 两两交互，上限 100 项），ridge 正则 λ=1e-5（标准化项上），仍**排除配对来料列与全部时间列**；
   - `Kind` 记录为 `rate_poly`，`SolveModel.IsRate`（Kind ∈ {rate, rate_poly}）统一判定速率律；`SOLVE.INVERSE` 布局、速率列、可达性、错误行为与 `rate` 完全一致；
   - **普通输出的 `auto` 候选保持 linear/poly/rate 不变**（不改动既有 auto 行为与 QUALITY 行数）；`rate_poly` 需显式指定。
   - 样本下限：展开项数 + 1（`rate` 的 k+1 是其特例）。
2. **多时间列（后缀配对）**：
   - 时间列识别：去掉角色前缀后名称**含** `Time`/`时间` 的特征列（如 `FixedTime`、`VariableTime`、`FixedTimeZ1`、`可调时间A`）；
   - 对每个输出 `Output<suffix>`，优先绑定 stripped 名称以 `Time<suffix>`/`时间<suffix>` 结尾的时间列（`FixedTimeZ1` ↔ `OutputZ1`）；无后缀匹配时回落到**唯一**全局时间列（向后兼容）；多个候选匹配（歧义）/ 多个时间列但无匹配（显式 rate）→ `#VALUE!`；auto 下逐输出跳过；
   - **所有**时间列一律不进入任何 g（物理结构：时间是乘子，不是速率因子）。
3. **跨输出共享速率（`SharedOutput*` / `共享输出*`）**：
   - 所有共享输出列构成**一个共享速率组**，组内成员共用同一 g；普通 `Output*` 列仍各自独立；
   - 配对规则与普通输出相同（后缀匹配来料与时间列）；组内成员须全部配对，否则显式 `rate`/`rate_poly` 报错、auto 逐输出回退；
   - 拟合：把每个成员的速率目标 `(Incoming_j − Output_j)/t_j` 堆叠为池化训练集，g 的排除集 = 组内全部配对来料列 ∪ 全部时间列；
   - auto 候选：共享组只比较 `rate` 与 `rate_poly`（池化 CV，Output 原尺度 R²，并列 <1e-9 取 rate）；不参与普通输出的 linear/poly 候选；
   - 输出布局不变：`SOLVE.INVERSE` 速率列每输出一列（同组数值相同）；`SOLVE.EQUATION` 每个成员各出一行速率方程（表达式相同）；`SOLVE.QUALITY` 共享输出给 `rate`/`rate_poly` 两行候选；
   - 显式 `linear`/`poly` 时共享角色退化为独立输出（该语义只约束速率律）。
4. **确定性/守卫**：沿用 `XorShift64` + seed；池化 CV 与单输出 CV 同折规则（n≥20 五折，否则 LOO，下限 5）；规模上限、时间正性校验（自动回退语义）与 ADR-0008 一致。

## 原因

1. `rate_poly` 复用既有二次展开与 ridge 内核，成本与 `poly` 同级；显式取值保证向后兼容（auto/rate 行为不变）。
2. 后缀配对是 ADR-0008 来料配对约定的直接延伸，零新概念；回落规则保证单时间列旧表零改动。
3. 共享组用池化拟合与池化 CV，同时满足"共享同一物理机理"与"不同输出各自的可达性/偏差评估"（偏差 σ 仍按各自历史输出标准差，Output 原尺度）。

## 约束

- 不新增/不修改 UDF 签名；`model` 取值扩展 `rate_poly`，表头前缀扩展 `SharedOutput*`/`共享输出*`。
- g 的排除集为"配对来料 ∪ 全部时间列"；共享组的排除集为"组内全部配对来料 ∪ 全部时间列"。
- 共享组暂限一个（全部 SharedOutput 列）；两组及以上待后续评估。
- 新增数值路径必须有硬编码期望单测 + CrossVal 真对照（rate_poly 预测、共享 g 系数/截距）。
- `SolveCore` 保持零 Excel 依赖；`object[,]` 入口守卫与规模上限覆盖四个入口。

## 影响

- **正面**：速率律可表达曲率（rate_poly）；多工段表（每输出独立时间列）可解；同机理多响应共享 g，提升小样本稳定性并输出一致的速率解读；UDF 签名与旧表布局不变。
- **负面/代价**：`rate_poly` 高次外推风险高于线性速率（边界默认历史 min/max，触界需实验确认）；共享组只有一条 g，若成员机理不同会被池化平均（文档提示分组前先看 `SOLVE.QUALITY`）。
- **需同步**：`api-reference.md`（model 取值/时间列配对/SharedOutput 角色/QUALITY 候选）、`user-manual.md`（示例与解读）、`context.md`（术语：共享输出/共享速率、多项式速率）、`project-structure.md`（本 ADR 登记）、`AGENTS.md`/`docs/README.md`（ADR 范围）、`tests/CrossValRunner`（PredictRatePoly/FitSharedRate 条目）、`scripts/verify-manual.py`（独立 Python 对照）。

## 演进

- **2026-09-12**: 初始确认。多共享组、多项式速率的 auto 纳入、多时间列的交乘项（`Σ tₖ·gₖ`）未纳入，待后续评估。
