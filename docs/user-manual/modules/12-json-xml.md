# JSON / XML — 数据处理

> **函数索引**：[JSON.PARSE](#json-parse) · [JSON.QUERY](#json-query) · [JSON.VALIDATE](#json-validate) · [JSON.PRETTIFY](#json-prettify) · [JSON.TOTABLE](#json-totable) · [XML.XPATH](#xml-xpath) · [XML.VALIDATE](#xml-validate) · [XML.TOTABLE](#xml-totable)

### 示例 JSON（放在单元格 A1 中）

```json
[{"Name":"Alice","Age":30,"City":"NYC"},{"Name":"Bob","Age":25,"City":"LA"},{"Name":"Carol","Age":35,"City":"SF"},{"Name":"David","Age":28,"City":"TX"},{"Name":"Eva","Age":32,"City":"FL"}]
```

### 示例 XML（放在单元格 A1 中）

```xml
<employees><employee><name>Alice</name><dept>Sales</dept><salary>50000</salary></employee><employee><name>Bob</name><dept>R&amp;D</dept><salary>75000</salary></employee><employee><name>Carol</name><dept>Support</dept><salary>45000</salary></employee><employee><name>David</name><dept>Engineering</dept><salary>90000</salary></employee><employee><name>Eva</name><dept>HR</dept><salary>60000</salary></employee></employees>
```

---

<a id="json-parse"></a>

### JSON.PARSE — 解析 JSON

**语法**：`=JSON.PARSE(json_text)`

**返回**：`object` — JSON 原生结构：标量直接返回；JSON 数组返回一维数组；JSON 对象/嵌套结构返回 Dictionary/嵌套数组，在 Excel 中渲染为非表格。

**示例**：
```
=JSON.PARSE(A1)     → 标量值或一维标量数组正常返回
=JSON.PARSE(A2)     → 嵌套对象/数组渲染为非表格——转二维表请用 JSON.TOTABLE
```

---

<a id="json-query"></a>

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

<a id="json-validate"></a>

### JSON.VALIDATE — JSON 验证

**语法**：`=JSON.VALIDATE(json_text)`

**示例**：
```
=JSON.VALIDATE("{\"a\":1}")          → TRUE
=JSON.VALIDATE("not valid json")     → FALSE
```

---

<a id="json-prettify"></a>

### JSON.PRETTIFY — JSON 美化

**语法**：`=JSON.PRETTIFY(json_text)`

返回带缩进的格式化 JSON 字符串。支持数组。

**示例**：
```
=JSON.PRETTIFY("{\"a\":1,\"b\":2}")
→ 格式化的多行 JSON
```

---

<a id="json-totable"></a>

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

<a id="xml-xpath"></a>

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

<a id="xml-validate"></a>

### XML.VALIDATE — XML 验证

**语法**：`=XML.VALIDATE(xml_text)`

**示例**：
```
=XML.VALIDATE("<root><a/></root>")     → TRUE
=XML.VALIDATE("<root><a></b>")         → FALSE
```

---

<a id="xml-totable"></a>

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
