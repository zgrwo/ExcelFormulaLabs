# REGEX — 正则表达式

> .NET 正则引擎。支持数组公式（逐元素），超时 5 秒自动取消。
> `ignore_case` **默认 TRUE（不区分大小写）**；显式传 FALSE 才区分大小写。
>
> **函数索引**：[TEST](#regex-test) · [COUNT](#regex-count) · [MATCH](#regex-match) · [MATCHALL](#regex-matchall) · [REPLACE](#regex-replace) · [SPLIT](#regex-split) · [GROUPS](#regex-groups) · [ESCAPE](#regex-escape) · [ISMATCH](#regex-ismatch)

### 示例文本

|   | A |
|---|---|
| **1** | Text |
| **2** | The quick brown fox jumps over the lazy dog |
| **3** | Contact: alice@example.com or bob@test.org |
| **4** | Phone: (555) 123-4567, Fax: (555) 765-4321 |
| **5** | Prices: $19.99, $299.50, $1,200.00 |
| **6** | Date: 2024-06-15, Time: 14:30:00 |

---

<a id="regex-test"></a>

### REGEX.TEST — 是否匹配正则

**语法**：`=REGEX.TEST(text, pattern, [ignore_case])`

**示例**：
```
=REGEX.TEST("hello123", "\d+")            → TRUE
=REGEX.TEST("hello", "\d+")               → FALSE
=REGEX.TEST("ABC", "abc", FALSE)          → FALSE
=REGEX.TEST("ABC", "abc", TRUE)           → TRUE
```

---

<a id="regex-count"></a>

### REGEX.COUNT — 非重叠匹配次数

**语法**：`=REGEX.COUNT(text, pattern, [ignore_case])`

**示例**：
```
=REGEX.COUNT("a1b2c3d4", "\d")                   → 4
=REGEX.COUNT("The cat and the Cat", "cat", TRUE)  → 2
```

---

<a id="regex-match"></a>

### REGEX.MATCH — 第 N 个匹配子串

**语法**：`=REGEX.MATCH(text, pattern, [ignore_case], [instance_num])`

1=第一个（默认），-1=最后一个。

**示例**：
```
=REGEX.MATCH("a1b2c3", "\d+")           → "1"
=REGEX.MATCH("a1b2c3", "\d+", , 2)      → "2"
=REGEX.MATCH("a1b2c3", "\d+", , -1)     → "3"
```

---

<a id="regex-matchall"></a>

### REGEX.MATCHALL — 所有匹配

**语法**：`=REGEX.MATCHALL(text, pattern, [ignore_case])`

**返回**：`string[]`

**示例**：
```
=REGEX.MATCHALL("a1b22c333", "\d+")
→ {"1", "22", "333"}
```

---

<a id="regex-replace"></a>

### REGEX.REPLACE — 正则替换

**语法**：`=REGEX.REPLACE(text, pattern, replacement, [ignore_case], [instance_num])`

0/省略=替换全部（默认），1=第一个，-1=最后一个。替换串按**字面量**插入，不解释 `$1`/`$&` 等组替换模式（所有 instance_num 取值行为一致）。

**示例**：
```
=REGEX.REPLACE("a1b2c3", "\d", "X")           → "aXbXcX"
=REGEX.REPLACE("a1b2c3", "\d", "X", , 1)      → "aXb2c3"
=REGEX.REPLACE("a1b2c3", "\d", "X", , -1)     → "a1b2cX"
```

---

<a id="regex-split"></a>

### REGEX.SPLIT — 正则拆分

**语法**：`=REGEX.SPLIT(text, pattern, [ignore_case], [instance_num])`

0/省略=无限拆分，>0=最多拆 instance_num 次（得 instance_num+1 段）；instance_num > 100,000 饱和到 100,000（不报错）。

**返回**：`string[]`

**示例**：
```
=REGEX.SPLIT("a,b;c|d", "[,;|]")
→ {"a", "b", "c", "d"}

=REGEX.SPLIT("one123two456three", "\d+", , 1)
→ {"one", "two456three"}   (仅拆第1次)
```

---

<a id="regex-groups"></a>

### REGEX.GROUPS — 捕获组

**语法**：`=REGEX.GROUPS(text, pattern, [ignore_case])`

**返回**：`object[2,n]` — row0=组名, row1=值。

**示例**：
```
=REGEX.GROUPS("John Doe, 35", "(\w+)\s(\w+),\s(\d+)")
```
结果（2×4）：

| | 0 | 1 | 2 | 3 |
|---|---|---|---|---|
| Names | 0 | 1 | 2 | 3 |
| Values | John Doe, 35 | John | Doe | 35 |

---

<a id="regex-escape"></a>

### REGEX.ESCAPE — 转义正则特殊字符

**语法**：`=REGEX.ESCAPE(text)`

**示例**：
```
=REGEX.ESCAPE("a.b(c)")      → "a\.b\(c\)"
=REGEX.ESCAPE("$100.00")     → "\$100\.00"
```

---

<a id="regex-ismatch"></a>

### REGEX.ISMATCH — 不区分大小写匹配

**语法**：`=REGEX.ISMATCH(text, pattern)`

等同于 `REGEX.TEST(text, pattern, TRUE)`。

**示例**：
```
=REGEX.ISMATCH("HELLO", "hello")     → TRUE
=REGEX.ISMATCH("world", "hello")     → FALSE
```

---
