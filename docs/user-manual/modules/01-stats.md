# STATS — 描述统计

> 对标 Python scipy，精度 1e-10。元素级函数（ABS/SQRT/LN/LOG10/EXP/SIGN）支持数组公式。
>
> **函数索引**：[MEAN](#stats-mean) · [GEOMEAN](#stats-geomean) · [HARMEAN](#stats-harmean) · [MEDIAN](#stats-median) · [VARP](#stats-varp) · [VAR](#stats-var) · [STDEVP](#stats-stdevp) · [STDEV](#stats-stdev) · [SKEW](#stats-skew) · [KURT](#stats-kurt) · [MIN](#stats-min) · [MAX](#stats-max) · [RANGE](#stats-range) · [SUM](#stats-sum) · [PRODUCT](#stats-product) · [PERCENTILE](#stats-percentile) · [IQR](#stats-iqr) · [SUMMARY](#stats-summary) · [COUNT](#stats-count) · [MODE](#stats-mode) · [COVARP](#stats-covarp) · [COVAR](#stats-covar) · [PEARSON](#stats-pearson) · [SPEARMAN](#stats-spearman) · [CORRMATRIX](#stats-corrmatrix) · [TTEST1](#stats-ttest1) · [TTEST2](#stats-ttest2) · [ZSCORE](#stats-zscore) · [ABS](#stats-abs) · [SQRT](#stats-sqrt) · [LN](#stats-ln) · [LOG10](#stats-log10) · [EXP](#stats-exp) · [SIGN](#stats-sign)

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

<a id="stats-mean"></a>

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

<a id="stats-geomean"></a>

### STATS.GEOMEAN — 几何平均值

**语法**：`=STATS.GEOMEAN(number1)`

仅正数数组有效，含负数返回 `#NUM!`。

**示例**：
```
=STATS.GEOMEAN(B2:E6)
→ 26.2007...
```

---

<a id="stats-harmean"></a>

### STATS.HARMEAN — 调和平均值

**语法**：`=STATS.HARMEAN(number1)`

**示例**：
```
=STATS.HARMEAN(B2:E6)
→ 23.4386...
```

---

<a id="stats-median"></a>

### STATS.MEDIAN — 中位数

**语法**：`=STATS.MEDIAN(number1)`

**示例**：
```
=STATS.MEDIAN(B2:E6)
→ 29
```

---

<a id="stats-varp"></a>

### STATS.VARP — 总体方差（除以 n）

**语法**：`=STATS.VARP(number1)`

**示例**：
```
=STATS.VARP(B2:E6)
→ 132.36
```

---

<a id="stats-var"></a>

### STATS.VAR — 样本方差（除以 n-1）

**语法**：`=STATS.VAR(number1)`

**示例**：
```
=STATS.VAR(B2:E6)
→ 139.3263...
```

---

<a id="stats-stdevp"></a>

### STATS.STDEVP — 总体标准差（除以 n）

**语法**：`=STATS.STDEVP(number1)`

**示例**：
```
=STATS.STDEVP(B2:E6)
→ 11.5048...
```

---

<a id="stats-stdev"></a>

### STATS.STDEV — 样本标准差（除以 n-1）

**语法**：`=STATS.STDEV(number1)`

**示例**：
```
=STATS.STDEV(B2:E6)
→ 11.8037...
```

---

<a id="stats-skew"></a>

### STATS.SKEW — 样本偏度

**语法**：`=STATS.SKEW(number1)`

返回经小样本校正的偏度。对称分布 ≈ 0。此数据近似对称。

**示例**：
```
=STATS.SKEW(B2:E6)
→ ≈0（≈0.002）
```

---

<a id="stats-kurt"></a>

### STATS.KURT — 样本超额峰度

**语法**：`=STATS.KURT(number1)`

**示例**：
```
=STATS.KURT(B2:E6)
→ -1.2132...
```

---

<a id="stats-min"></a>

### STATS.MIN — 最小值

**语法**：`=STATS.MIN(number1)`

**示例**：
```
=STATS.MIN(B2:E6)    → 10
```

---

<a id="stats-max"></a>

### STATS.MAX — 最大值

**语法**：`=STATS.MAX(number1)`

**示例**：
```
=STATS.MAX(B2:E6)    → 48
```

---

<a id="stats-range"></a>

### STATS.RANGE — 极差

**语法**：`=STATS.RANGE(number1)`

即 max - min。

**示例**：
```
=STATS.RANGE(B2:E6)    → 38
```

---

<a id="stats-sum"></a>

### STATS.SUM — 求和

**语法**：`=STATS.SUM(number1)`

**示例**：
```
=STATS.SUM(B2:E6)    → 576
```

---

<a id="stats-product"></a>

### STATS.PRODUCT — 求积

**语法**：`=STATS.PRODUCT(number1)`

**示例**：
```
=STATS.PRODUCT({2,3,4,5,6})    → 720
```

---

<a id="stats-percentile"></a>

### STATS.PERCENTILE — 百分位数

**语法**：`=STATS.PERCENTILE(array, k)`

| 参数 | 说明 |
|------|------|
| `array` | 数值区域 |
| `k` | 百分位值，0–100 |

使用 R7 算法（对标 Excel PERCENTILE.INC）。

**示例**：
```
=STATS.PERCENTILE(B2:E6, 25)   → 19.5
=STATS.PERCENTILE(B2:E6, 50)   → 29
=STATS.PERCENTILE(B2:E6, 75)   → 38.5
```

---

<a id="stats-iqr"></a>

### STATS.IQR — 四分位距

**语法**：`=STATS.IQR(number1)`

即 Q3 - Q1（R7 分位数）。

**示例**：
```
=STATS.IQR(B2:E6)    → 19
```

---

<a id="stats-summary"></a>

### STATS.SUMMARY — 描述统计摘要

**语法**：`=STATS.SUMMARY(number1)`

**返回**：`double[9]` — 1×9水平数组：`[n, mean, stdev, min, q1, median, q3, max, iqr]`

**示例**：
```
=STATS.SUMMARY(B2:E6)
→ {20, 28.8, 11.804..., 10, 19.5, 29, 38.5, 48, 19}
```

---

<a id="stats-count"></a>

### STATS.COUNT — 元素个数

**语法**：`=STATS.COUNT(number)`

**示例**：
```
=STATS.COUNT(B2:E6)    → 20
```

---

<a id="stats-mode"></a>

### STATS.MODE — 众数

**语法**：`=STATS.MODE(number)`

出现频率最高的值。全唯一返回 `#NUM!`，平局返回最小值（对标 Excel MODE.SNGL）。

**示例**：
```
=STATS.MODE({1,2,2,3,4})       → 2
=STATS.MODE(B2:E6)             → #NUM!  (20 个值全唯一)
```

---

<a id="stats-covarp"></a>

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

<a id="stats-covar"></a>

### STATS.COVAR — 样本协方差（除以 n-1）

**语法**：`=STATS.COVAR(array1, array2)`

对标 Excel COVARIANCE.S。

**示例**（同上数据）：
```
=STATS.COVAR(A2:A6, B2:B6)    → 20
```

---

<a id="stats-pearson"></a>

### STATS.PEARSON — Pearson 相关系数

**语法**：`=STATS.PEARSON(array1, array2)`

范围 -1~1。对标 Excel PEARSON。

**示例**（同上数据）：
```
=STATS.PEARSON(A2:A6, B2:B6)    → 1  (完全线性相关)
```

---

<a id="stats-spearman"></a>

### STATS.SPEARMAN — Spearman 秩相关系数

**语法**：`=STATS.SPEARMAN(array1, array2)`

**示例**（同上数据）：
```
=STATS.SPEARMAN(A2:A6, B2:B6)    → 1
```

---

<a id="stats-corrmatrix"></a>

### STATS.CORRMATRIX — Pearson 相关矩阵

**语法**：`=STATS.CORRMATRIX(data)`

多列 Pearson 相关系数矩阵。每列为一个变量，返回对称矩阵，对角线为 1.0。常量列（方差为零）返回 NaN。

**示例**：

| A | B | C |
|---|----|----|
| 1 | 2  | 10 |
| 2 | 4  | 5  |
| 3 | 6  | 0  |

```
=STATS.CORRMATRIX(A1:C3)
→ 3×3 对称矩阵：
│  1   1  -1 │
│  1   1  -1 │
│ -1  -1   1 │
```
A 列与 B 列完全正相关 (r=1)，A 列与 C 列完全负相关 (r=-1)。

---

<a id="stats-ttest1"></a>

### STATS.TTEST1 — 单样本双侧 t 检验

**语法**：`=STATS.TTEST1(array, x)`

H₀: mean = x。p < 0.05 = 均值与 x 差异显著。

**示例**：
```
=STATS.TTEST1(B2:E6, 25)
→ 0.1662...  (p > 0.05，均值 28.8 与 25 无显著差异)
```

---

<a id="stats-ttest2"></a>

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
→ 0.0009...  (p < 0.05，两组差异显著)
```

---

<a id="stats-zscore"></a>

### STATS.ZSCORE — Z 值标准化

**语法**：`=STATS.ZSCORE(number1)`

(x - mean) / stdev。

**示例**：
```
=STATS.ZSCORE({10,20,30,40,50})
→ {-1.414, -0.707, 0, 0.707, 1.414}
```

---

<a id="stats-abs"></a>

### STATS.ABS — 逐元素绝对值

**语法**：`=STATS.ABS(number)`

对标 Excel ABS。支持数组。

**示例**：
```
=STATS.ABS({-10,20,-30,40,-50})
→ {10,20,30,40,50}
```

---

<a id="stats-sqrt"></a>

### STATS.SQRT — 逐元素平方根

**语法**：`=STATS.SQRT(number)`

对标 Excel SQRT。负数返回 NaN。支持数组。

**示例**：
```
=STATS.SQRT({4,9,16,25,36})
→ {2,3,4,5,6}
```

---

<a id="stats-ln"></a>

### STATS.LN — 逐元素自然对数

**语法**：`=STATS.LN(number)`

对标 Excel LN。非正数返回 NaN。支持数组。

**示例**：
```
=STATS.LN({1, 2.71828, 7.38906, 20.0855, 54.5982})
→ {0, 1, 2, 3, 4}   (近似)
```

---

<a id="stats-log10"></a>

### STATS.LOG10 — 逐元素常用对数

**语法**：`=STATS.LOG10(number)`

对标 Excel LOG10。非正数返回 NaN。支持数组。

**示例**：
```
=STATS.LOG10({1,10,100,1000,10000})
→ {0,1,2,3,4}
```

---

<a id="stats-exp"></a>

### STATS.EXP — 逐元素指数函数

**语法**：`=STATS.EXP(number)`

eˣ。对标 Excel EXP。支持数组。

**示例**：
```
=STATS.EXP({0,1,2,3,4})
→ {1, 2.718, 7.389, 20.086, 54.598}   (近似)
```

---

<a id="stats-sign"></a>

### STATS.SIGN — 逐元素符号

**语法**：`=STATS.SIGN(number)`

返回 -1, 0 或 1。NaN → 0。对标 Excel SIGN。支持数组。

**示例**：
```
=STATS.SIGN({-10, 0, 30, -0.5, 100})
→ {-1, 0, 1, -1, 1}
```

---

### STATS — 结果解读指南

#### SUMMARY — 描述统计摘要

`=STATS.SUMMARY(number1)` 返回 9 元素水平数组，各索引含义：

| 索引 | 字段 | 含义 |
|------|------|------|
| 1 | `n` | 有效样本量（已排除空值和错误值） |
| 2 | `mean` | 算术平均值 |
| 3 | `stdev` | 样本标准差（除以 n-1） |
| 4 | `min` | 最小值 |
| 5 | `q1` | 第一四分位数（25 分位） |
| 6 | `median` | 中位数（50 分位） |
| 7 | `q3` | 第三四分位数（75 分位） |
| 8 | `max` | 最大值 |
| 9 | `iqr` | 四分位距（Q3 - Q1），用于检测异常值 |

**用 INDEX 提取**：`=INDEX(STATS.SUMMARY(A1:A100), 1, 2)` → 提取 mean。

**异常值检测**：典型方法是计算「栅栏」：下限 = Q1 - 1.5×IQR，上限 = Q3 + 1.5×IQR。超出栅栏的值可能是异常值。

#### ZSCORE — Z 值标准化

`=STATS.ZSCORE(number1)` 将数据转为标准正态分布（均值 0、标准差 1）下的 z 分数。

| z 范围 | 解释 |
|--------|------|
| \|z\| < 1 | 约 68% 的数据落在此范围，正常波动 |
| 1 ≤ \|z\| < 2 | 约 27% 数据，轻微偏离 |
| 2 ≤ \|z\| < 3 | 约 4% 数据，**可能为异常值** |
| \|z\| ≥ 3 | 约 0.3% 数据，**强烈提示异常值** |

> 正态分布下：68% 数据在 ±1σ、95% 在 ±2σ、99.7% 在 ±3σ（68-95-99.7 规则）。

#### MODE — 众数

返回出现频率最高的值。**返回 NaN = 所有值唯一，不存在众数**（对标 Excel `MODE.SNGL`）。多众数时返回最小值。

#### PERCENTILE — 百分位数

`=STATS.PERCENTILE(array, k)` 中 k 为 0~100。使用 R7 插值算法（对标 Excel `PERCENTILE.INC`）。IQR 内部用 `k=25` 和 `k=75` 计算。

#### 相关系数解读

| 相关系数 | 函数 | 适用场景 |
|----------|------|----------|
| Pearson r | `PEARSON` | 线性关系，数据近似正态 |
| Spearman ρ | `SPEARMAN` | 单调关系，不要求正态，对异常值稳健 |

两者范围均为 -1~1：\|r\| > 0.7 为强相关，0.3~0.7 中等，< 0.3 弱相关。

#### T 检验 p 值解读

| 函数 | 原假设 H₀ | p < 0.05 含义 |
|------|-----------|--------------|
| `TTEST1` | 样本均值 = x | 样本均值与 x 差异显著 |
| `TTEST2` | 两组均值相等 | 两组均值差异显著 |

p > 0.05 = 没有充分证据拒绝原假设（不意味着"没有差异"，而是"证据不足"）。

---
