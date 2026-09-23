# SQL — SQL 查询

> 参数化 INSERT，列名经字母数字消毒。表名固定：单表 = `data`，双表 = `data` + `extra`，三表 = `data` + `b` + `c`。第一行自动识别为表头。请在可信输入上使用。
>
> **函数索引**：[QUERY](#sql-query) · [JOIN](#sql-join) · [QUERY3](#sql-query3)

### 示例数据

|   | A (Name) | B (Dept) | C (Salary) | D (City) |
|---|---|---|---|---|
| **1** | Alice | Sales | 50000 | NYC |
| **2** | Bob | R&D | 75000 | LA |
| **3** | Carol | Support | 45000 | SF |
| **4** | David | Engineering | 90000 | TX |
| **5** | Eva | HR | 60000 | FL |

---

<a id="sql-query"></a>

### SQL.QUERY — 单表 SQL

**语法**：`=SQL.QUERY(source_range, sql_query, [has_headers])`

默认 `has_headers=TRUE`：首行为列名；传 `FALSE` 时首行按数据处理，列名自动生成 `Col1..ColN`。
仅允许只读查询（SELECT/WITH，允许前导空白与注释）；INSERT/UPDATE/DELETE/DDL/PRAGMA/ATTACH 等关键字在整条语句中被拒绝（字符串字面量含整词也会被拒，安全取舍）。
源区域中的错误单元格（如 `#DIV/0!`）按空值处理；列类型按全表扫描推断，混合类型列按文本处理。

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

<a id="sql-join"></a>

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

<a id="sql-query3"></a>

### SQL.QUERY3 — 三表 SQL

**语法**：`=SQL.QUERY3(table1, table2, table3, sql_query)`

表名：`data`, `b`, `c`。

**示例**：
```
=SQL.QUERY3(A1:D5, F1:G3, I1:J4, "SELECT data.Name, b.Dept, c.Region FROM data JOIN b ON data.Dept=b.Dept JOIN c ON data.City=c.City")
```

---
