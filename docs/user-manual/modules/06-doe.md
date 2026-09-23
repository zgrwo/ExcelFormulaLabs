# DOE — 实验设计

> 生成 DOE（实验设计）矩阵。支持全因子（method=`"full"`）、田口正交表（method=`"taguchi"`）、2水平部分因子（method=`"fractional"`）、响应面（method=`"rsm"` CCD / `"bb"` Box-Behnken）。因子水平编码为 -1/0/+1（coded 单位）。

<a id="doe-plan"></a>

### DOE.PLAN — 全因子 / 田口 / 部分因子 / 响应面实验设计

**语法**：`=DOE.PLAN(factor_qty1, factor_level1, factor_qty2, factor_level2, method, [randomize], [seed])`

- `method`：设计方法，支持 `"full"`（全因子）、`"taguchi"`（田口正交表）、`"fractional"`（2水平 ½ 部分因子）、`"rsm"`（响应面 CCD）、`"bb"`（Box-Behnken）。
- `[randomize]`：是否随机化运行顺序，默认 TRUE。
- `[seed]`：固定随机种子（null=随机）；同 seed 结果可复现。

返回带表头的二维表：`StdOrder`、`RunOrder`、`A`、`B`…。**表行序恒为标准序**（`StdOrder` 1..N），`randomize` 只改变 `RunOrder` 列（打乱后的执行顺序）；`randomize=FALSE` 时 RunOrder = StandardOrder。

**全因子示例**（2 因子 × 2 水平，不随机化）：
```
=DOE.PLAN(2, 2, 0, 2, "full", FALSE)
```
输出：

| StdOrder | RunOrder | A | B |
|---|---|---|---|
| 1 | 1 | -1 | -1 |
| 2 | 2 | 1 | -1 |
| 3 | 3 | -1 | 1 |
| 4 | 4 | 1 | 1 |

**混合全因子示例**（1 个 2 水平因子 × 1 个 3 水平因子）：
```
=DOE.PLAN(1, 2, 1, 3, "full", FALSE)
```
输出 6 行：A ∈ {-1, 1}，B ∈ {-1, 0, 1}（3 水平编码 -1/0/+1）。

**田口正交表示例**（3 个 2 水平因子 → L4）：
```
=DOE.PLAN(3, 2, 0, 2, "taguchi", FALSE)
```
输出 L4(2³)：4 行 × 3 因子列，每列 {-1, +1} 平衡，任意两列正交。

**田口混合示例**（1 个 2 水平 + 7 个 3 水平 → L18）：
```
=DOE.PLAN(1, 2, 7, 3, "taguchi", FALSE)
```
输出 L18(2¹×3⁷)：18 行，列 A（2 水平 -1/+1）与 B~H（3 水平 -1/0/+1）正交。

**部分因子示例**（4 个 2 水平因子 → 2⁴⁻¹，8 运行）：
```
=DOE.PLAN(4, 2, 0, 2, "fractional", FALSE)
```
输出 2⁴⁻¹ 设计（8 行）：A、B、C 为独立因子（全因子顺序），D = A×B×C（生成元），对齐 Minitab 默认最高分辨率生成元。

**响应面示例**（2 个连续因子 → CCD，16 运行）：
```
=DOE.PLAN(2, 2, 0, 2, "rsm", FALSE)
```
输出 CCD（中心复合，可旋转 α=√2）：4 全因子点（±1）+ 4 中心点（0）+ 4 轴向点（±α）+ 4 中心点（0），共 16 行。

**Box-Behnken 示例**（3 个连续因子 → Box-Behnken，15 运行）：
```
=DOE.PLAN(3, 2, 0, 2, "bb", FALSE)
```
输出 Box-Behnken：12 边点（每对因子 ±1，其余 0）+ 3 中心点（0），共 15 行。

### 结果分析

将 `DOE.PLAN` 生成的因子列与实验响应列一并分析：

**效应表**：
```
=DOE.ANALYZE(factor_matrix, response, "main")
```
输出每项的 `Term`、`Coef`（系数）、`Effect`（2×Coef）、`t`、`p`。`p<0.05` 表示该效应显著。

**多因素 ANOVA**：
```
=DOE.ANOVA(factor_matrix, response, "2way")
```
输出每项的 `Source`、`SS`、`df`、`MS`、`F`、`p`，含 Error 和 Total 行。

**Pareto 排序**：
```
=DOE.PARETO(factor_matrix, response, "2way")
```
输出按 |效应| 降序的 `Term`、`Effect`，供 Pareto 图。

> `terms` 可选 `"main"`（主效应）、`"2way"`（默认，主效应+2阶交互）、`"quadratic"`（含平方项，需 3 水平设计）。

---
