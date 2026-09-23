# PIVOT — 数据透视

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

> **函数索引**：[PIVOT](#pivot-pivot) · [UNPIVOT](#pivot-unpivot) · [GROUPBY](#pivot-groupby) · [CROSSJOIN](#pivot-crossjoin)

<a id="pivot-pivot"></a>

### PIVOT.PIVOT — 创建透视表

**语法**：`=PIVOT.PIVOT(source_range, row_field, col_field, value_field, [aggregation], [has_headers])`

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

<a id="pivot-unpivot"></a>

### PIVOT.UNPIVOT — 逆透视

**语法**：`=PIVOT.UNPIVOT(source_range, id_fields, value_fields, [has_headers])`

将宽列转为键值行。

**示例**（将季度列转为行）：

输入宽表（Product, Q1, Q2, Q3）：
```
=PIVOT.UNPIVOT(A1:D4, {0}, {1,2,3})
```
输出长表（Product, Attribute, Value）。

---

<a id="pivot-groupby"></a>

### PIVOT.GROUPBY — 分组聚合

**语法**：`=PIVOT.GROUPBY(source_range, group_fields, agg_column, [aggregation], [has_headers])`

**示例**：
```
=PIVOT.GROUPBY(A1:D6, {0}, 3, "SUM")
```

| Alpha | 1980 |
| Beta | 1520 |
| Gamma | 360 |

---

<a id="pivot-crossjoin"></a>

### PIVOT.CROSSJOIN — 交叉连接

**语法**：`=PIVOT.CROSSJOIN(table1, table2)`

笛卡尔积。结果不超过 1,000,000 单元格。

**示例**：
```
=PIVOT.CROSSJOIN(A1:A3, B1:B2)
→ 3×2 = 6 行组合
```

---
