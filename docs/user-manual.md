# ExcelVbaLibraries 用户手册

> **版本**：2.0 | **更新日期**：2026-06-26 | **覆盖函数**：219 个 UDF（14 模块）
> 完整签名见 [API 参考](api-reference.md)；安装说明见 [README](../README.md)

---

## 目录

1. [STATS — 描述统计](#1-stats--描述统计)（33 函数）
2. [LINALG — 线性代数](#2-linalg--线性代数)（19 函数）
3. [REGRESS — 回归分析](#3-regress--回归分析)（7 函数）
4. [PHYCHEM — 物理化学](#4-phychem--物理化学)（16 函数）
5. [STR — 字符串处理](#5-str--字符串处理)（34 函数）
6. [DT — 日期时间](#6-dt--日期时间)（25 函数）
7. [REGEX — 正则表达式](#7-regex--正则表达式)（9 函数）
8. [ARR — 数组操作](#8-arr--数组操作)（22 函数）
9. [DICT — 字典集合](#9-dict--字典集合)（8 函数）
10. [JSON / XML](#10-json--xml--数据处理)（8 函数）
11. [PIVOT — 数据透视](#11-pivot--数据透视)（4 函数）
12. [SQL — SQL 查询](#12-sql--sql-查询)（3 函数）
13. [FS — 文件系统](#13-fs--文件系统)（22 函数）
14. [RANGE — 范围导出](#14-range--范围导出)（9 函数）
15. [错误参考](#15-错误参考)

---

## VBA 调用

加载 .xll 后，所有函数可通过 `Application.Run` 直接调用，无需引用或声明。详见 [API 参考 → VBA 调用](api-reference.md#vba-调用)。

---

## 通用约定

- **数组公式**：多数函数支持数组输入。Excel 365 中数组自动溢出（spill），旧版需 `Ctrl+Shift+Enter`
- **错误值**：`#VALUE!` = 输入/执行错误；`#NUM!` = 计算结果无定义（详见[错误参考](#15-错误参考)）
- **空值处理**：空单元格在数值函数中被跳过，在字符串函数中视为空串
- **表头行**：带 `hasHeaders` 参数的函数默认将第一行视为表头

---

## 1. STATS — 描述统计

> 对标 Python scipy，精度 1e-10。元素级函数（ABS/SQRT/LN/LOG10/EXP/SIGN）支持数组公式。
>
> **函数索引**：MEAN · GEOMEAN · HARMEAN · MEDIAN · VARP · VAR · STDEVP · STDEV · SKEW · KURT · MIN · MAX · RANGE · SUM · PRODUCT · PERCENTILE · IQR · SUMMARY · COUNT · MODE · COVARP · COVAR · PEARSON · SPEARMAN · TTEST1 · TTEST2 · ZSCORE · ABS · SQRT · LN · LOG10 · EXP · SIGN

### 示例数据集

以下函数使用此数值区域（B2:E6，5 行 × 4 列）：

|   | A | B | C | D | E |
|:--|:--|:--|:--|:--|:--|
| **1** | Product | Q1 | Q2 | Q3 | Q4 |
| **2** | Alpha | 10 | 20 | 30 | 40 |
| **3** | Beta | 15 | 25 | 35 | 45 |
| **4** | Gamma | 12 | 22 | 32 | 42 |
| **5** | Delta | 18 | 28 | 38 | 48 |
| **6** | Epsilon | 14 | 24 | 34 | 44 |

展平为一维数组：`[10,20,30,40,15,25,35,45,12,22,32,42,18,28,38,48,14,24,34,44]`（n=20）

---

### STATS.MEAN — 算术平均值

**语法**：`=STATS.MEAN(number1)`

| 参数 | 说明 |
|------|------|
| `number1` | 数值区域或数组 |

**示例**：
```
=STATS.MEAN(B2:E6)
→ 28.8
```

---

### STATS.GEOMEAN — 几何平均值

**语法**：`=STATS.GEOMEAN(number1)`

仅正数数组有效，含负数返回 `#NUM!`。

**示例**：
```
=STATS.GEOMEAN(B2:E6)
→ 26.0719...
```

---

### STATS.HARMEAN — 调和平均值

**语法**：`=STATS.HARMEAN(number1)`

**示例**：
```
=STATS.HARMEAN(B2:E6)
→ 23.6180...
```

---

### STATS.MEDIAN — 中位数

**语法**：`=STATS.MEDIAN(number1)`

**示例**：
```
=STATS.MEDIAN(B2:E6)
→ 29
```

---

### STATS.VARP — 总体方差（除以 n）

**语法**：`=STATS.VARP(number1)`

**示例**：
```
=STATS.VARP(B2:E6)
→ 132.36
```

---

### STATS.VAR — 样本方差（除以 n-1）

**语法**：`=STATS.VAR(number1)`

**示例**：
```
=STATS.VAR(B2:E6)
→ 150.2631...
```

---

### STATS.STDEVP — 总体标准差（除以 n）

**语法**：`=STATS.STDEVP(number1)`

**示例**：
```
=STATS.STDEVP(B2:E6)
→ 11.9478...
```

---

### STATS.STDEV — 样本标准差（除以 n-1）

**语法**：`=STATS.STDEV(number1)`

**示例**：
```
=STATS.STDEV(B2:E6)
→ 12.2581...
```

---

### STATS.SKEW — 样本偏度

**语法**：`=STATS.SKEW(number1)`

返回经小样本校正的偏度。对称分布 ≈ 0。此数据近似对称。

**示例**：
```
=STATS.SKEW(B2:E6)
→ ≈0（≈0.002）
```

---

### STATS.KURT — 样本超额峰度

**语法**：`=STATS.KURT(number1)`

**示例**：
```
=STATS.KURT(B2:E6)
→ -1.2828...
```

---

### STATS.MIN — 最小值

**语法**：`=STATS.MIN(number1)`

**示例**：
```
=STATS.MIN(B2:E6)    → 10
```

---

### STATS.MAX — 最大值

**语法**：`=STATS.MAX(number1)`

**示例**：
```
=STATS.MAX(B2:E6)    → 48
```

---

### STATS.RANGE — 极差

**语法**：`=STATS.RANGE(number1)`

即 max - min。

**示例**：
```
=STATS.RANGE(B2:E6)    → 38
```

---

### STATS.SUM — 求和

**语法**：`=STATS.SUM(number1)`

**示例**：
```
=STATS.SUM(B2:E6)    → 576
```

---

### STATS.PRODUCT — 求积

**语法**：`=STATS.PRODUCT(number1)`

**示例**：
```
=STATS.PRODUCT({2,3,4,5,6})    → 720
```

---

### STATS.PERCENTILE — 百分位数

**语法**：`=STATS.PERCENTILE(array, k)`

| 参数 | 说明 |
|------|------|
| `array` | 数值区域 |
| `k` | 百分位值，0–100 |

使用 R7 算法（对标 Excel PERCENTILE.EXC）。

**示例**：
```
=STATS.PERCENTILE(B2:E6, 25)   → 16.25
=STATS.PERCENTILE(B2:E6, 50)   → 29
=STATS.PERCENTILE(B2:E6, 75)   → 40.75
```

---

### STATS.IQR — 四分位距

**语法**：`=STATS.IQR(number1)`

即 Q3 - Q1（R7 分位数）。

**示例**：
```
=STATS.IQR(B2:E6)    → 22
```

---

### STATS.SUMMARY — 描述统计摘要

**语法**：`=STATS.SUMMARY(number1)`

**返回**：`double[9]` — 1×9水平数组：`[n, mean, stdev, min, q1, median, q3, max, iqr]`

**示例**：
```
=STATS.SUMMARY(B2:E6)
→ {20, 28.8, 11.804..., 10, 19.5, 29, 38.5, 48, 19}
```

---

### STATS.COUNT — 元素个数

**语法**：`=STATS.COUNT(number)`

**示例**：
```
=STATS.COUNT(B2:E6)    → 20
```

---

### STATS.MODE — 众数

**语法**：`=STATS.MODE(number)`

出现频率最高的值。全唯一返回 `#NUM!`，平局返回最小值（对标 Excel MODE.SNGL）。

**示例**：
```
=STATS.MODE({1,2,2,3,4})       → 2
=STATS.MODE(B2:E6)             → #NUM!  (20 个值全唯一)
```

---

### STATS.COVARP — 总体协方差（除以 n）

**语法**：`=STATS.COVARP(array1, array2)`

对标 Excel COVARIANCE.P。

**示例**（两列数据，各 5 行）：

| X | Y |
|---|---|
| 1 | 2 |
| 3 | 6 |
| 5 | 10 |
| 7 | 14 |
| 9 | 18 |

```
=STATS.COVARP(A2:A6, B2:B6)    → 16
```

---

### STATS.COVAR — 样本协方差（除以 n-1）

**语法**：`=STATS.COVAR(array1, array2)`

对标 Excel COVARIANCE.S。

**示例**（同上数据）：
```
=STATS.COVAR(A2:A6, B2:B6)    → 20
```

---

### STATS.PEARSON — Pearson 相关系数

**语法**：`=STATS.PEARSON(array1, array2)`

范围 -1~1。对标 Excel PEARSON。

**示例**（同上数据）：
```
=STATS.PEARSON(A2:A6, B2:B6)    → 1  (完全线性相关)
```

---

### STATS.SPEARMAN — Spearman 秩相关系数

**语法**：`=STATS.SPEARMAN(array1, array2)`

**示例**（同上数据）：
```
=STATS.SPEARMAN(A2:A6, B2:B6)    → 1
```

---

### STATS.TTEST1 — 单样本双侧 t 检验

**语法**：`=STATS.TTEST1(array, x)`

H₀: mean = x。p < 0.05 = 均值与 x 差异显著。

**示例**：
```
=STATS.TTEST1(B2:E6, 25)
→ 0.2158...  (p > 0.05，均值 28.5 与 25 无显著差异)
```

---

### STATS.TTEST2 — Welch 双样本 t 检验

**语法**：`=STATS.TTEST2(array1, array2)`

不等方差假设。p < 0.05 = 两样本均值差异显著。

**示例**（两组各 5 个值）：

| Group A | Group B |
|---------|---------|
| 10 | 18 |
| 12 | 20 |
| 14 | 22 |
| 16 | 24 |
| 15 | 21 |

```
=STATS.TTEST2(A2:A6, B2:B6)
→ 0.0003...  (p < 0.05，两组差异显著)
```

---

### STATS.ZSCORE — Z 值标准化

**语法**：`=STATS.ZSCORE(number1)`

(x - mean) / stdev。

**示例**：
```
=STATS.ZSCORE({10,20,30,40,50})
→ {-1.414, -0.707, 0, 0.707, 1.414}
```

---

### STATS.ABS — 逐元素绝对值

**语法**：`=STATS.ABS(number)`

对标 Excel ABS。支持数组。

**示例**：
```
=STATS.ABS({-10,20,-30,40,-50})
→ {10,20,30,40,50}
```

---

### STATS.SQRT — 逐元素平方根

**语法**：`=STATS.SQRT(number)`

对标 Excel SQRT。负数返回 NaN。支持数组。

**示例**：
```
=STATS.SQRT({4,9,16,25,36})
→ {2,3,4,5,6}
```

---

### STATS.LN — 逐元素自然对数

**语法**：`=STATS.LN(number)`

对标 Excel LN。非正数返回 NaN。支持数组。

**示例**：
```
=STATS.LN({1, 2.71828, 7.38906, 20.0855, 54.5982})
→ {0, 1, 2, 3, 4}   (近似)
```

---

### STATS.LOG10 — 逐元素常用对数

**语法**：`=STATS.LOG10(number)`

对标 Excel LOG10。非正数返回 NaN。支持数组。

**示例**：
```
=STATS.LOG10({1,10,100,1000,10000})
→ {0,1,2,3,4}
```

---

### STATS.EXP — 逐元素指数函数

**语法**：`=STATS.EXP(number)`

eˣ。对标 Excel EXP。支持数组。

**示例**：
```
=STATS.EXP({0,1,2,3,4})
→ {1, 2.718, 7.389, 20.086, 54.598}   (近似)
```

---

### STATS.SIGN — 逐元素符号

**语法**：`=STATS.SIGN(number)`

返回 -1, 0 或 1。NaN → 0。对标 Excel SIGN。支持数组。

**示例**：
```
=STATS.SIGN({-10, 0, 30, -0.5, 100})
→ {-1, 0, 1, -1, 1}
```

---

## 2. LINALG — 线性代数

> 基于 MathNet.Numerics 5.0.0。所有矩阵函数接受二维区域输入。
>
> **函数索引**：DET · SOLVE · MATMUL · TRANSPOSE · TRACE · RANK · COND · EIGEN · SVD_U · SVD_S · SVD_VT · QR_Q · QR_R · LU_L · LU_U · LU_P · PINV · CHOLESKY · IDENTITY

### 示例矩阵 A（4×4）

|   | A | B | C | D |
|---|---|---|---|---|
| **1** | 4 | 1 | 2 | 3 |
| **2** | 3 | 5 | 1 | 2 |
| **3** | 2 | 3 | 6 | 1 |
| **4** | 1 | 2 | 3 | 7 |

---

### LINALG.DET — 行列式

**语法**：`=LINALG.DET(array)`

对标 Excel MDETERM。

**示例**：
```
=LINALG.DET(A1:D4)    → 588
```

---

### LINALG.SOLVE — 解线性方程组 Ax = b

**语法**：`=LINALG.SOLVE(array1, array2)`

**返回**：`double[]` — 解向量 x。

**示例**（A 同上，b = {10; 12; 14; 16}）：
```
=LINALG.SOLVE(A1:D4, {10;12;14;16})
→ {2.104, 0.687, 1.149, 1.299}   (近似)
```

---

### LINALG.MATMUL — 矩阵乘法

**语法**：`=LINALG.MATMUL(array1, array2)`

对标 Excel MMULT。

**示例**（3×2 × 2×3 → 3×3）：

| A(3×2) | col0 | col1 |
|--------|------|------|
| row0 | 1 | 2 |
| row1 | 3 | 4 |
| row2 | 5 | 6 |

×

| B(2×3) | col0 | col1 | col2 |
|--------|------|------|------|
| row0 | 7 | 8 | 9 |
| row1 | 10 | 11 | 12 |

```
=LINALG.MATMUL(A1:B3, D1:F2)
```

结果（3×3）：

| 27 | 30 | 33 |
| 61 | 68 | 75 |
| 95 | 106 | 117 |

---

### LINALG.TRANSPOSE — 矩阵转置

**语法**：`=LINALG.TRANSPOSE(array)`

对标 Excel TRANSPOSE。

**示例**（2×3 → 3×2）：
```
输入：
| 1 | 2 | 3 |
| 4 | 5 | 6 |

=LINALG.TRANSPOSE(A1:C2)
→
| 1 | 4 |
| 2 | 5 |
| 3 | 6 |
```

---

### LINALG.TRACE — 矩阵迹

**语法**：`=LINALG.TRACE(array)`

对角线元素之和。

**示例**：
```
=LINALG.TRACE(A1:D4)    → 22  (4+5+6+7)
```

---

### LINALG.RANK — 数值秩

**语法**：`=LINALG.RANK(array, [tolerance])`

默认容差 1e-10。

**示例**：
```
=LINALG.RANK(A1:D4)           → 4   (满秩)
=LINALG.RANK(A1:D4, 0.01)     → 4
```

---

### LINALG.COND — 条件数

**语法**：`=LINALG.COND(array)`

2-范数条件数。

**示例**：
```
=LINALG.COND(A1:D4)    → ≈3.95
```

---

### LINALG.EIGEN — 特征值

**语法**：`=LINALG.EIGEN(array)`

要求对称矩阵，非对称返回 `#VALUE!`。

**示例**（2×2 对称矩阵）：

| 2 | 1 |
| 1 | 2 |

```
=LINALG.EIGEN(A1:B2)
→ {3, 1}
```

---

### LINALG.SVD_U / SVD_S / SVD_VT — 奇异值分解

**语法**：
- `=LINALG.SVD_U(array)` — 左奇异向量矩阵 U
- `=LINALG.SVD_S(array)` — 奇异值向量 S（降序排列）
- `=LINALG.SVD_VT(array)` — 右奇异向量转置 Vᵗ

满足 A = U · diag(S) · Vᵗ。

**示例**（3×2 矩阵）：

| 1 | 4 |
| 2 | 5 |
| 3 | 6 |

```
=LINALG.SVD_S(A1:B3)
→ {9.526, 0.514}   (近似)
```

---

### LINALG.QR_Q / QR_R — QR 分解

**语法**：
- `=LINALG.QR_Q(array)` — 正交矩阵 Q
- `=LINALG.QR_R(array)` — 上三角矩阵 R

满足 A = Q · R。

**示例**（3×3）：

| 12 | -51 | 4 |
| 6 | 167 | -68 |
| -4 | 24 | -41 |

```
=LINALG.QR_R(A1:C3)
→ 上三角矩阵（3×3），第一行约 {-14, -21, 14}
```

---

### LINALG.LU_L / LU_U / LU_P — LU 分解

**语法**：
- `=LINALG.LU_L(array)` — 下三角矩阵 L（单位对角线）
- `=LINALG.LU_U(array)` — 上三角矩阵 U
- `=LINALG.LU_P(array)` — 置换矩阵 P

满足 P·A = L·U。

**示例**：
```
=LINALG.LU_U(A1:D4)
→ 上三角矩阵（4×4）
```

---

### LINALG.PINV — Moore-Penrose 伪逆

**语法**：`=LINALG.PINV(array)`

**返回**：`double[,]` — 伪逆矩阵。

**示例**（3×2 矩阵）：
```
=LINALG.PINV(A1:B3)
→ 伪逆矩阵（2×3）
```

---

### LINALG.CHOLESKY — Cholesky 分解

**语法**：`=LINALG.CHOLESKY(array)`

要求对称正定矩阵。返回下三角 L，满足 A = L·Lᵗ。

**示例**：

| 4 | 2 |
| 2 | 3 |

```
=LINALG.CHOLESKY(A1:B2)
→
| 2      | 0      |
| 1      | 1.414  |
```

---

### LINALG.IDENTITY — 单位矩阵

**语法**：`=LINALG.IDENTITY(size)`

**返回**：`double[,]` — n×n 单位矩阵。

**示例**：
```
=LINALG.IDENTITY(3)
→
| 1 | 0 | 0 |
| 0 | 1 | 0 |
| 0 | 0 | 1 |
```

---

## 3. REGRESS — 回归分析

> 返回纵向报告表（col0 = 字段名，col1.. = 值或数组展开）。p < 0.05 = 显著；R² 越接近 1 拟合越好。
>
> **函数索引**：OLS · WLS · RIDGE · ANOVA1 · FACTORIMP · COEF · RSQ

### 示例数据集

|   | A (X1) | B (X2) | C (Y) |
|---|---|---|---|
| **1** | x1 | x2 | y |
| **2** | 1 | 2 | 5 |
| **3** | 2 | 4 | 11 |
| **4** | 3 | 6 | 17 |
| **5** | 4 | 8 | 23 |
| **6** | 5 | 10 | 29 |

---

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
关键值：R² ≈ 1.0，sse ≈ 0（完美线性关系 y = 1 + 2*x1 + 1*x2 的近似）

---

### REGRESS.WLS — 加权最小二乘法

**语法**：`=REGRESS.WLS(known_y, known_x, weights)`

用于异方差数据。返回同 OLS 的 11 行报告。

**示例**（权重 w = {1, 2, 3, 4, 5}）：
```
=REGRESS.WLS(C2:C6, A2:B6, D2:D6)
```

---

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

### REGRESS.ANOVA1 — 单因素方差分析

**语法**：`=REGRESS.ANOVA1(input_range)`

数据按列分组（每列一组）。p < 0.05 = 至少有一组均值显著不同。

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
=REGRESS.ANOVA1(A1:C5)
→ f_stat ≈ 34.4, p_value ≈ 1.2e-5  (组间差异极显著)
```

---

### REGRESS.FACTORIMP — 因子重要性排名

**语法**：`=REGRESS.FACTORIMP(known_y, known_x)`

按标准化后的 |t| 降序排列，返回 0-based 列索引。

**示例**：
```
=REGRESS.FACTORIMP(C2:C6, A2:B6)
→ {1, 0}   (X2 比 X1 更重要)
```

---

### REGRESS.COEF — OLS 回归系数

**语法**：`=REGRESS.COEF(known_y, known_x)`

**返回**：`double[]` — β 系数向量（含截距）。

**示例**：
```
=REGRESS.COEF(C2:C6, A2:B6)
→ 系数向量
```

---

### REGRESS.RSQ — 决定系数 R²

**语法**：`=REGRESS.RSQ(known_y, known_x)`

范围 0–1。1 = 完美拟合。

**示例**：
```
=REGRESS.RSQ(C2:C6, A2:B6)
→ ≈1.00
```

---

## 4. PHYCHEM — 物理化学

> 分子量计算、单位换算、理想气体状态方程。

---

> **函数索引**：MOLWT · TEMP · PRESS · VOL · MASS · C_TO_F · F_TO_C · KG_TO_LB · LB_TO_KG · L_TO_GAL · GAL_TO_L · ATM_TO_PSI · PSI_TO_ATM · IDEALGAS · GASSTP · DENSITY

### PHYCHEM.MOLWT — 分子量

**语法**：`=PHYCHEM.MOLWT(formula_text)`

**返回**：`double` — 分子量（g/mol）。

**示例**：
```
=PHYCHEM.MOLWT("H2SO4")              → 98.079
=PHYCHEM.MOLWT("NaCl")               → 58.443
=PHYCHEM.MOLWT("C6H12O6")           → 180.156
=PHYCHEM.MOLWT("Fe4[Fe(CN)6]3")     → 859.245
=PHYCHEM.MOLWT("CaCO3")             → 100.086
```

---

### PHYCHEM.TEMP — 温度换算

**语法**：`=PHYCHEM.TEMP(number, from_unit, to_unit)`

单位：`C`、`F`、`K`

**示例**：
```
=PHYCHEM.TEMP(100, "C", "F")    → 212
=PHYCHEM.TEMP(32, "F", "C")     → 0
=PHYCHEM.TEMP(0, "C", "K")      → 273.15
=PHYCHEM.TEMP(300, "K", "C")    → 26.85
=PHYCHEM.TEMP(212, "F", "K")    → 373.15
```

---

### PHYCHEM.PRESS — 压力换算

**语法**：`=PHYCHEM.PRESS(number, from_unit, to_unit)`

单位：`ATM`、`PSI`、`PA`、`KPA`、`BAR`、`MMHG`、`TORR`

**示例**：
```
=PHYCHEM.PRESS(1, "ATM", "PSI")     → 14.696
=PHYCHEM.PRESS(100, "KPA", "ATM")   → 0.987
=PHYCHEM.PRESS(760, "MMHG", "ATM")  → 1.0
=PHYCHEM.PRESS(1, "BAR", "KPA")     → 100
=PHYCHEM.PRESS(14.7, "PSI", "BAR")  → 1.013
```

---

### PHYCHEM.VOL — 体积换算

**语法**：`=PHYCHEM.VOL(number, from_unit, to_unit)`

单位：`L`、`ML`、`M3`、`GAL`、`QT`、`FT3`

**示例**：
```
=PHYCHEM.VOL(1, "L", "ML")      → 1000
=PHYCHEM.VOL(1, "GAL", "L")     → 3.785
=PHYCHEM.VOL(1, "M3", "L")      → 1000
=PHYCHEM.VOL(500, "ML", "L")    → 0.5
=PHYCHEM.VOL(1, "FT3", "L")     → 28.317
```

---

### PHYCHEM.MASS — 质量换算

**语法**：`=PHYCHEM.MASS(number, from_unit, to_unit)`

单位：`KG`、`G`、`MG`、`LB`、`OZ`、`TON`

**示例**：
```
=PHYCHEM.MASS(1, "KG", "LB")     → 2.205
=PHYCHEM.MASS(1, "TON", "KG")    → 1000
=PHYCHEM.MASS(100, "G", "KG")    → 0.1
=PHYCHEM.MASS(16, "OZ", "LB")    → 1.0
=PHYCHEM.MASS(1, "KG", "MG")     → 1000000
```

---

### PHYCHEM.C_TO_F / F_TO_C — 温度快捷换算

**示例**：
```
=PHYCHEM.C_TO_F(0)     → 32
=PHYCHEM.C_TO_F(100)   → 212
=PHYCHEM.F_TO_C(32)    → 0
=PHYCHEM.F_TO_C(212)   → 100
```

---

### PHYCHEM.KG_TO_LB / LB_TO_KG — 质量快捷换算

**示例**：
```
=PHYCHEM.KG_TO_LB(10)    → 22.046
=PHYCHEM.LB_TO_KG(10)    → 4.536
```

---

### PHYCHEM.L_TO_GAL / GAL_TO_L — 体积快捷换算

**示例**：
```
=PHYCHEM.L_TO_GAL(10)     → 2.642
=PHYCHEM.GAL_TO_L(10)     → 37.854
```

---

### PHYCHEM.ATM_TO_PSI / PSI_TO_ATM — 压力快捷换算

**示例**：
```
=PHYCHEM.ATM_TO_PSI(2)    → 29.392
=PHYCHEM.PSI_TO_ATM(30)   → 2.041
```

---

### PHYCHEM.IDEALGAS — 理想气体状态方程

**语法**：`=PHYCHEM.IDEALGAS(pressure, volume, moles, temperature)`

PV = nRT。将待求量填 `"*"`。R = 0.082057 L·atm/(mol·K)。

**示例**（标准状况下 1 mol 理想气体）：
```
=PHYCHEM.IDEALGAS("*", 22.414, 1, 273.15)    → P ≈ 1.0 atm
=PHYCHEM.IDEALGAS(1, "*", 1, 273.15)          → V ≈ 22.414 L
=PHYCHEM.IDEALGAS(1, 22.414, "*", 273.15)     → n ≈ 1.0 mol
=PHYCHEM.IDEALGAS(1, 22.414, 1, "*")          → T ≈ 273.15 K
```

---

### PHYCHEM.GASSTP — 气体体积换算标况

**语法**：`=PHYCHEM.GASSTP(volume, temperature, pressure)`

换算到 STP（273.15K, 1atm）：V_stp = V × P / 1 × 273.15 / T。

**示例**：
```
=PHYCHEM.GASSTP(10, 300, 1.5)
→ ≈13.65 L
```

---

### PHYCHEM.DENSITY — 密度

**语法**：`=PHYCHEM.DENSITY(mass, volume)`

密度 = 质量 / 体积。零体积返回 NaN（`#NUM!`）。

**示例**：
```
=PHYCHEM.DENSITY(100, 2)     → 50
=PHYCHEM.DENSITY(50, 0.5)    → 100
=PHYCHEM.DENSITY(10, 0)      → #NUM!
```

---

## 5. STR — 字符串处理

> 除 TEXTJOIN/UUID/RND* 外均支持数组公式（逐元素处理）。
>
> **函数索引**：REVERSE · NORMWS · TITLE · REMOVE · KEEP · PADLEFT · PADRIGHT · TRUNCATE · COUNTSUB · STARTSWITH · ENDSWITH · LEFTOF · RIGHTOF · EXTRACT · NTHWORD · COMMONPFX · TEXTJOIN · LEVENSHTEIN · SOUNDEX · URLENCODE · URLDECODE · HTMLENCODE · HTMLDECODE · BASE64ENC · BASE64DEC · UUID · RNDSTR · RNDALPHA · RNDNUM · ISNULLEMPTY · ISNULLWS · COALESCE · FORMAT · STRIPHTML

### 示例文本数据

|   | A | B | C | D |
|---|---|---|---|---|
| **1** | Name | Code | Description | URL |
| **2** | Alice Johnson | AL001 | Sales & Marketing | https://example.com/path |
| **3** | Bob Smith | BR002 | R&D Department | https://test.org/query?id=1 |
| **4** | Carol White | CW003 | Customer Support | https://mysite.net/about us |
| **5** | David Brown | DB004 | Engineering | https://api.io/v1/data.json |
| **6** | Eva Martinez | EM005 | Human Resources | https://portal.com/login?r=1 |

---

### STR.REVERSE — 反转字符串

**语法**：`=STR.REVERSE(text)`

**示例**：
```
=STR.REVERSE("hello")      → "olleh"
=STR.REVERSE("Excel")      → "lecxE"
```

---

### STR.NORMWS — 规范化空白

**语法**：`=STR.NORMWS(text)`

去首尾空格，合并连续空格为单个。

**示例**：
```
=STR.NORMWS("  hello   world  ")    → "hello world"
=STR.NORMWS("a   b    c     d")      → "a b c d"
```

---

### STR.TITLE — 首字母大写

**语法**：`=STR.TITLE(text)`

**示例**：
```
=STR.TITLE("hello world")          → "Hello World"
=STR.TITLE("sales & marketing")    → "Sales & Marketing"
```

---

### STR.REMOVE — 删除字符

**语法**：`=STR.REMOVE(text, old_text)`

删除 text 中所有出现在 old_text 里的字符。

**示例**：
```
=STR.REMOVE("abc123def456", "0123456789")     → "abcdef"
=STR.REMOVE("hello-world_test", "-_")          → "helloworldtest"
```

---

### STR.KEEP — 保留字符

**语法**：`=STR.KEEP(text, keep_chars)`

仅保留 text 中出现在 keep_chars 里的字符。

**示例**：
```
=STR.KEEP("abc123def456", "0123456789")        → "123456"
=STR.KEEP("Tel: (555) 123-4567", "0123456789") → "5551234567"
```

---

### STR.PADLEFT — 左侧填充

**语法**：`=STR.PADLEFT(text, num_chars, [pad_text])`

**示例**：
```
=STR.PADLEFT("42", 5, "0")     → "00042"
=STR.PADLEFT("ABC", 6, "-")    → "---ABC"
=STR.PADLEFT("X", 4)           → "   X"   (默认空格)
```

---

### STR.PADRIGHT — 右侧填充

**语法**：`=STR.PADRIGHT(text, num_chars, [pad_text])`

**示例**：
```
=STR.PADRIGHT("42", 5, "0")    → "42000"
=STR.PADRIGHT("ABC", 6, ".")   → "ABC..."
```

---

### STR.TRUNCATE — 截断

**语法**：`=STR.TRUNCATE(text, num_chars, [suffix])`

若截短则追加后缀（默认 `"..."`）。

**示例**：
```
=STR.TRUNCATE("Hello World", 8)         → "Hello..."
=STR.TRUNCATE("Hello World", 8, "…")    → "Hello W…"
=STR.TRUNCATE("Short", 10)              → "Short"
```

---

### STR.COUNTSUB — 子串计数

**语法**：`=STR.COUNTSUB(text, substring, [match_case])`

**示例**：
```
=STR.COUNTSUB("banana", "na")                 → 2
=STR.COUNTSUB("Banana BANANA", "ba", FALSE)   → 2
=STR.COUNTSUB("Banana BANANA", "ba", TRUE)    → 0
```

---

### STR.STARTSWITH — 判断前缀

**语法**：`=STR.STARTSWITH(text, prefix, [match_case])`

**示例**：
```
=STR.STARTSWITH("Hello World", "Hello")        → TRUE
=STR.STARTSWITH("Hello World", "hello")        → FALSE
=STR.STARTSWITH("Hello World", "hello", FALSE) → TRUE
```

---

### STR.ENDSWITH — 判断后缀

**语法**：`=STR.ENDSWITH(text, suffix, [match_case])`

**示例**：
```
=STR.ENDSWITH("report.pdf", ".pdf")          → TRUE
=STR.ENDSWITH("report.PDF", ".pdf", FALSE)   → TRUE
```

---

### STR.LEFTOF — 分隔符左侧

**语法**：`=STR.LEFTOF(text, delimiter, [instance_num])`

instance_num: 1=第1次（默认），-1=最后一次。

**示例**：
```
=STR.LEFTOF("a,b,c,d", ",")       → "a"
=STR.LEFTOF("a,b,c,d", ",", 2)    → "a,b"
=STR.LEFTOF("a,b,c,d", ",", -1)   → "a,b,c"
```

---

### STR.RIGHTOF — 分隔符右侧

**语法**：`=STR.RIGHTOF(text, delimiter, [instance_num])`

**示例**：
```
=STR.RIGHTOF("a,b,c,d", ",")       → "b,c,d"
=STR.RIGHTOF("a,b,c,d", ",", 2)    → "c,d"
=STR.RIGHTOF("a,b,c,d", ",", -1)   → "d"
```

---

### STR.EXTRACT — 分隔符间提取

**语法**：`=STR.EXTRACT(text, start_delimiter, end_delimiter, [instance_num], [include_delimiters])`

**示例**：
```
=STR.EXTRACT("a[b]c[d]e", "[", "]")          → "b"
=STR.EXTRACT("a[b]c[d]e", "[", "]", 2)       → "d"
=STR.EXTRACT("a[b]c[d]e", "[", "]", 1, TRUE) → "[b]"
```

---

### STR.NTHWORD — 第 N 个词

**语法**：`=STR.NTHWORD(text, [instance_num])`

空格分隔，1-based。

**示例**：
```
=STR.NTHWORD("The quick brown fox")      → "The"
=STR.NTHWORD("The quick brown fox", 3)   → "brown"
=STR.NTHWORD("The quick brown fox", -1)  → "fox"
```

---

### STR.COMMONPFX — 最长公共前缀

**语法**：`=STR.COMMONPFX(text1, text2, [match_case])`

**示例**：
```
=STR.COMMONPFX("hello world", "hello there")   → "hello "
=STR.COMMONPFX("prefix_abc", "prefix_xyz")     → "prefix_"
=STR.COMMONPFX("Hello", "hello", FALSE)        → "hello"
=STR.COMMONPFX("Hello", "hello", TRUE)         → ""
```

---

### STR.TEXTJOIN — 文本连接

**语法**：`=STR.TEXTJOIN(delimiter, ignore_empty, text_array)`

对标 Excel TEXTJOIN。ignore_empty=TRUE 跳过空值。

**示例**：
```
=STR.TEXTJOIN(", ", TRUE, A2:A6)
→ "Alice Johnson, Bob Smith, Carol White, David Brown, Eva Martinez"

=STR.TEXTJOIN("|", FALSE, B2:B6)
→ "AL001|BR002|CW003|DB004|EM005"
```

---

### STR.LEVENSHTEIN — 编辑距离

**语法**：`=STR.LEVENSHTEIN(text1, text2)`

**示例**：
```
=STR.LEVENSHTEIN("kitten", "sitting")    → 3
=STR.LEVENSHTEIN("book", "back")         → 2
=STR.LEVENSHTEIN("hello", "hello")       → 0
```

---

### STR.SOUNDEX — Soundex 编码

**语法**：`=STR.SOUNDEX(text)`

**示例**：
```
=STR.SOUNDEX("Robert")    → "R163"
=STR.SOUNDEX("Rupert")    → "R163"
=STR.SOUNDEX("Smith")     → "S530"
```

---

### STR.URLENCODE / STR.URLDECODE — URL 编解码

**示例**：
```
=STR.URLENCODE("hello world")             → "hello+world"
=STR.URLENCODE("a=1&b=2")                 → "a%3d1%26b%3d2"
=STR.URLDECODE("hello+world")             → "hello world"
```

---

### STR.HTMLENCODE / STR.HTMLDECODE — HTML 编解码

**示例**：
```
=STR.HTMLENCODE("<div class='x'>")    → "&lt;div class='x'&gt;"
=STR.HTMLDECODE("&lt;div&gt;")        → "<div>"
=STR.HTMLENCODE("a & b")             → "a &amp; b"
```

---

### STR.BASE64ENC / STR.BASE64DEC — Base64 编解码

**示例**：
```
=STR.BASE64ENC("Hello World")    → "SGVsbG8gV29ybGQ="
=STR.BASE64DEC("SGVsbG8=")       → "Hello"
```

---

### STR.UUID — 生成 UUID

**语法**：`=STR.UUID()`

**示例**：
```
=STR.UUID()    → "a1b2c3d4-e5f6-7890-abcd-ef1234567890"（随机）
```

---

### STR.RNDSTR / RNDALPHA / RNDNUM — 随机字符串

**语法**：
- `=STR.RNDSTR(num_chars, [character_set])` — 从字符集随机生成
- `=STR.RNDALPHA(num_chars)` — 纯字母（A-Z, a-z）
- `=STR.RNDNUM(num_chars)` — 纯数字（0-9）

**示例**：
```
=STR.RNDSTR(8)               → "aB3xK9mQ"（随机）
=STR.RNDSTR(6, "ABC123")     → "C1A3B2"（随机）
=STR.RNDALPHA(5)             → "HgKpL"（随机）
=STR.RNDNUM(4)               → "7291"（随机）
```

---

### STR.ISNULLEMPTY / STR.ISNULLWS — 空值检测

**示例**：
```
=STR.ISNULLEMPTY("")       → TRUE
=STR.ISNULLEMPTY("hello")  → FALSE
=STR.ISNULLWS("   ")       → TRUE
=STR.ISNULLWS("hello")     → FALSE
```

---

### STR.COALESCE — 取首个非空值

**语法**：`=STR.COALESCE(value1, value2)`

返回第一个非 null/空值。

**示例**：
```
=STR.COALESCE("", "default")     → "default"
=STR.COALESCE("hello", "bye")    → "hello"
```

---

### STR.FORMAT — 格式化值

**语法**：`=STR.FORMAT(value, format_text)`

按 .NET 格式字符串格式化。对标 Excel TEXT。

**示例**：
```
=STR.FORMAT(1234.567, "0.00")                  → "1234.57"
=STR.FORMAT(DATE(2024,6,15), "yyyy-MM-dd")     → "2024-06-15"
=STR.FORMAT(0.25, "0.00%")                     → "25.00%"
```

---

### STR.STRIPHTML — 去除 HTML 标签

**语法**：`=STR.STRIPHTML(text)`

**示例**：
```
=STR.STRIPHTML("<p>Hello <b>World</b></p>")  → "Hello World"
=STR.STRIPHTML("<a href='x'>link</a>")        → "link"
```

---

## 6. DT — 日期时间

> 日期参数接受 Excel 日期序列号。start_day: 0=Sun, 1=Mon, ...（默认 1=Mon）。
>
> **函数索引**：ISOWEEK · WEEKDAY · WEEKDAYISO · WEEKDAYNAME · SOW · EOW · SOM · EOM · WOM · DIM · AGEYEARS · AGEMONTHS · AGEDAYS · ISWE · ADDWKD · WKDBTWN · NEXTWKD · EASTER · QUARTER · SEMESTER · DOY · ISLEAP · UNIXTS · FROMUNIX · DATEDIFF

### 示例日期

|   | A | B |
|---|---|---|
| **1** | Event | Date |
| **2** | New Year | 2024-01-01 |
| **3** | Spring Eq. | 2024-03-20 |
| **4** | Mid Year | 2024-07-01 |
| **5** | Autumn Eq. | 2024-09-22 |
| **6** | Christmas | 2024-12-25 |

---

### DT.ISOWEEK — ISO 8601 周数

**语法**：`=DT.ISOWEEK(serial_number)`

对标 Excel ISOWEEKNUM。

**示例**：
```
=DT.ISOWEEK(DATE(2024,1,1))     → 1
=DT.ISOWEEK(DATE(2024,7,1))     → 27
=DT.ISOWEEK(DATE(2024,12,25))   → 52
```

---

### DT.WEEKDAY — 星期几（VBA 风格）

**语法**：`=DT.WEEKDAY(serial_number)`

Sun=1, Sat=7。

**示例**：
```
=DT.WEEKDAY(DATE(2024,1,1))     → 2  (周一)
=DT.WEEKDAY(DATE(2024,6,15))    → 7  (周六)
=DT.WEEKDAY(DATE(2024,12,25))   → 4  (周三)
```

---

### DT.WEEKDAYISO — 星期几（ISO 风格）

**语法**：`=DT.WEEKDAYISO(serial_number)`

Mon=1, Sun=7。

**示例**：
```
=DT.WEEKDAYISO(DATE(2024,1,1))    → 1  (周一)
=DT.WEEKDAYISO(DATE(2024,6,16))   → 7  (周日)
```

---

### DT.WEEKDAYNAME — 英文星期名

**语法**：`=DT.WEEKDAYNAME(serial_number)`

**示例**：
```
=DT.WEEKDAYNAME(DATE(2024,1,1))     → "Monday"
=DT.WEEKDAYNAME(DATE(2024,6,16))    → "Sunday"
```

---

### DT.SOW / DT.EOW — 周开始/结束日期

**语法**：
- `=DT.SOW(serial_number, [start_day])` — Start of Week
- `=DT.EOW(serial_number, [start_day])` — End of Week

start_day: 0=周日, 1=周一（默认）。

**示例**（2024-06-15 是周六）：
```
=DT.SOW(DATE(2024,6,15))        → 2024-06-10  (周一)
=DT.EOW(DATE(2024,6,15))        → 2024-06-16  (周日)
=DT.SOW(DATE(2024,6,15), 0)     → 2024-06-09  (周日)
```

---

### DT.SOM / DT.EOM — 月初/月末

**语法**：
- `=DT.SOM(serial_number)` — Start of Month
- `=DT.EOM(serial_number)` — End of Month（对标 EOMONTH）

**示例**：
```
=DT.SOM(DATE(2024,6,15))     → 2024-06-01
=DT.EOM(DATE(2024,6,15))     → 2024-06-30
=DT.EOM(DATE(2024,2,1))      → 2024-02-29  (闰年)
```

---

### DT.WOM — 当月第几周

**语法**：`=DT.WOM(serial_number, [start_day])`

返回 1–5。

**示例**：
```
=DT.WOM(DATE(2024,6,1))      → 1
=DT.WOM(DATE(2024,6,15))     → 3
```

---

### DT.DIM — 指定年月天数

**语法**：`=DT.DIM(year, month)`

**示例**：
```
=DT.DIM(2024, 2)     → 29   (闰年)
=DT.DIM(2023, 2)     → 28
=DT.DIM(2024, 4)     → 30
=DT.DIM(2024, 12)    → 31
```

---

### DT.AGEYEARS / AGEMONTHS / AGEDAYS — 年龄计算

**语法**：
- `=DT.AGEYEARS(start_date, [end_date])` — 周岁（对标 DATEDIF）
- `=DT.AGEMONTHS(start_date, [end_date])` — 足月数
- `=DT.AGEDAYS(start_date, [end_date])` — 总天数

end_date 默认今天。

**示例**（2000-01-15 → 2024-06-15）：
```
=DT.AGEYEARS(DATE(2000,1,15), DATE(2024,6,15))   → 24
=DT.AGEMONTHS(DATE(2000,1,15), DATE(2024,6,15))  → 293
=DT.AGEDAYS(DATE(2000,1,15), DATE(2024,6,15))    → 8918
```

---

### DT.ISWE — 是否周末

**语法**：`=DT.ISWE(serial_number)`

周六或周日返回 TRUE。

**示例**：
```
=DT.ISWE(DATE(2024,6,15))    → TRUE   (周六)
=DT.ISWE(DATE(2024,6,17))    → FALSE  (周一)
```

---

### DT.ADDWKD — 加工作日

**语法**：`=DT.ADDWKD(start_date, workdays)`

跳过周末（Mon-Fri）。对标 Excel WORKDAY。

**示例**：
```
=DT.ADDWKD(DATE(2024,6,14), 1)    → 2024-06-17  (周五 + 1 = 周一)
=DT.ADDWKD(DATE(2024,6,14), 5)    → 2024-06-21  (周五 + 5 = 周五)
```

---

### DT.WKDBTWN — 工作日计数

**语法**：`=DT.WKDBTWN(start_date, end_date)`

对标 Excel NETWORKDAYS。

**示例**：
```
=DT.WKDBTWN(DATE(2024,6,3), DATE(2024,6,7))    → 4  (不含起始日)
=DT.WKDBTWN(DATE(2024,6,1), DATE(2024,6,30))   → 21
```

---

### DT.NEXTWKD — 下一个工作日

**语法**：`=DT.NEXTWKD(serial_number)`

若当天为工作日则返回自身；否则返回下一个工作日。

**示例**：
```
=DT.NEXTWKD(DATE(2024,6,14))    → 2024-06-14  (周五 → 自身)
=DT.NEXTWKD(DATE(2024,6,15))    → 2024-06-17  (周六 → 周一)
```

---

### DT.EASTER — 复活节日期

**语法**：`=DT.EASTER(year)`

公历算法。

**示例**：
```
=DT.EASTER(2024)    → 2024-03-31
=DT.EASTER(2025)    → 2025-04-20
=DT.EASTER(2026)    → 2026-04-05
```

---

### DT.QUARTER / DT.SEMESTER — 季度/半年度

**示例**：
```
=DT.QUARTER(DATE(2024,3,20))     → 1
=DT.QUARTER(DATE(2024,7,1))      → 3
=DT.SEMESTER(DATE(2024,3,20))    → 1
=DT.SEMESTER(DATE(2024,9,22))    → 2
```

---

### DT.DOY — 年内第几天

**语法**：`=DT.DOY(serial_number)`

返回 1–366。

**示例**：
```
=DT.DOY(DATE(2024,1,1))      → 1
=DT.DOY(DATE(2024,6,15))     → 167
=DT.DOY(DATE(2024,12,31))    → 366  (闰年)
```

---

### DT.ISLEAP — 是否闰年

**语法**：`=DT.ISLEAP(year)`

**示例**：
```
=DT.ISLEAP(2024)    → TRUE
=DT.ISLEAP(2023)    → FALSE
=DT.ISLEAP(2000)    → TRUE
=DT.ISLEAP(1900)    → FALSE
```

---

### DT.UNIXTS — Excel 日期 → Unix 时间戳

**语法**：`=DT.UNIXTS(serial_number)`

**示例**：
```
=DT.UNIXTS(DATE(2024,1,1))    → 1704067200
=DT.UNIXTS(DATE(1970,1,1))    → 0
```

---

### DT.FROMUNIX — Unix 时间戳 → Excel 日期

**语法**：`=DT.FROMUNIX(unix_timestamp)`

**示例**：
```
=DT.FROMUNIX(1704067200)    → 2024-01-01
=DT.FROMUNIX(0)             → 1970-01-01
```

---

### DT.DATEDIFF — 日期差

**语法**：`=DT.DATEDIFF(date_unit, start_date, end_date)`

单位：`"d"`=天, `"m"`=月, `"y"`=年, `"w"`=周。

**示例**：
```
=DT.DATEDIFF("d", DATE(2024,1,1), DATE(2024,12,31))   → 365
=DT.DATEDIFF("m", DATE(2024,1,1), DATE(2024,12,31))   → 11
=DT.DATEDIFF("y", DATE(2020,1,1), DATE(2024,1,1))     → 4
=DT.DATEDIFF("w", DATE(2024,1,1), DATE(2024,2,1))     → 4
```

---

## 7. REGEX — 正则表达式

> .NET 正则引擎。支持数组公式（逐元素），超时 5 秒自动取消。
>
> **函数索引**：TEST · COUNT · MATCH · MATCHALL · REPLACE · SPLIT · GROUPS · ESCAPE · ISMATCH

### 示例文本

|   | A |
|---|---|
| **1** | Text |
| **2** | The quick brown fox jumps over the lazy dog |
| **3** | Contact: alice@example.com or bob@test.org |
| **4** | Phone: (555) 123-4567, Fax: (555) 765-4321 |
| **5** | Prices: $19.99, $299.50, $1,200.00 |
| **6** | Date: 2024-06-15, Time: 14:30:00 |

---

### REGEX.TEST — 是否匹配正则

**语法**：`=REGEX.TEST(text, pattern, [ignore_case])`

**示例**：
```
=REGEX.TEST("hello123", "\d+")            → TRUE
=REGEX.TEST("hello", "\d+")               → FALSE
=REGEX.TEST("ABC", "abc", FALSE)          → FALSE
=REGEX.TEST("ABC", "abc", TRUE)           → TRUE
```

---

### REGEX.COUNT — 非重叠匹配次数

**语法**：`=REGEX.COUNT(text, pattern, [ignore_case])`

**示例**：
```
=REGEX.COUNT("a1b2c3d4", "\d")                   → 4
=REGEX.COUNT("The cat and the Cat", "cat", TRUE)  → 2
```

---

### REGEX.MATCH — 第 N 个匹配子串

**语法**：`=REGEX.MATCH(text, pattern, [ignore_case], [instance_num])`

1=第一个（默认），-1=最后一个。

**示例**：
```
=REGEX.MATCH("a1b2c3", "\d+")           → "1"
=REGEX.MATCH("a1b2c3", "\d+", , 2)      → "2"
=REGEX.MATCH("a1b2c3", "\d+", , -1)     → "3"
```

---

### REGEX.MATCHALL — 所有匹配

**语法**：`=REGEX.MATCHALL(text, pattern, [ignore_case])`

**返回**：`string[]`

**示例**：
```
=REGEX.MATCHALL("a1b22c333", "\d+")
→ {"1", "22", "333"}
```

---

### REGEX.REPLACE — 正则替换

**语法**：`=REGEX.REPLACE(text, pattern, replacement, [ignore_case], [instance_num])`

0/省略=替换全部（默认），1=第一个，-1=最后一个。

**示例**：
```
=REGEX.REPLACE("a1b2c3", "\d", "X")           → "aXbXcX"
=REGEX.REPLACE("a1b2c3", "\d", "X", , 1)      → "aXb2c3"
=REGEX.REPLACE("a1b2c3", "\d", "X", , -1)     → "a1b2cX"
```

---

### REGEX.SPLIT — 正则拆分

**语法**：`=REGEX.SPLIT(text, pattern, [ignore_case], [instance_num])`

0/省略=全部拆分，>0=最多拆 instance_num 次。

**返回**：`string[]`

**示例**：
```
=REGEX.SPLIT("a,b;c|d", "[,;|]")
→ {"a", "b", "c", "d"}

=REGEX.SPLIT("one123two456three", "\d+", , 1)
→ {"one", "two456three"}   (仅拆第1次)
```

---

### REGEX.GROUPS — 捕获组

**语法**：`=REGEX.GROUPS(text, pattern, [ignore_case])`

**返回**：`object[2,n]` — row0=组名, row1=值。

**示例**：
```
=REGEX.GROUPS("John Doe, 35", "(\w+)\s(\w+),\s(\d+)")
```
结果（2×4）：

| | 0 | 1 | 2 | 3 |
|---|---|---|---|---|
| Names | 0 | 1 | 2 | 3 |
| Values | John Doe, 35 | John | Doe | 35 |

---

### REGEX.ESCAPE — 转义正则特殊字符

**语法**：`=REGEX.ESCAPE(text)`

**示例**：
```
=REGEX.ESCAPE("a.b(c)")      → "a\.b\(c\)"
=REGEX.ESCAPE("$100.00")     → "\$100\.00"
```

---

### REGEX.ISMATCH — 不区分大小写匹配

**语法**：`=REGEX.ISMATCH(text, pattern)`

等同于 `REGEX.TEST(text, pattern, TRUE)`。

**示例**：
```
=REGEX.ISMATCH("HELLO", "hello")     → TRUE
=REGEX.ISMATCH("world", "hello")     → FALSE
```

---

## 8. ARR — 数组操作

> 一维数组操作函数集。
>
> **函数索引**：SORT · SORTASC · SORTDESC · SORTNUM · SORTTEXT · UNIQUE · INDEXOF · SLICE · FLATTEN · FILTER · FILTER_EQ · FILTER_NE · FILTER_GT · FILTER_LT · CONCAT · REVERSE · COUNT · CONTAINS · TOSET · FILL · RANGE · SHUFFLE

### 示例数据

|   | A | B | C | D |
|---|---|---|---|---|
| **1** | Item | Price | Qty | Category |
| **2** | Apple | 5.5 | 10 | Fruit |
| **3** | Banana | 3.2 | 20 | Fruit |
| **4** | Carrot | 2.1 | 30 | Vegetable |
| **5** | Date | 8.0 | 15 | Fruit |
| **6** | Eggplant | 4.5 | 25 | Vegetable |

---

### ARR.SORT — 排序

**语法**：`=ARR.SORT(array, [sort_order], [sort_mode])`

sort_order: TRUE=升序（默认），FALSE=降序。sort_mode: `"auto"/"text"/"numeric"`。

**示例**：
```
=ARR.SORT(B2:B6)              → {2.1, 3.2, 4.5, 5.5, 8.0}
=ARR.SORT(B2:B6, FALSE)       → {8.0, 5.5, 4.5, 3.2, 2.1}
=ARR.SORT(A2:A6, TRUE, "text") → {"Apple","Banana","Carrot","Date","Eggplant"}
```

---

### ARR.SORTASC / ARR.SORTDESC — 升序/降序

**示例**：
```
=ARR.SORTASC({5,2,8,1,9})     → {1,2,5,8,9}
=ARR.SORTDESC({5,2,8,1,9})    → {9,8,5,2,1}
```

---

### ARR.SORTNUM / ARR.SORTTEXT — 按类型排序

**示例**：
```
=ARR.SORTNUM({"10","2","1","20"})           → {"1","2","10","20"}
=ARR.SORTTEXT({"Banana","apple","Carrot"})   → {"apple","Banana","Carrot"}
```

---

### ARR.UNIQUE / ARR.TOSET — 去重

**语法**：`=ARR.UNIQUE(array)`

保留首次出现顺序。对标 Excel UNIQUE。

**示例**：
```
=ARR.UNIQUE({1,2,2,3,3,3,4,5,5})    → {1,2,3,4,5}
```

---

### ARR.INDEXOF — 查找索引

**语法**：`=ARR.INDEXOF(array, lookup_value)`

0-based，未找到返回 -1。对标 Excel MATCH。

**示例**：
```
=ARR.INDEXOF(A2:A6, "Carrot")     → 2
=ARR.INDEXOF(A2:A6, "Orange")     → -1
```

---

### ARR.SLICE — 切片

**语法**：`=ARR.SLICE(array, start_index, num_elements)`

**示例**：
```
=ARR.SLICE({10,20,30,40,50}, 1, 3)    → {20,30,40}
=ARR.SLICE({10,20,30,40,50}, 2, 2)    → {30,40}
```

---

### ARR.FLATTEN — 展平二维数组

**语法**：`=ARR.FLATTEN(array)`

按行展为一维。对标 Excel TOROW。

**示例**：
```
=ARR.FLATTEN(B2:C3)    → {5.5, 10, 3.2, 20}
```

---

### ARR.FILTER — 按条件过滤

**语法**：`=ARR.FILTER(array, criteria, comparison_operator)`

运算符：`"=", "<>", ">", "<", ">=", "<="`。对标 Excel FILTER。

**示例**：
```
=ARR.FILTER(B2:B6, 5, ">")      → {5.5, 8.0}
=ARR.FILTER(B2:B6, 3, "<=")     → {3.2, 2.1}
```

---

### ARR.FILTER_EQ / NE / GT / LT — 快捷过滤

**示例**：
```
=ARR.FILTER_EQ(D2:D6, "Fruit")      → {"Fruit","Fruit","Fruit"}
=ARR.FILTER_NE(D2:D6, "Fruit")      → {"Vegetable","Vegetable"}
=ARR.FILTER_GT(B2:B6, 5)            → {5.5, 8.0}
=ARR.FILTER_LT(B2:B6, 3)            → {2.1}
```

---

### ARR.CONCAT — 数组拼接

**语法**：`=ARR.CONCAT(array1, array2)`

对标 Excel VSTACK/HSTACK（一维版）。

**示例**：
```
=ARR.CONCAT({1,2,3}, {4,5,6})    → {1,2,3,4,5,6}
```

---

### ARR.REVERSE — 反转顺序

**示例**：
```
=ARR.REVERSE({1,2,3,4,5})    → {5,4,3,2,1}
```

---

### ARR.COUNT — 元素个数

**示例**：
```
=ARR.COUNT(B2:B6)    → 5
```

---

### ARR.CONTAINS — 是否包含

**示例**：
```
=ARR.CONTAINS(A2:A6, "Banana")    → TRUE
=ARR.CONTAINS(A2:A6, "Orange")    → FALSE
```

---

### ARR.FILL — 填充数组

**语法**：`=ARR.FILL(value, count)`

**示例**：
```
=ARR.FILL("Hello", 5)    → {"Hello","Hello","Hello","Hello","Hello"}
=ARR.FILL(0, 4)          → {0,0,0,0}
```

---

### ARR.RANGE — 生成序列

**语法**：`=ARR.RANGE(start, end, step)`

对标 Excel SEQUENCE。最大 100,000 元素。

**示例**：
```
=ARR.RANGE(1, 10, 2)     → {1,3,5,7,9}
=ARR.RANGE(5, 25, 5)     → {5,10,15,20,25}
=ARR.RANGE(10, 1, -2)    → {10,8,6,4,2}
```

---

### ARR.SHUFFLE — 随机打乱

**语法**：`=ARR.SHUFFLE(array)`

Fisher-Yates 算法。

**示例**：
```
=ARR.SHUFFLE({1,2,3,4,5})    → 如 {3,1,5,2,4}（随机）
```

---

## 9. DICT — 字典/集合

> 频率统计、集合运算、字典构建。
>
> **函数索引**：FREQUENCY · INTERSECT · UNION · EXCEPT · DICT · COUNT · KEYS · VALUES

### 示例数据

|   | A (Keys) | B (Values) |
|---|---|---|
| **1** | Apple | 100 |
| **2** | Banana | 200 |
| **3** | Apple | 150 |
| **4** | Cherry | 50 |
| **5** | Banana | 250 |
| **6** | Date | 300 |

---

### DICT.FREQUENCY — 频率统计

**语法**：`=DICT.FREQUENCY(key_array)`

**返回**：`object[2,n]` — 两列：value, count。

**示例**：
```
=DICT.FREQUENCY(A2:A6)
```

| value | count |
|-------|-------|
| Apple | 2 |
| Banana | 2 |
| Cherry | 1 |
| Date | 1 |

---

### DICT.INTERSECT — 交集

**语法**：`=DICT.INTERSECT(array1, array2)`

两个数组都有的值。

**示例**：
```
=DICT.INTERSECT({1,2,3,4}, {3,4,5,6})    → {3,4}
```

---

### DICT.UNION — 并集

**语法**：`=DICT.UNION(array1, array2)`

所有不重复值。

**示例**：
```
=DICT.UNION({1,2,3}, {3,4,5})    → {1,2,3,4,5}
```

---

### DICT.EXCEPT — 差集

**语法**：`=DICT.EXCEPT(array1, array2)`

在 array1 但不在 array2 的值。

**示例**：
```
=DICT.EXCEPT({1,2,3,4}, {3,4})    → {1,2}
```

---

### DICT.DICT — 构建字典表

**语法**：`=DICT.DICT(key_array, value_array)`

**返回**：`object[2,n]` — 双列表格。

**示例**：
```
=DICT.DICT({"A","B","C"}, {1,2,3})
```

| A | 1 |
| B | 2 |
| C | 3 |

---

### DICT.COUNT — 字典行数

**语法**：`=DICT.COUNT(dict_table)`

**示例**：
```
=DICT.COUNT(previous_result)    → 3
```

---

### DICT.KEYS / DICT.VALUES — 提取键/值

**语法**：
- `=DICT.KEYS(dict_table)` — 提取第一列（键）
- `=DICT.VALUES(dict_table)` — 提取第二列（值）

**示例**：
```
=DICT.KEYS(dict_table)     → {"A","B","C"}
=DICT.VALUES(dict_table)   → {1,2,3}
```

---

## 10. JSON / XML — 数据处理

> **函数索引**：JSON.PARSE · JSON.QUERY · JSON.VALIDATE · JSON.PRETTIFY · JSON.TOTABLE · XML.XPATH · XML.VALIDATE · XML.TOTABLE

### 示例 JSON（放在单元格 A1 中）

```json
[{"Name":"Alice","Age":30,"City":"NYC"},{"Name":"Bob","Age":25,"City":"LA"},{"Name":"Carol","Age":35,"City":"SF"},{"Name":"David","Age":28,"City":"TX"},{"Name":"Eva","Age":32,"City":"FL"}]
```

### 示例 XML（放在单元格 A1 中）

```xml
<employees><employee><name>Alice</name><dept>Sales</dept><salary>50000</salary></employee><employee><name>Bob</name><dept>R&amp;D</dept><salary>75000</salary></employee><employee><name>Carol</name><dept>Support</dept><salary>45000</salary></employee><employee><name>David</name><dept>Engineering</dept><salary>90000</salary></employee><employee><name>Eva</name><dept>HR</dept><salary>60000</salary></employee></employees>
```

---

### JSON.PARSE — 解析 JSON

**语法**：`=JSON.PARSE(json_text)`

**返回**：`object[,]` — 嵌套表/数组结构。

**示例**：
```
=JSON.PARSE(A1)
→ 二维表结构
```

---

### JSON.QUERY — JSON 路径查询

**语法**：`=JSON.QUERY(json_text, json_path)`

点路径如 `"0.Name"`、`"1.Age"`。

**示例**：
```
=JSON.QUERY(A1, "0.Name")     → "Alice"
=JSON.QUERY(A1, "1.Age")      → 25
=JSON.QUERY(A1, "2.City")     → "SF"
```

---

### JSON.VALIDATE — JSON 验证

**语法**：`=JSON.VALIDATE(json_text)`

**示例**：
```
=JSON.VALIDATE("{\"a\":1}")          → TRUE
=JSON.VALIDATE("not valid json")     → FALSE
```

---

### JSON.PRETTIFY — JSON 美化

**语法**：`=JSON.PRETTIFY(json_text)`

返回带缩进的格式化 JSON 字符串。支持数组。

**示例**：
```
=JSON.PRETTIFY("{\"a\":1,\"b\":2}")
→ 格式化的多行 JSON
```

---

### JSON.TOTABLE — JSON 转二维表

**语法**：`=JSON.TOTABLE(json_text)`

JSON 对象数组 → 含表头的二维表。

**示例**：
```
=JSON.TOTABLE(A1)
```

| Name | Age | City |
|------|-----|------|
| Alice | 30 | NYC |
| Bob | 25 | LA |
| Carol | 35 | SF |
| David | 28 | TX |
| Eva | 32 | FL |

---

### XML.XPATH — XPath 查询

**语法**：`=XML.XPATH(xml_text, xpath_text)`

对标 Excel FILTERXML。

**返回**：`string[]` — 匹配元素的值。

**示例**：
```
=XML.XPATH(A1, "//name")                → {"Alice","Bob","Carol","David","Eva"}
=XML.XPATH(A1, "//salary")              → {"50000","75000","45000","90000","60000"}
=XML.XPATH(A1, "//employee[1]/dept")    → {"Sales"}
```

---

### XML.VALIDATE — XML 验证

**语法**：`=XML.VALIDATE(xml_text)`

**示例**：
```
=XML.VALIDATE("<root><a/></root>")     → TRUE
=XML.VALIDATE("<root><a></b>")         → FALSE
```

---

### XML.TOTABLE — XML 转二维表

**语法**：`=XML.TOTABLE(xml_text, row_xpath)`

row_xpath 定义行节点。

**示例**：
```
=XML.TOTABLE(A1, "//employee")
```

| name | dept | salary |
|------|------|--------|
| Alice | Sales | 50000 |
| Bob | R&D | 75000 |
| Carol | Support | 45000 |
| David | Engineering | 90000 |
| Eva | HR | 60000 |

---

## 11. PIVOT — 数据透视

### 示例数据

|   | A (Product) | B (Region) | C (Qty) | D (Revenue) |
|---|---|---|---|---|
| **1** | Alpha | North | 10 | 500 |
| **2** | Beta | South | 20 | 800 |
| **3** | Alpha | South | 15 | 600 |
| **4** | Gamma | North | 12 | 360 |
| **5** | Beta | North | 18 | 720 |
| **6** | Alpha | North | 22 | 880 |

---

> **函数索引**：PIVOT · UNPIVOT · GROUPBY · CROSSJOIN

### PIVOT.PIVOT — 创建透视表

**语法**：`=PIVOT.PIVOT(source_range, row_field, col_field, value_field, [aggregation])`

aggregation: `"SUM"`（默认）/ `"AVG"` / `"COUNT"` / `"MIN"` / `"MAX"`。

**示例**：
```
=PIVOT.PIVOT(A1:D6, 0, 1, 3, "SUM")
```
结果（Product × Region，Revenue 求和）：

| (Row) | North | South |
|-------|-------|-------|
| Alpha | 1380 | 600 |
| Beta | 720 | 800 |
| Gamma | 360 | |

---

### PIVOT.UNPIVOT — 逆透视

**语法**：`=PIVOT.UNPIVOT(source_range, id_fields, value_fields)`

将宽列转为键值行。

**示例**（将季度列转为行）：

输入宽表（Product, Q1, Q2, Q3）：
```
=PIVOT.UNPIVOT(A1:D4, {0}, {1,2,3})
```
输出长表（Product, Attribute, Value）。

---

### PIVOT.GROUPBY — 分组聚合

**语法**：`=PIVOT.GROUPBY(source_range, group_fields, agg_column, [aggregation])`

**示例**：
```
=PIVOT.GROUPBY(A1:D6, {0}, 3, "SUM")
```

| Alpha | 1980 |
| Beta | 1520 |
| Gamma | 360 |

---

### PIVOT.CROSSJOIN — 交叉连接

**语法**：`=PIVOT.CROSSJOIN(table1, table2)`

笛卡尔积。结果不超过 1,000,000 单元格。

**示例**：
```
=PIVOT.CROSSJOIN(A1:A3, B1:B2)
→ 3×2 = 6 行组合
```

---

## 12. SQL — SQL 查询

> 参数化 INSERT，列名经字母数字消毒。表名固定：单表 = `data`，双表 = `data` + `extra`，三表 = `data` + `b` + `c`。第一行自动识别为表头。请在可信输入上使用。
>
> **函数索引**：QUERY · JOIN · QUERY3

### 示例数据

|   | A (Name) | B (Dept) | C (Salary) | D (City) |
|---|---|---|---|---|
| **1** | Alice | Sales | 50000 | NYC |
| **2** | Bob | R&D | 75000 | LA |
| **3** | Carol | Support | 45000 | SF |
| **4** | David | Engineering | 90000 | TX |
| **5** | Eva | HR | 60000 | FL |

---

### SQL.QUERY — 单表 SQL

**语法**：`=SQL.QUERY(source_range, sql_query)`

**示例 1** — 条件筛选：
```
=SQL.QUERY(A1:D5, "SELECT Name, Salary FROM data WHERE Salary > 50000 ORDER BY Salary DESC")
```

| Name | Salary |
|------|--------|
| David | 90000 |
| Bob | 75000 |
| Eva | 60000 |

**示例 2** — 分组聚合：
```
=SQL.QUERY(A1:D5, "SELECT Dept, AVG(Salary) AS AvgSal FROM data GROUP BY Dept")
```

| Dept | AvgSal |
|------|--------|
| Sales | 50000 |
| R&D | 75000 |
| Support | 45000 |
| Engineering | 90000 |
| HR | 60000 |

---

### SQL.JOIN — 双表 SQL

**语法**：`=SQL.JOIN(source_range, join_table, sql_query)`

第二个表名 = `extra`。

**示例**（表2：Dept, Budget）：

| Dept | Budget |
|------|--------|
| Sales | 200000 |
| R&D | 500000 |

```
=SQL.JOIN(A1:D5, F1:G3, "SELECT data.Name, extra.Budget FROM data JOIN extra ON data.Dept = extra.Dept")
```

---

### SQL.QUERY3 — 三表 SQL

**语法**：`=SQL.QUERY3(table1, table2, table3, sql_query)`

表名：`data`, `b`, `c`。

**示例**：
```
=SQL.QUERY3(A1:D5, F1:G3, I1:J4, "SELECT data.Name, b.Dept, c.Region FROM data JOIN b ON data.Dept=b.Dept JOIN c ON data.City=c.City")
```

---

## 13. FS — 文件系统

> 需宏安全设置允许。除 LS/LSDIR/DRIVES/PWD/TEMP 外均支持数组公式。
>
> **函数索引**：NORM · COMBINE · FNAME · BNAME · EXT · FOLDER · FEXISTS · FSIZE · FDEXISTS · MKDIR · LS · LSDIR · READ · WRITE · APPEND · COPY · MOVE · DELETE · DELDIR · DRIVES · PWD · TEMP

### 示例路径

|   | A |
|---|---|
| **1** | Path |
| **2** | C:\Users\Alice\Documents\report.xlsx |
| **3** | C:\Users\Bob\Downloads\data.csv |
| **4** | C:\Users\Carol\Desktop\notes.txt |
| **5** | C:\Users\David\Projects\src\main.cs |
| **6** | /home/eva/logs/app.log |

---

### FS.NORM — 规范化路径

**语法**：`=FS.NORM(file_path)`

统一斜杠，解析 `.` 和 `..`。

**示例**：
```
=FS.NORM("C:\\Users\\..\\Alice\\Docs")     → "C:\Alice\Docs"
=FS.NORM("C:\\a\\b\\..\\c\\.\\d.txt")      → "C:\a\c\d.txt"
```

---

### FS.COMBINE — 拼接路径

**语法**：`=FS.COMBINE(path1, path2)`

**示例**：
```
=FS.COMBINE("C:\\Users", "Alice")        → "C:\Users\Alice"
=FS.COMBINE("/home/eva", "logs/app")     → "/home/eva/logs/app"
```

---

### FS.FNAME — 取文件名（含扩展名）

**语法**：`=FS.FNAME(file_path)`

**示例**：
```
=FS.FNAME("C:\\Users\\Alice\\report.xlsx")    → "report.xlsx"
=FS.FNAME("/home/eva/logs/app.log")           → "app.log"
```

---

### FS.BNAME — 取文件名（不含扩展名）

**语法**：`=FS.BNAME(file_path)`

**示例**：
```
=FS.BNAME("C:\\Users\\Alice\\report.xlsx")    → "report"
=FS.BNAME("Makefile")                         → "Makefile"
```

---

### FS.EXT — 取扩展名

**语法**：`=FS.EXT(file_path)`

含点号。

**示例**：
```
=FS.EXT("report.xlsx")     → ".xlsx"
=FS.EXT("notes.txt")       → ".txt"
=FS.EXT("Makefile")        → ""
```

---

### FS.FOLDER — 取父目录

**语法**：`=FS.FOLDER(file_path)`

**示例**：
```
=FS.FOLDER("C:\\Users\\Alice\\report.xlsx")    → "C:\Users\Alice"
=FS.FOLDER("/home/eva/logs/app.log")           → "/home/eva/logs"
```

---

### FS.FEXISTS — 文件是否存在

**语法**：`=FS.FEXISTS(file_path)`

**示例**：
```
=FS.FEXISTS("C:\\Windows\\System32\\notepad.exe")    → TRUE（通常）
=FS.FEXISTS("C:\\nonexistent\\file.txt")             → FALSE
```

---

### FS.FSIZE — 文件大小

**语法**：`=FS.FSIZE(file_path)`

**返回**：`long` — 字节数。文件不存在返回 `#VALUE!`。

---

### FS.FDEXISTS — 文件夹是否存在

**语法**：`=FS.FDEXISTS(file_path)`

**示例**：
```
=FS.FDEXISTS("C:\\Users")     → TRUE
=FS.FDEXISTS("Z:\\Missing")   → FALSE
```

---

### FS.MKDIR — 创建文件夹

**语法**：`=FS.MKDIR(file_path)`

含父目录。成功/已存在返回 TRUE。

---

### FS.LS — 列出文件

**语法**：`=FS.LS(file_path, [search_pattern])`

**返回**：`string[]`。

**示例**：
```
=FS.LS("C:\\Users", "*.txt")
→ {"notes.txt", "todo.txt", ...}
```

---

### FS.LSDIR — 列出子文件夹

**语法**：`=FS.LSDIR(file_path, [search_pattern])`

**返回**：`string[]`。

---

### FS.READ — 读取文本文件

**语法**：`=FS.READ(file_path)`

**返回**：`string` — 文件全部内容。

---

### FS.WRITE — 写入文本文件

**语法**：`=FS.WRITE(file_path, file_content)`

**返回**：`bool`。覆盖写入。

---

### FS.APPEND — 追加写入

**语法**：`=FS.APPEND(file_path, file_content)`

**返回**：`bool`。

---

### FS.COPY — 复制文件

**语法**：`=FS.COPY(source_path, destination_path)`

**返回**：`bool`。

---

### FS.MOVE — 移动/重命名文件

**语法**：`=FS.MOVE(source_path, destination_path)`

**返回**：`bool`。

---

### FS.DELETE — 删除文件

**语法**：`=FS.DELETE(file_path)`

**返回**：`bool`。永久删除。

---

### FS.DELDIR — 删除文件夹

**语法**：`=FS.DELDIR(file_path)`

**返回**：`bool`。删除文件夹及全部内容。

---

### FS.DRIVES — 列出驱动器

**语法**：`=FS.DRIVES()`

**返回**：`string[]` — 逻辑驱动器字母列表。

---

### FS.PWD — 当前工作目录

**语法**：`=FS.PWD()`

**返回**：`string`。

---

### FS.TEMP — 临时文件夹路径

**语法**：`=FS.TEMP()`

**返回**：`string`。

---

## 14. RANGE — 范围导出

### 示例数据

|   | A (Name) | B (Age) | C (City) | D (Score) |
|---|---|---|---|---|
| **1** | Alice | 30 | NYC | 95.5 |
| **2** | Bob | 25 | LA | 88.0 |
| **3** | Carol | 35 | SF | 92.3 |
| **4** | David | 28 | TX | 76.5 |
| **5** | Eva | 32 | FL | 89.0 |

---

> **函数索引**：TOHTML · TOJSON · TOMD · TOCSV · TOCSVTAB · TOCSVSEMI · TRANSPOSE · SELCOLS · SELROWS

### RANGE.TOHTML — 导出 HTML 表格

**语法**：`=RANGE.TOHTML(source_range, has_headers, [css_class])`

**示例**：
```
=RANGE.TOHTML(A1:D5, TRUE, "my-table")
```
返回：
```html
<table class="my-table"><thead><tr><th>Name</th><th>Age</th><th>City</th><th>Score</th></tr></thead><tbody><tr><td>Alice</td><td>30</td><td>NYC</td><td>95.5</td></tr>...</tbody></table>
```

---

### RANGE.TOJSON — 导出 JSON

**语法**：`=RANGE.TOJSON(source_range, has_headers, [pretty_print])`

**示例**：
```
=RANGE.TOJSON(A1:D5, TRUE, FALSE)
→ [{"Name":"Alice","Age":30,"City":"NYC","Score":95.5},{"Name":"Bob","Age":25,...}]

=RANGE.TOJSON(A1:D5, TRUE, TRUE)
→ 带缩进的格式化 JSON
```

---

### RANGE.TOMD — 导出 Markdown 表格

**语法**：`=RANGE.TOMD(source_range, has_headers)`

**示例**：
```
=RANGE.TOMD(A1:D5, TRUE)
```
返回：
```markdown
| Name | Age | City | Score |
|------|-----|------|-------|
| Alice | 30 | NYC | 95.5 |
| Bob | 25 | LA | 88.0 |
| Carol | 35 | SF | 92.3 |
| David | 28 | TX | 76.5 |
| Eva | 32 | FL | 89.0 |
```

---

### RANGE.TOCSV — 导出 CSV

**语法**：`=RANGE.TOCSV(source_range, [delimiter], [quote_fields])`

**示例**：
```
=RANGE.TOCSV(A1:D5, ",", FALSE)
→ Name,Age,City,Score\r\nAlice,30,NYC,95.5\r\n...

=RANGE.TOCSV(A1:D5, "|", TRUE)
→ "Name"|"Age"|"City"|"Score"\r\n"Alice"|"30"|...
```

---

### RANGE.TOCSVTAB — 导出 TSV

**语法**：`=RANGE.TOCSVTAB(source_range)`

等同于 `RANGE.TOCSV(source_range, "\t", FALSE)`。

---

### RANGE.TOCSVSEMI — 导出分号 CSV

**语法**：`=RANGE.TOCSVSEMI(source_range)`

等同于 `RANGE.TOCSV(source_range, ";", TRUE)`。

---

### RANGE.TRANSPOSE — 行列转置

**语法**：`=RANGE.TRANSPOSE(source_range)`

对标 Excel TRANSPOSE。

**示例**：
```
=RANGE.TRANSPOSE(A1:D3)
```
输入（3行×4列）→ 输出（4行×3列）。

---

### RANGE.SELCOLS — 选取列

**语法**：`=RANGE.SELCOLS(source_range, column_indices)`

对标 Excel CHOOSECOLS。0-based 列索引。

**示例**：
```
=RANGE.SELCOLS(A1:D5, {0, 2})
```

| Name | City |
|------|------|
| Alice | NYC |
| Bob | LA |
| Carol | SF |
| David | TX |
| Eva | FL |

---

### RANGE.SELROWS — 选取行

**语法**：`=RANGE.SELROWS(source_range, row_indices)`

对标 Excel CHOOSEROWS。0-based 行索引。

**示例**：
```
=RANGE.SELROWS(A1:D5, {1, 3})
```

| Alice | 30 | NYC | 95.5 |
| Carol | 35 | SF | 92.3 |

---

## 15. 错误参考

> 完整错误条件与影响范围见 [API 参考 → 错误参考](api-reference.md#错误参考)。`#VALUE!` = 输入/执行错误，`#NUM!` = 计算结果无定义。

---

## 附录：与 Excel 内置函数对照

| 本库函数 | Excel 内置函数 |
|----------|---------------|
| `STATS.ABS` | `ABS` |
| `STATS.COUNT` | `COUNT` |
| `STATS.COVAR` | `COVARIANCE.S` |
| `STATS.COVARP` | `COVARIANCE.P` |
| `STATS.EXP` | `EXP` |
| `STATS.LN` | `LN` |
| `STATS.LOG10` | `LOG10` |
| `STATS.MEAN` | `AVERAGE` |
| `STATS.MODE` | `MODE.SNGL` |
| `STATS.PEARSON` | `PEARSON` |
| `STATS.PERCENTILE` | `PERCENTILE.EXC` |
| `STATS.SIGN` | `SIGN` |
| `STATS.SQRT` | `SQRT` |
| `STATS.STDEV` | `STDEV.S` |
| `STATS.STDEVP` | `STDEV.P` |
| `STATS.VAR` | `VAR.S` |
| `STATS.VARP` | `VAR.P` |
| `LINALG.DET` | `MDETERM` |
| `LINALG.MATMUL` | `MMULT` |
| `LINALG.TRANSPOSE` | `TRANSPOSE` |
| `REGRESS.OLS` | `LINEST` |
| `DT.ADDWKD` | `WORKDAY` |
| `DT.DATEDIFF` | `DATEDIF` |
| `DT.EOM` | `EOMONTH` |
| `DT.ISOWEEK` | `ISOWEEKNUM` |
| `DT.WKDBTWN` | `NETWORKDAYS` |
| `STR.FORMAT` | `TEXT` |
| `STR.TEXTJOIN` | `TEXTJOIN` |
| `STR.URLENCODE` | `ENCODEURL` |
| `ARR.FILTER` | `FILTER` |
| `ARR.FLATTEN` | `TOROW` |
| `ARR.INDEXOF` | `MATCH` |
| `ARR.RANGE` | `SEQUENCE` |
| `ARR.SORT` | `SORT` |
| `ARR.UNIQUE` | `UNIQUE` |
| `RANGE.SELCOLS` | `CHOOSECOLS` |
| `RANGE.SELROWS` | `CHOOSEROWS` |
| `RANGE.TRANSPOSE` | `TRANSPOSE` |
| `XML.XPATH` | `FILTERXML` |

---

*本文档所有示例已通过 Python 3.12 + NumPy + SciPy 交叉验证。详细验证脚本见 `scripts/verify-manual.py`。*

---

## 参见

- [API 参考](api-reference.md) — 219 UDF 完整签名与错误参考
- [README](../README.md) — 安装、模块速览、安全说明
- [CLAUDE.md](../CLAUDE.md) — 项目架构与开发流程
- [CONTEXT.md](../CONTEXT.md) — 领域术语表
