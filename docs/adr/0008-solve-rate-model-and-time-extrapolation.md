# ADR-0008: SOLVE 速率模型（rate）与时间外推

**日期**: 2026-09-12
**状态**: 已确认
**关系**: 扩展 [ADR-0007](0007-solve-module-and-bounded-search.md) 决策 2（v1 排除 `rate`，本 ADR 兑现其 v2 评估）

## 上下文

用户真实批次数据（`logs/probes/Data.xlsx` / `logs/probes/examples.xlsx`，随 logs/ 永久不入库）只有一个时间点（t=60s），但工艺语义是**速率过程**：输出 = 来料 − 时间 × 去除速率，去除速率受 IncomingBow/可调参数影响；且需要在 t≠60（如 30s/90s/120s）下预测与反解。ADR-0007 的 v1 模型（linear/poly）把 t 当普通特征，只能拟合 60s 截面，无法外推，也无法输出"去除速率"这一物理量。

无代码实验（Python/sklearn，读真实数据）结论：
- 把 `(IncomingZx−OutputZx)/t` 作为目标、共享特征建模，在 CV 上是可拟合的结构；Bow 与速率相关（corr 0.37~0.65，Z2/Z3/Z4 明显）。
- t 恒为 60 时，速率结构退化为普通线性（自由系数已覆盖）；**本扩展的核心价值是 t 可变时的外推/反解与速率输出**，而非提升 60s 截面拟合。

用户决策：Time 可调整；配对缺失直接报错；INVERSE 需额外输出去除速率；`auto` 需纳入 rate 候选。

## 决策

1. **`model="rate"`（不新增 UDF）**：在现有 4 个 SOLVE 函数上扩展 `model` 取值，v2 速率模型 = 线性速率律：
   `OutputZx(t) = IncomingZx − t · g(Bow, 可调, 固定, 其他来料)`，`g` 为线性模型（不含配对 IncomingZx 与时间列，两者系数固定为 1 与 −t）。
2. **配对规则**：`Output*` 去掉角色前缀后的后缀必须与某个 `Incoming*` 去掉前缀后的后缀（OrdinalIgnoreCase）一致（`OutputZ1` ↔ `IncomingZ1`，`输出收率` ↔ `来料收率`）。显式 `rate` 缺配对/缺时间列 → `#VALUE!`；`auto` 下结构不可用则与 poly 一样显示 `跳过`。
3. **时间列识别**：唯一一个表头以 `Time`（IgnoreCase）或 `时间` 结尾的特征列；多个/缺失时显式 rate 报错。**时间是否可调由列角色决定**：`VariableTime`/`可调时间` 缺失时参与寻优（请求空白 = 求解，边界默认历史 min/max，可用 bounds 表放宽）；`FixedTime` 为条件（留空取历史中位数，行内取给定值）。不引入新的"求解时间"专用参数。
4. **速率输出**：`SOLVE.INVERSE` 在 rate 生效时增加每输出一列 `<输出名>速率`（单位：输出单位/时间单位）；`SOLVE.EQUATION` 增加 `速率方程` 行（`OutputZx速率 = g(...)`）。
5. **auto 候选**：`linear` / `poly` / `rate` 三候选按 Output 尺度的 CV R² 选优（并列 <1e-9 时优先级 linear > poly > rate）；rate 结构不可用或样本不足时跳过（`PolySkipped` 同款 `RateSkipped`）。
6. **数值口径**：CV/σ/目标偏差一律在 Output 原尺度；t 必须有限且 >0，否则报错（显式 rate）或跳过（auto）。可复现性沿用 `XorShift64` + seed。

## 原因

1. 时间外推是 60s 单点数据无法用现有模型表达的物理需求（ADR-0007 决策 2 将 rate 留给 v2 评估，条件已满足）。
2. 配对/时间用命名约定而非新参数，保持"一个公式出结果"，与既有表头前缀角色体系一致。
3. 时间可调通过列角色复用现有寻优机制（`Variable*` 已支持求解与 bounds），零额外概念。
4. 速率在 Output 原尺度评估（σ/可达性/CV）避免用户换算，速率列满足工艺解读。

## 约束

- rate 仅线性（多项式速率留待后续评估）；g 不含配对 Incoming 与时间列（强制物理结构）。
- `SolveCore` 保持零 Excel 依赖；不改 UDF 签名（`model` 参数取值扩展，api-reference 同步）。
- 缺失配对/时间/非正 t：显式 rate 抛异常（UDF → `#VALUE!`）；auto 跳过并保持其余候选可用。
- CrossVal 必须真对照 C#（FitRate / PredictRate / SolveInverseRate 条目 + Python 独立实现）；单元测试期望硬编码。
- 速率列仅在 rate 生效时出现（显式 rate，或 auto 下至少一个输出选中 rate），否则布局与 v1 完全一致（向后兼容）。

## 影响

- **正面**：支持 t≠60 预测/反解（时间外推）；输出物理量"去除速率"；auto 自动判断 60s 截面上 rate 是否优于 linear/poly。v1 调用完全不受影响（不选 rate 时无布局变化）。
- **代价**：Core 增加约 250 行（速率拟合/预测/CV/规划/装配）；文档新增 rate 语义与示例；CrossVal 新增 4 条。
- **需同步**：`api-reference.md`（model 取值/布局/配对与时间约定/错误行为）、`user-manual.md`（rate 示例）、`context.md`（速率模型/配对/时间列术语）、`project-structure.md`（本 ADR 登记）、`README.md` SOLVE 行。

## 演进

- **2026-09-12**: 初始确认。多项式速率/多时间列/共享速率系数（cross-output）未纳入，待后续评估。
