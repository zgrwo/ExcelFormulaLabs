# ARR — 数组操作

> 一维数组操作函数集。
>
> **函数索引**：[SORT](#arr-sort) · [SORTASC](#arr-sortasc) · [SORTDESC](#arr-sortdesc) · [SORTNUM](#arr-sortnum) · [SORTTEXT](#arr-sorttext) · [UNIQUE](#arr-unique) · [INDEXOF](#arr-indexof) · [SLICE](#arr-slice) · [FLATTEN](#arr-flatten) · [FILTER](#arr-filter) · [FILTER_EQ](#arr-filter-eq) · [FILTER_NE](#arr-filter-ne) · [FILTER_GT](#arr-filter-gt) · [FILTER_LT](#arr-filter-lt) · [CONCAT](#arr-concat) · [REVERSE](#arr-reverse) · [COUNT](#arr-count) · [CONTAINS](#arr-contains) · [TOSET](#arr-toset) · [FILL](#arr-fill) · [RANGE](#arr-range) · [SHUFFLE](#arr-shuffle)

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

<a id="arr-sort"></a>

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

<a id="arr-sortasc"></a>

<a id="arr-sortdesc"></a>

### ARR.SORTASC / ARR.SORTDESC — 升序/降序

**示例**：
```
=ARR.SORTASC({5,2,8,1,9})     → {1,2,5,8,9}
=ARR.SORTDESC({5,2,8,1,9})    → {9,8,5,2,1}
```

---

<a id="arr-sortnum"></a>

<a id="arr-sorttext"></a>

### ARR.SORTNUM / ARR.SORTTEXT — 按类型排序

**示例**：
```
=ARR.SORTNUM({"10","2","1","20"})           → {"1","2","10","20"}
=ARR.SORTTEXT({"Banana","apple","Carrot"})   → {"apple","Banana","Carrot"}
```

---

<a id="arr-unique"></a>

<a id="arr-toset"></a>

### ARR.UNIQUE / ARR.TOSET — 去重

**语法**：`=ARR.UNIQUE(array)`

保留首次出现顺序。对标 Excel UNIQUE。

**示例**：
```
=ARR.UNIQUE({1,2,2,3,3,3,4,5,5})    → {1,2,3,4,5}
```

---

<a id="arr-indexof"></a>

### ARR.INDEXOF — 查找索引

**语法**：`=ARR.INDEXOF(array, lookup_value)`

0-based，未找到返回 -1。对标 Excel MATCH。

**示例**：
```
=ARR.INDEXOF(A2:A6, "Carrot")     → 2
=ARR.INDEXOF(A2:A6, "Orange")     → -1
```

---

<a id="arr-slice"></a>

### ARR.SLICE — 切片

**语法**：`=ARR.SLICE(array, start_index, num_elements)`

**示例**：
```
=ARR.SLICE({10,20,30,40,50}, 1, 3)    → {20,30,40}
=ARR.SLICE({10,20,30,40,50}, 2, 2)    → {30,40}
```

---

<a id="arr-flatten"></a>

### ARR.FLATTEN — 展平二维数组

**语法**：`=ARR.FLATTEN(array)`

按行展为一维。对标 Excel TOROW。

**示例**：
```
=ARR.FLATTEN(B2:C3)    → {5.5, 10, 3.2, 20}
```

---

<a id="arr-filter"></a>

### ARR.FILTER — 按条件过滤

**语法**：`=ARR.FILTER(array, criteria, comparison_operator)`

运算符：`"=", "<>", ">", "<", ">=", "<="`。对标 Excel FILTER。

**示例**：
```
=ARR.FILTER(B2:B6, 5, ">")      → {5.5, 8.0}
=ARR.FILTER(B2:B6, 3, "<=")     → {3.2, 2.1}
```

---

<a id="arr-filter-eq"></a> <a id="arr-filter-ne"></a> <a id="arr-filter-gt"></a> <a id="arr-filter-lt"></a>

### ARR.FILTER_EQ / NE / GT / LT — 快捷过滤

**示例**：
```
=ARR.FILTER_EQ(D2:D6, "Fruit")      → {"Fruit","Fruit","Fruit"}
=ARR.FILTER_NE(D2:D6, "Fruit")      → {"Vegetable","Vegetable"}
=ARR.FILTER_GT(B2:B6, 5)            → {5.5, 8.0}
=ARR.FILTER_LT(B2:B6, 3)            → {2.1}
```

---

<a id="arr-concat"></a>

### ARR.CONCAT — 数组拼接

**语法**：`=ARR.CONCAT(array1, array2)`

对标 Excel VSTACK/HSTACK（一维版）。

**示例**：
```
=ARR.CONCAT({1,2,3}, {4,5,6})    → {1,2,3,4,5,6}
```

---

<a id="arr-reverse"></a>

### ARR.REVERSE — 反转顺序

**示例**：
```
=ARR.REVERSE({1,2,3,4,5})    → {5,4,3,2,1}
```

---

<a id="arr-count"></a>

### ARR.COUNT — 元素个数

**示例**：
```
=ARR.COUNT(B2:B6)    → 5
```

---

<a id="arr-contains"></a>

### ARR.CONTAINS — 是否包含

**示例**：
```
=ARR.CONTAINS(A2:A6, "Banana")    → TRUE
=ARR.CONTAINS(A2:A6, "Orange")    → FALSE
```

---

<a id="arr-fill"></a>

### ARR.FILL — 填充数组

**语法**：`=ARR.FILL(value, count)`

**示例**：
```
=ARR.FILL("Hello", 5)    → {"Hello","Hello","Hello","Hello","Hello"}
=ARR.FILL(0, 4)          → {0,0,0,0}
```

---

<a id="arr-range"></a>

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

<a id="arr-shuffle"></a>

### ARR.SHUFFLE — 随机打乱

**语法**：`=ARR.SHUFFLE(array)`

Fisher-Yates 算法。

**示例**：
```
=ARR.SHUFFLE({1,2,3,4,5})    → 如 {3,1,5,2,4}（随机）
```

---
