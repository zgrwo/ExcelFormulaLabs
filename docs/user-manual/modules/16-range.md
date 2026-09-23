# RANGE — 范围导出

### 示例数据

|   | A (Name) | B (Age) | C (City) | D (Score) |
|---|---|---|---|---|
| **1** | Alice | 30 | NYC | 95.5 |
| **2** | Bob | 25 | LA | 88.0 |
| **3** | Carol | 35 | SF | 92.3 |
| **4** | David | 28 | TX | 76.5 |
| **5** | Eva | 32 | FL | 89.0 |

---

> **函数索引**：[TOHTML](#range-tohtml) · [TOJSON](#range-tojson) · [TOMD](#range-tomd) · [TOCSV](#range-tocsv) · [TOCSVTAB](#range-tocsvtab) · [TOCSVSEMI](#range-tocsvsemi) · [TRANSPOSE](#range-transpose) · [SELCOLS](#range-selcols) · [SELROWS](#range-selrows)

<a id="range-tohtml"></a>

### RANGE.TOHTML — 导出 HTML 表格

**语法**：`=RANGE.TOHTML(source_range, [has_headers], [css_class])`

**示例**：
```
=RANGE.TOHTML(A1:D5, TRUE, "my-table")
```
返回：
```html
<table class="my-table"><thead><tr><th>Name</th><th>Age</th><th>City</th><th>Score</th></tr></thead><tbody><tr><td>Alice</td><td>30</td><td>NYC</td><td>95.5</td></tr>...</tbody></table>
```

---

<a id="range-tojson"></a>

### RANGE.TOJSON — 导出 JSON

**语法**：`=RANGE.TOJSON(source_range, [has_headers], [pretty_print])`

**示例**：
```
=RANGE.TOJSON(A1:D5, TRUE, FALSE)
→ [{"Name":"Alice","Age":30,"City":"NYC","Score":95.5},{"Name":"Bob","Age":25,...}]

=RANGE.TOJSON(A1:D5, TRUE, TRUE)
→ 带缩进的格式化 JSON
```

---

<a id="range-tomd"></a>

### RANGE.TOMD — 导出 Markdown 表格

**语法**：`=RANGE.TOMD(source_range, [has_headers])`

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

<a id="range-tocsv"></a>

### RANGE.TOCSV — 导出 CSV

**语法**：`=RANGE.TOCSV(source_range, [delimiter], [quote_fields], [has_headers])`

> **参数说明**：`has_headers` 为兼容参数，**无实际效果**——CSV 恒导出全部行（含首行），不像 TOHTML/TOJSON/TOMD 那样解释表头。该参数仅为与其他 RANGE.* 导出签名一致而保留。

**示例**：
```
=RANGE.TOCSV(A1:D5, ",", FALSE)
→ Name,Age,City,Score\r\nAlice,30,NYC,95.5\r\n...

=RANGE.TOCSV(A1:D5, "|", TRUE)
→ "Name"|"Age"|"City"|"Score"\r\n"Alice"|"30"|...
```

---

<a id="range-tocsvtab"></a>

### RANGE.TOCSVTAB — 导出 TSV

**语法**：`=RANGE.TOCSVTAB(source_range)`

等同于 `RANGE.TOCSV(source_range, "\t", FALSE)`。

---

<a id="range-tocsvsemi"></a>

### RANGE.TOCSVSEMI — 导出分号 CSV

**语法**：`=RANGE.TOCSVSEMI(source_range)`

等同于 `RANGE.TOCSV(source_range, ";", TRUE)`。

---

<a id="range-transpose"></a>

### RANGE.TRANSPOSE — 行列转置

**语法**：`=RANGE.TRANSPOSE(source_range)`

对标 Excel TRANSPOSE。

**示例**：
```
=RANGE.TRANSPOSE(A1:D3)
```
输入（3行×4列）→ 输出（4行×3列）。

---

<a id="range-selcols"></a>

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

<a id="range-selrows"></a>

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
