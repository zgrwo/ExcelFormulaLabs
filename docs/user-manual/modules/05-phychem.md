# PHYCHEM — 物理化学

> 分子量计算、单位换算、理想气体状态方程。

---

> **函数索引**：[MOLWT](#phychem-molwt) · [TEMP](#phychem-temp) · [PRESS](#phychem-press) · [VOL](#phychem-vol) · [MASS](#phychem-mass) · [C_TO_F](#phychem-c-to-f) · [F_TO_C](#phychem-f-to-c) · [KG_TO_LB](#phychem-kg-to-lb) · [LB_TO_KG](#phychem-lb-to-kg) · [L_TO_GAL](#phychem-l-to-gal) · [GAL_TO_L](#phychem-gal-to-l) · [ATM_TO_PSI](#phychem-atm-to-psi) · [PSI_TO_ATM](#phychem-psi-to-atm) · [IDEALGAS](#phychem-idealgas) · [GASSTP](#phychem-gasstp) · [DENSITY](#phychem-density)

<a id="phychem-molwt"></a>

### PHYCHEM.MOLWT — 分子量

**语法**：`=PHYCHEM.MOLWT(formula_text)`

**返回**：`double` — 分子量（g/mol）。

**示例**：
```
=PHYCHEM.MOLWT("H2SO4")              → 98.078
=PHYCHEM.MOLWT("NaCl")               → 58.443
=PHYCHEM.MOLWT("C6H12O6")           → 180.156
=PHYCHEM.MOLWT("Fe4[Fe(CN)6]3")     → 859.239
=PHYCHEM.MOLWT("CaCO3")             → 100.086
```

---

<a id="phychem-temp"></a>

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

<a id="phychem-press"></a>

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

<a id="phychem-vol"></a>

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

<a id="phychem-mass"></a>

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

<a id="phychem-c-to-f"></a> <a id="phychem-f-to-c"></a>

### PHYCHEM.C_TO_F / F_TO_C — 温度快捷换算

**示例**：
```
=PHYCHEM.C_TO_F(0)     → 32
=PHYCHEM.C_TO_F(100)   → 212
=PHYCHEM.F_TO_C(32)    → 0
=PHYCHEM.F_TO_C(212)   → 100
```

---

<a id="phychem-kg-to-lb"></a> <a id="phychem-lb-to-kg"></a>

### PHYCHEM.KG_TO_LB / LB_TO_KG — 质量快捷换算

**示例**：
```
=PHYCHEM.KG_TO_LB(10)    → 22.046
=PHYCHEM.LB_TO_KG(10)    → 4.536
```

---

<a id="phychem-l-to-gal"></a> <a id="phychem-gal-to-l"></a>

### PHYCHEM.L_TO_GAL / GAL_TO_L — 体积快捷换算

**示例**：
```
=PHYCHEM.L_TO_GAL(10)     → 2.642
=PHYCHEM.GAL_TO_L(10)     → 37.854
```

---

<a id="phychem-atm-to-psi"></a> <a id="phychem-psi-to-atm"></a>

### PHYCHEM.ATM_TO_PSI / PSI_TO_ATM — 压力快捷换算

**示例**：
```
=PHYCHEM.ATM_TO_PSI(2)    → 29.392
=PHYCHEM.PSI_TO_ATM(30)   → 2.041
```

---

<a id="phychem-idealgas"></a>

### PHYCHEM.IDEALGAS — 理想气体状态方程

**语法**：`=PHYCHEM.IDEALGAS(pressure, volume, moles, temperature)`

PV = nRT。将待求量填 `"*"`。R 取精确值 `8.31446261815324 / 101.325` ≈ 0.082057366... L·atm/(mol·K)（非 0.082057 约数）。

**示例**（标准状况下 1 mol 理想气体）：
```
=PHYCHEM.IDEALGAS("*", 22.414, 1, 273.15)    → P ≈ 1.0 atm
=PHYCHEM.IDEALGAS(1, "*", 1, 273.15)          → V ≈ 22.414 L
=PHYCHEM.IDEALGAS(1, 22.414, "*", 273.15)     → n ≈ 1.0 mol
=PHYCHEM.IDEALGAS(1, 22.414, 1, "*")          → T ≈ 273.15 K
```

---

<a id="phychem-gasstp"></a>

### PHYCHEM.GASSTP — 气体体积换算标况

**语法**：`=PHYCHEM.GASSTP(volume, temperature, pressure, [t_unit], [p_unit])`

换算到 STP（273.15K, 1atm）：V_stp = V × P / P_stp × T_stp / T。

| 参数 | 说明 |
|------|------|
| volume | 气体体积 |
| temperature | 温度，默认单位 ℃（摄氏度） |
| pressure | 压力，默认单位 atm |
| [t_unit] | 温度单位：C（摄氏，默认）/ K（开尔文）/ F（华氏度） |
| [p_unit] | 压力单位：atm（默认）/ PSI / KPA / PA / BAR / MMHG / TORR |

**示例**：
```
=PHYCHEM.GASSTP(10, 26.85, 1.5)         → ≈13.65 L   (26.85℃ = 300K)
=PHYCHEM.GASSTP(10, 300, 1.5, "K")      → ≈13.65 L   (直接使用开尔文)
```

---

<a id="phychem-density"></a>

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
