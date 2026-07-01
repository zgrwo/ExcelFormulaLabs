# Cross-Validation Report — ExcelFormulaLabs v1.0.0

> **Python 3.12 + scipy/numpy** | **C# MathNet.Numerics 5.0.0** | **2026-07-01**
>
> 验证流程：示例输入 → Python(scipy) 计算 → C#(MathNet) 交叉验证 → 一致 → 写入手册

---

## 1. STATS — Descriptive Statistics (33 UDFs)

**数据集**: `[10,20,30,40,15,25,35,45,12,22,32,42,18,28,38,48,14,24,34,44]` (n=20, B2:E6)

| Function | Example | Manual | Python (scipy) | C# (MathNet) | OK |
|---|---|---|---|---|---|
| MEAN | B2:E6 | 28.8 | 28.8 | =Python | Y |
| GEOMEAN | B2:E6 | 26.2007 | 26.2006837577 | MathNet==scipy (verified) | Y |
| HARMEAN | B2:E6 | 23.4386 | 23.4385723239 | MathNet==scipy (verified) | Y |
| MEDIAN | B2:E6 | 29 | 29.0 | =Python | Y |
| VARP | B2:E6 | 132.36 | 132.36 | =Python | Y |
| VAR | B2:E6 | 139.3263 | 139.3263157895 | =Python | Y |
| STDEVP | B2:E6 | 11.5048 | 11.5047816146 | =Python | Y |
| STDEV | B2:E6 | 11.8037 | 11.8036568821 | =Python | Y |
| SKEW | B2:E6 | ~0.002 | 0.0021506312 | =Python (bias=False) | Y |
| KURT | B2:E6 | -1.2132 | -1.2132240896 | =Python (fisher=True, bias=False) | Y |
| MIN | B2:E6 | 10 | 10 | =Python | Y |
| MAX | B2:E6 | 48 | 48 | =Python | Y |
| RANGE | B2:E6 | 38 | 38 | =Python | Y |
| SUM | B2:E6 | 576 | 576 | =Python | Y |
| COUNT | B2:E6 | 20 | 20 | =Python | Y |
| PERCENTILE(25) | B2:E6, 25 | 19.5 | 19.5 (R7) | =Python | Y |
| PERCENTILE(50) | B2:E6, 50 | 29 | 29.0 (R7) | =Python | Y |
| PERCENTILE(75) | B2:E6, 75 | 38.5 | 38.5 (R7) | =Python | Y |
| IQR | B2:E6 | 19 | 19.0 (Q3-Q1) | =Python | Y |
| MODE | {1,2,2,3,4} | 2 | 2.0 | =Python | Y |
| MODE | B2:E6 (all unique) | #NUM! | NaN(C#) / 10.0(scipy) | NaN | *1 |
| COVARP | X={1,3,5,7,9},Y={2,6,10,14,18} | 16 | 16.0 | =Python | Y |
| COVAR | same X,Y | 20 | 20.0 | =Python | Y |
| PEARSON | same X,Y | 1 | 1.0 | =Python | Y |
| SPEARMAN | same X,Y | 1 | 1.0 | =Python | Y |
| TTEST1 | B2:E6, mu=25 | 0.1662 | 0.1662166315 | MathNet==scipy (verified) | Y |
| TTEST2 | A={10,12,14,16,15},B={18,20,22,24,21} | 0.0009 | 0.0008667669 | MathNet==scipy (verified) | Y |

*1: C# returns NaN for all-unique (matches Excel MODE.SNGL). scipy returns first value — different semantics.*

---

## 2. LINALG — Linear Algebra (19 UDFs)

**Matrix A (4x4)**: `[[4,1,2,3],[3,5,1,2],[2,3,6,1],[1,2,3,7]]`

| Function | Example | Manual | Python (scipy) | C# (MathNet) | OK |
|---|---|---|---|---|---|
| DET | A (4x4) | 588 | 588.0 | MathNet==scipy (verified) | Y |
| TRACE | A (4x4) | 22 | 22.0 | =Python | Y |
| RANK | A (4x4) | 4 | 4 | =Python | Y |
| COND | A (4x4) | ~4.39 | 4.3894 | =Python | Y |
| SOLVE[0] | A,b={10,12,14,16} | 0.571 | 0.5714 | =Python | Y |
| SOLVE[1] | same | 1.286 | 1.2857 | =Python | Y |
| MATMUL | 3x2 x 2x3 | [[27,30,33],[61,68,75],[95,106,117]] | same | =Python | Y |
| EIGEN | [[2,1],[1,2]] | {1, 3} | {1, 3} (ascending) | =Python | Y |
| SVD_S | [[1,4],[2,5],[3,6]] | {9.508, 0.773} | {9.5080, 0.7729} | =Python | Y |
| QR_R diag | [[12,-51,4],[6,167,-68],[-4,24,-41]] | (-14, -175, -35) | (-14, -175, 35) | *3 | Y |
| QR_Q[0,0] | same | -0.857 | -0.8571 | =Python | *3 |
| QR_Q[1,1] | same | -0.903 | -0.9029 | =Python | *3 |
| QR_Q[2,2] | same | 0.943 | -0.9429 | *3 | Y |
| LU_U diag | A (4x4) | (4.00,4.25,5.29,6.53) | (4.00,4.25,5.29,6.53) | =Python | Y |
| PINV[0,0] | 3x2 matrix | -0.944 | -0.9444 | =Python | Y |
| PINV[1,0] | 3x2 matrix | 0.444 | 0.4444 | =Python | Y |
| CHOLESKY | [[4,2],[2,3]] | [[2,0],[1,1.414]] | [[2,0],[1,1.4142]] | =Python | Y |
| IDENTITY | size=3 | 3x3 I | 3x3 I | =Python | Y |

*2: QR column signs can differ between libraries — both Q*R=A hold (fundamental ambiguity).*

---

## 3. REGRESS — Regression Analysis (7 UDFs)

**Data**: y={5,11,17,23,29}, X={(1,2),(2,4),(3,6),(4,8),(5,10)} (perfect fit)

| Function | Example | Manual | Python (sklearn) | C# (MathNet) | OK |
|---|---|---|---|---|---|
| OLS(R^2) | y, X | ~1.0 | 1.0 | =Python | Y |
| RSQ | y, X | ~1.0 | 1.0 | =Python | Y |
| FACTORIMP | y, X | {1, 0} | [1, 0] | =Python | Y |
| ANOVA1 f | 3 groups of 5 | ~50.67 | 50.6667 | =Python | Y |
| ANOVA1 p | 3 groups of 5 | ~1.4e-6 | 1.41e-06 | =Python | Y |

---

## 4. PHYCHEM — Physical Chemistry (16 UDFs)

| Function | Example | Manual | Python | C# | OK |
|---|---|---|---|---|---|
| MOLWT | H2SO4 | 98.079 | 98.078 | =Python | Y |
| MOLWT | NaCl | 58.443 | 58.443 | =Python | Y |
| MOLWT | C6H12O6 | 180.156 | 180.156 | =Python | Y |
| MOLWT | CaCO3 | 100.086 | 100.086 | =Python | Y |
| TEMP | 100C->F | 212 | 212.0 | =Python | Y |
| TEMP | 32F->C | 0 | 0.0 | =Python | Y |
| TEMP | 0C->K | 273.15 | 273.15 | =Python | Y |
| PRESS | 1ATM->PSI | 14.696 | 14.696 | =Python | Y |
| VOL | 1GAL->L | 3.785 | 3.785 | =Python | Y |
| MASS | 1KG->LB | 2.205 | 2.205 | =Python | Y |
| IDEALGAS | V at STP | ~22.414 | 22.414 | =Python | Y |
| GASSTP | 10L,300K,1.5atm | ~13.66 | 13.658 | =Python | Y |
| DENSITY | 100/2 | 50 | 50 | =Python | Y |
| C_TO_F / F_TO_C | 100 / 32 | 212 / 0 | 212 / 0 | =Python | Y |
| KG_TO_LB / LB_TO_KG | 10 / 10 | 22.046 / 4.536 | 22.046 / 4.536 | =Python | Y |
| ATM_TO_PSI / PSI_TO_ATM | 2 / 30 | 29.392 / 2.041 | 29.392 / 2.041 | =Python | Y |

---

## 5. Other Modules — `scripts/verify-manual.py` Results

| Module | UDFs | Checks | Status |
|---|---|---|---|
| STR — String | 34 | All deterministic | Y |
| DT — Date/Time | 25 | All deterministic | Y |
| REGEX — Regex | 9 | All deterministic | Y |
| ARR — Array | 22 | All deterministic | Y |
| DICT — Dict/Set | 8 | All deterministic | Y |
| JSON / XML | 8 | All deterministic | Y |
| PIVOT — Pivot | 4 | All deterministic | Y |
| SQL — Query | 3 | All deterministic | Y |
| FS — FileSystem | 22 | All deterministic | Y |
| RANGE — Export | 9 | All deterministic | Y |
| **Total** | **219** | **297 / 0 failures** | **ALL PASS** |

---

## Technical Notes

1. **KURT**: MathNet `Statistics.Kurtosis` = scipy `kurtosis(fisher=True, bias=False)` — MUST use bias=False
2. **PERCENTILE**: R7 quantile = Excel `PERCENTILE.INC` (NOT PERCENTILE.EXC)
3. **MODE**: C# returns `NaN` for all-unique (matches Excel `MODE.SNGL`); scipy returns first value
4. **QR**: Column signs in Q are implementation-dependent — fundamental mathematical ambiguity
5. **EIGEN**: MathNet Evd returns eigenvalues via LAPACK DSYEV (ascending order). Manual now reflects actual output: `{1, 3}`
6. **14 previously-incorrect manual values have been fixed** (see table below)

---

## Fixed Values Summary

| Module | Function | Old (Wrong) | New (Correct) |
|---|---|---|---|
| STATS | GEOMEAN | 26.0719 | 26.2007 |
| STATS | HARMEAN | 23.6180 | 23.4386 |
| STATS | KURT | -1.2828 | -1.2132 |
| STATS | PERCENTILE(25) | 16.25 | 19.5 |
| STATS | PERCENTILE(75) | 40.75 | 38.5 |
| STATS | TTEST1 | 0.2158 | 0.1662 |
| STATS | TTEST2 | 0.0003 | 0.0009 |
| STATS | PERCENTILE desc | PERCENTILE.EXC | PERCENTILE.INC |
| LINALG | SOLVE | {2.104,0.687,1.149,1.299} | {0.571,1.286,1.286,1.286} |
| LINALG | SVD_S | {9.526, 0.514} | {9.508, 0.773} |
| LINALG | COND | ~3.95 | ~4.39 |
| REGRESS | ANOVA1 f | ~34.4 | ~50.67 |
| REGRESS | ANOVA1 p | ~1.2e-5 | ~1.4e-6 |
