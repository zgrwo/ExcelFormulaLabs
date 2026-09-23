# DICT — 字典/集合

> 频率统计、集合运算、字典构建。
>
> **函数索引**：[FREQUENCY](#dict-frequency) · [INTERSECT](#dict-intersect) · [UNION](#dict-union) · [EXCEPT](#dict-except) · [DICT](#dict-dict) · [COUNT](#dict-count) · [KEYS](#dict-keys) · [VALUES](#dict-values)

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

<a id="dict-frequency"></a>

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

<a id="dict-intersect"></a>

### DICT.INTERSECT — 交集

**语法**：`=DICT.INTERSECT(array1, array2)`

两个数组都有的值。

**示例**：
```
=DICT.INTERSECT({1,2,3,4}, {3,4,5,6})    → {3,4}
```

---

<a id="dict-union"></a>

### DICT.UNION — 并集

**语法**：`=DICT.UNION(array1, array2)`

所有不重复值。

**示例**：
```
=DICT.UNION({1,2,3}, {3,4,5})    → {1,2,3,4,5}
```

---

<a id="dict-except"></a>

### DICT.EXCEPT — 差集

**语法**：`=DICT.EXCEPT(array1, array2)`

在 array1 但不在 array2 的值。

**示例**：
```
=DICT.EXCEPT({1,2,3,4}, {3,4})    → {1,2}
```

---

<a id="dict-dict"></a>

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

<a id="dict-count"></a>

### DICT.COUNT — 字典行数

**语法**：`=DICT.COUNT(dict_table)`

**示例**：
```
=DICT.COUNT(previous_result)    → 3
```

---

<a id="dict-keys"></a>

<a id="dict-values"></a>

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
