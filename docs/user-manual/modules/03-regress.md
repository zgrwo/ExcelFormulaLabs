# REGRESS — 回归分析

> 返回纵向报告表（col0 = 字段名，col1.. = 值或数组展开）。p < 0.05 = 显著；R² 越接近 1 拟合越好。
>
> **函数索引**：[OLS](#regress-ols) · [WLS](#regress-wls) · [RIDGE](#regress-ridge) · [ANOVA1](#regress-anova1) · [FACTORIMP](#regress-factorimp) · [COEF](#regress-coef) · [RSQ](#regress-rsq)

### 示例数据集

（y = 1 + 2·x₁ + 1·x₂，完美线性关系，R²=1.0）

|   | A (X1) | B (X2) | C (Y) |
|---|---|---|---|
| **1** | x1 | x2 | y |
| **2** | 1 | 3 | 6 |
| **3** | 2 | 1 | 6 |
| **4** | 3 | 4 | 11 |
| **5** | 4 | 2 | 11 |
| **6** | 5 | 5 | 16 |

---

<a id="regress-ols"></a>

### REGRESS.OLS — 普通最小二乘法

**语法**：`=REGRESS.OLS(known_y, known_x)`

对标 Excel LINEST（但返回更丰富的报告表）。

**返回**：`object[11,?]` — 11 行 × (1 + 宽度) 报告表：

| 行 | 字段 | 内容 |
|----|------|------|
| 0 | `coefficients` | β₀, β₁, ...（截距 + 各系数） |
| 1 | `sse` | 残差平方和 |
| 2 | `r_squared` | R² |
| 3 | `adj_r_squared` | 调整 R² |
| 4 | `residuals` | 残差数组 |
| 5 | `fitted_values` | 拟合值数组 |
| 6 | `standard_errors` | 标准误数组 |
| 7 | `t_stats` | t 值数组 |
| 8 | `p_values` | p 值数组 |
| 9 | `n` | 样本量 |
| 10 | `df` | 自由度 |

**示例**：
```
=REGRESS.OLS(C2:C6, A2:B6)
```
返回 11 行报告表。关键值：coefficients = {1, 2, 1}（截距=1, β₁=2, β₂=1），R² = 1.0, sse = 0（完美线性关系）

---

<a id="regress-wls"></a>

### REGRESS.WLS — 加权最小二乘法

**语法**：`=REGRESS.WLS(known_y, known_x, weights)`

用于异方差数据。返回同 OLS 的 11 行报告。`sse`/`r_squared`/标准误/t/p 为**加权（√w 变换）尺度**（与 statsmodels WLS 一致）；`residuals`/`fitted_values` 保持原始尺度便于与 y 比较。

**示例**（权重 w = {1, 2, 3, 4, 5}）：
```
=REGRESS.WLS(C2:C6, A2:B6, D2:D6)
```

---

<a id="regress-ridge"></a>

### REGRESS.RIDGE — 岭回归

**语法**：`=REGRESS.RIDGE(known_y, known_x, [lambda])`

L2 正则化（防过拟合）。λ 默认 1.0。不返回标准误/t值/p值（正则化下推断无效）。

**返回**：`object[8,?]` — 8 行：coefficients, sse, r_squared, residuals, fitted_values, lambda, n, df。

**示例**：
```
=REGRESS.RIDGE(C2:C6, A2:B6, 0.1)
=REGRESS.RIDGE(C2:C6, A2:B6)       // λ 默认 1.0
```

---

<a id="regress-anova1"></a>

### REGRESS.ANOVA1 — 单因素方差分析

**语法**：`=REGRESS.ANOVA1(input_range)`

数据按列分组（每列一组）。组内空单元格/错误值会被静默跳过，各组样本量可以不同（不平衡组）。首行若为文本列名（如示例的 `Group A/B/C`）会自动作为表头跳过；数据中间的非数值文本仍会报 `#VALUE!`。p < 0.05 = 至少有一组均值显著不同。

**返回**：`object[12,?]` — 12 行：ss_between, ss_within, ss_total, df_between, df_within, df_total, ms_between, ms_within, f_stat, p_value, group_means, group_counts。

**示例**（3组，各5个值）：

| Group A | Group B | Group C |
|---------|---------|---------|
| 10 | 20 | 15 |
| 12 | 22 | 17 |
| 14 | 24 | 16 |
| 11 | 21 | 18 |
| 13 | 23 | 14 |

```
=REGRESS.ANOVA1(A1:C6)   ← 首行列名自动跳过
→ f_stat ≈ 50.67, p_value ≈ 1.4e-6  (组间差异极显著)
```

---

<a id="regress-factorimp"></a>

### REGRESS.FACTORIMP — 因子重要性排名

**语法**：`=REGRESS.FACTORIMP(known_y, known_x)`

按标准化后的 |t| 降序排列，返回 0-based 列索引。

**示例**：
```
=REGRESS.FACTORIMP(C2:C6, A2:B6)
→ {0, 1}   (X1 比 X2 更重要, |t₁| > |t₂|)
```

---

<a id="regress-coef"></a>

### REGRESS.COEF — OLS 回归系数

**语法**：`=REGRESS.COEF(known_y, known_x)`

**返回**：`double[]` — β 系数向量（含截距）。

**示例**：
```
=REGRESS.COEF(C2:C6, A2:B6)
→ {1, 2, 1}  （截距=1, β₁=2, β₂=1）
```

---

<a id="regress-rsq"></a>

### REGRESS.RSQ — 决定系数 R²

**语法**：`=REGRESS.RSQ(known_y, known_x)`

范围 0–1。1 = 完美拟合。

**示例**：
```
=REGRESS.RSQ(C2:C6, A2:B6)
→ 1.0  （完美拟合）
```

---

### REGRESS — 结果解读指南

> 回归分析返回的是**报告表**（`object[rows, cols]`），不是单个值。本节教你如何读取和提取关键信息。

#### 报告表结构速查

所有报告表的第一列（col 0）是**字段名**，后续列为**数据值**（数组字段横向展开）。

**用 INDEX 提取单个值**：`=INDEX(report, row_number, column_number)`（Excel 中横行=1-based）

| 字段 | OLS/WLS | RIDGE | 含义 |
|------|---------|-------|------|
| `coefficients` | ✅ | ✅ | β₀=截距, β₁..=各系数。正=正相关, 负=负相关, 绝对值大=影响大 |
| `sse` | ✅ | ✅ | 残差平方和，越小拟合越好（不可跨模型比较） |
| `r_squared` | ✅ | ✅ | 决定系数 R²。0~1，>0.7 良好，>0.9 优秀。⚠️ 增加变量 R² 必然不降 |
| `adj_r_squared` | ✅ | ❌ | 调整 R²。惩罚多余变量。**模型比较时用这个，不用 R²** |
| `residuals` | ✅ | ✅ | 残差 = 实际 - 预测。应随机分布无模式；有模式 = 模型不适合 |
| `fitted_values` | ✅ | ✅ | 模型对每个样本的预测值 |
| `standard_errors` | ✅ | ❌ | 系数标准误。越小估计越精确 |
| `t_stats` | ✅ | ❌ | t = 系数/标准误。绝对值 > 2 通常显著 |
| `p_values` | ✅ | ❌ | **最重要**。p < 0.05 = 该系数统计显著（对 y 有真实影响） |
| `n` | ✅ | ✅ | 样本量 |
| `df` | ✅ | ✅ | 自由度（n - 变量数） |

#### 显著性判断三步法

1. **整体模型**：先看 R² 和调整 R²。R² > 0.5 模型有一定的解释力。
2. **各系数**：看 `p_values` 行。p < 0.05 的系数显著；p > 0.05 的变量可考虑移除。
3. **残差检查**：`residuals` 不应有明显趋势。若残差随拟合值增大而增大 → 异方差 → 改用 WLS。

#### 模型选择决策树

| 场景 | 推荐 | 原因 |
|------|------|------|
| 标准线性回归 | **OLS** | 最小二乘法，满足基本假设时最优 |
| 残差方差不均匀（异方差） | **WLS** | 权重调整，使大方差样本降权 |
| 变量多、样本少、共线性严重 | **RIDGE** | L2 正则化收缩系数，防止过拟合 |
| 仅需系数和 R² | **COEF + RSQ** | 比 OLS 报告表更轻量 |
| 比较多组均值差异 | **ANOVA1** | 单因素方差分析 |

#### ANOVA1 解读

```
=REGRESS.ANOVA1(data_range)
```

| 字段 | 含义 | 判断 |
|------|------|------|
| `f_stat` | F 值 = 组间方差/组内方差 | 越大组间差异越大 |
| `p_value` | 显著性 | p < 0.05 = 至少一组显著不同 |
| `ss_between` / `ss_within` | 组间/组内平方和 | 组间 >> 组内 → 分组有意义 |
| `group_means` | 各组均值 | 对比找出差异最大的组 |
| `group_counts` | 各组样本量 | 确认样本量是否平衡 |

> ⚠️ ANOVA 只能判断"是否有差异"，不能告诉你"哪两组不同"。如需事后两两比较，可用 TTEST2 手动检验。

#### Ridge 注意事项

- **不返回 SE/t/p**：正则化使标准推断理论不成立。看 `r_squared` 和 `coefficients` 即可。
- **λ 选择**：λ 越大系数越收缩。建议尝试 λ = 0.01, 0.1, 1, 10 对比 R² 变化。
- **何时用 Ridge**：共线性使 OLS 报错 `#VALUE!`（"near-singular"）时，改用 Ridge 可正常工作。

#### 常见 #VALUE! 错误速查

| 错误信息关键词 | 原因 | 解决方法 |
|------|------|---------|
| `rank-deficient` | 自变量线性相关（如 X2 = 2*X1） | 移除冗余变量，或用 Ridge |
| `near-singular` | 高度共线但未完全相关 | 移除冗余变量，或用 Ridge |
| `constant response` | y 所有值相同（方差为 0） | 检查数据范围 |
| `degrees of freedom` | 样本数 ≤ 变量数 | 增加样本或减少变量 |
| `negative weight` | WLS 权重含负数 | 权重必须 ≥ 0 |

---
