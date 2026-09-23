# LINALG — 线性代数

> 基于 MathNet.Numerics 5.0.0。所有矩阵函数接受二维区域输入。
>
> **函数索引**：[DET](#linalg-det) · [SOLVE](#linalg-solve) · [MATMUL](#linalg-matmul) · [TRANSPOSE](#linalg-transpose) · [TRACE](#linalg-trace) · [RANK](#linalg-rank) · [COND](#linalg-cond) · [EIGEN](#linalg-eigen) · [SVD_U](#linalg-svd-u) · [SVD_S](#linalg-svd-s) · [SVD_VT](#linalg-svd-vt) · [QR_Q](#linalg-qr-q) · [QR_R](#linalg-qr-r) · [LU_L](#linalg-lu-l) · [LU_U](#linalg-lu-u) · [LU_P](#linalg-lu-p) · [PINV](#linalg-pinv) · [CHOLESKY](#linalg-cholesky) · [IDENTITY](#linalg-identity)

### 示例矩阵 A（4×4）

|   | A | B | C | D |
|---|---|---|---|---|
| **1** | 4 | 1 | 2 | 3 |
| **2** | 3 | 5 | 1 | 2 |
| **3** | 2 | 3 | 6 | 1 |
| **4** | 1 | 2 | 3 | 7 |

---

<a id="linalg-det"></a>

### LINALG.DET — 行列式

**语法**：`=LINALG.DET(array)`

对标 Excel MDETERM。

**示例**：
```
=LINALG.DET(A1:D4)    → 588
```

---

<a id="linalg-solve"></a>

### LINALG.SOLVE — 解线性方程组 Ax = b

**语法**：`=LINALG.SOLVE(array1, array2)`

**返回**：`double[]` — 解向量 x。

**错误行为**：A 奇异或病态（条件数 > 1e14）→ `#VALUE!`（奇异系统可用 `LINALG.PINV` 求最小范数解）。

**示例**（A 同上，b = {10; 12; 14; 16}）：
```
=LINALG.SOLVE(A1:D4, {10;12;14;16})
→ {0.571, 1.286, 1.286, 1.286}   (近似)
```

---

<a id="linalg-matmul"></a>

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

<a id="linalg-transpose"></a>

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

<a id="linalg-trace"></a>

### LINALG.TRACE — 矩阵迹

**语法**：`=LINALG.TRACE(array)`

对角线元素之和。

**示例**：
```
=LINALG.TRACE(A1:D4)    → 22  (4+5+6+7)
```

---

<a id="linalg-rank"></a>

### LINALG.RANK — 数值秩

**语法**：`=LINALG.RANK(array, [tolerance])`

默认容差：省略或传 0 时使用相对判据 `max(m,n)·1e-16·σ_max`（numpy/MATLAB 约定）；也可显式传绝对阈值。

> 已知限制：极小量纲（denormal，如 ~1e-310 级奇异值）在 MathNet SVD 下可能下溢，
> 使秩估计偏低（与 numpy 偶有 1 的差异）。此类输入建议显式传入合适的绝对容差。

**示例**：
```
=LINALG.RANK(A1:D4)           → 4   (满秩)
=LINALG.RANK(A1:D4, 0.01)     → 4
```

---

<a id="linalg-cond"></a>

### LINALG.COND — 条件数

**语法**：`=LINALG.COND(array)`

2-范数条件数。奇异矩阵返回 `NaN`（条件数不可表示）。

**示例**：
```
=LINALG.COND(A1:D4)    → ≈4.39
```

---

<a id="linalg-eigen"></a>

### LINALG.EIGEN — 特征值

**语法**：`=LINALG.EIGEN(array)`

要求对称矩阵，非对称返回 `#VALUE!`。

**示例**（2×2 对称矩阵）：

| 2 | 1 |
| 1 | 2 |

```
=LINALG.EIGEN(A1:B2)
→ {1, 3}
```

---

<a id="linalg-svd-u"></a> <a id="linalg-svd-s"></a> <a id="linalg-svd-vt"></a>

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
→ {9.508, 0.773}   (近似)
```

---

<a id="linalg-qr-q"></a> <a id="linalg-qr-r"></a>

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
→
| -14 | -21 | 14 |
| 0 | -175 | 70 |
| 0 | 0 | 35 |

=LINALG.QR_Q(A1:C3)
→
| -0.86 | 0.39 | -0.33 |
| -0.43 | -0.90 | 0.03 |
| 0.29 | -0.17 | -0.94 |（Q·R = A 成立）
```

---

<a id="linalg-lu-l"></a> <a id="linalg-lu-u"></a> <a id="linalg-lu-p"></a>

### LINALG.LU_L / LU_U / LU_P — LU 分解

**语法**：
- `=LINALG.LU_L(array)` — 下三角矩阵 L（单位对角线）
- `=LINALG.LU_U(array)` — 上三角矩阵 U
- `=LINALG.LU_P(array)` — 置换矩阵 P

满足 P·A = L·U。

**示例**：
```
=LINALG.LU_U(A1:D4)
→
| 4.00 | 1.00 | 2.00 | 3.00 |
| 0 | 4.25 | -0.50 | -0.25 |
| 0 | 0 | 5.29 | -0.35 |
| 0 | 0 | 0 | 6.53 |
```

---

<a id="linalg-pinv"></a>

### LINALG.PINV — Moore-Penrose 伪逆

**语法**：`=LINALG.PINV(array)`

**返回**：`double[,]` — 伪逆矩阵。

**示例**（3×2 矩阵）：
```
=LINALG.PINV(A1:B3)
→
| -0.94 | -0.11 | 0.72 |
| 0.44 | 0.11 | -0.22 |
```

---

<a id="linalg-cholesky"></a>

### LINALG.CHOLESKY — Cholesky 分解

**语法**：`=LINALG.CHOLESKY(array)`

要求对称正定矩阵；非对称输入 → `#VALUE!`。返回下三角 L，满足 A = L·Lᵗ。

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

<a id="linalg-identity"></a>

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

### LINALG — 结果解读指南

#### COND — 条件数

`=LINALG.COND(matrix)` 返回矩阵的 2-范数条件数，衡量矩阵求解时对误差的放大程度。

| 条件数 | 等级 | 含义 |
|--------|------|------|
| < 10 | ✅ 良好 | 数值稳定，精度可靠 |
| 10–1000 | ⚠️ 中等 | 轻微放大误差，结果可参考 |
| > 1000 | ❌ 病态 | 输入误差可能被放大 1000+ 倍，结果不可靠 |

> 条件数大 → 矩阵接近奇异 → 求解不可靠。此时可考虑 Ridge 回归（共线性场景）或数据标准化。奇异矩阵的条件数不可表示 → 返回 `NaN`。

#### RANK — 数值秩

`=LINALG.RANK(matrix, [tolerance])` 中 tolerance 决定多大以下的奇异值视为零。

- 默认 `tolerance = 1e-10`，适合大多数场景。
- 增大 tolerance → 更少奇异值被视为非零 → 秩变小。
- 数据含噪声时适当增大（如 1e-6）。

#### SVD — 奇异值分解

`A = U · diag(S) · Vt`：

| 函数 | 返回 | 含义 | 应用 |
|------|------|------|------|
| `SVD_U` | 左奇异向量矩阵 | 列 × 列正交 | PCA（主成分方向）、降维 |
| `SVD_S` | 奇异值向量（降序） | 各成分的"能量"大小 | 判断取多少成分（看奇异值衰减） |
| `SVD_VT` | 右奇异向量转置 | 行 × 行正交 | 特征向量、变量关系 |

**PCA 思路**：对中心化后的数据矩阵 X 做 `SVD_S(X)`，取前 k 个奇异值对应的右奇异向量作为主成分。

#### 其他分解函数速览

| 分解 | 函数 | 公式 | 用途 |
|------|------|------|------|
| QR | `QR_Q` + `QR_R` | A = Q·R | 最小二乘求解、特征值计算 |
| LU | `LU_L` + `LU_U` + `LU_P` | A = P·L·U | 行列式计算、线性方程组 |
| Cholesky | `CHOLESKY` | A = L·Lᵀ | 正定矩阵分解、蒙特卡洛采样 |
| 伪逆 | `PINV` | A⁺ | 欠定/超定最小二乘解 |

#### 矩阵维度要求速查

| 函数 | 要求 |
|------|------|
| DET | 方阵 |
| SOLVE | 方阵 A + 向量 b（可逆；奇异/病态 cond>1e14 → `#VALUE!`） |
| EIGEN | 对称方阵（非对称 → `#VALUE!`） |
| CHOLESKY | 对称正定方阵（非对称 → `#VALUE!`） |
| QR | m ≥ n（宽矩阵 > 2000 列 → `#VALUE!`） |
| MATMUL | A(m×k) · B(k×n) |

---
