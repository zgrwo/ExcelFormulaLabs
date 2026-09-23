# STR — 字符串处理

> 除 TEXTJOIN/UUID/RND* 外均支持数组公式（逐元素处理）。
>
> **函数索引**：[REVERSE](#str-reverse) · [NORMWS](#str-normws) · [TITLE](#str-title) · [REMOVE](#str-remove) · [KEEP](#str-keep) · [PADLEFT](#str-padleft) · [PADRIGHT](#str-padright) · [TRUNCATE](#str-truncate) · [COUNTSUB](#str-countsub) · [STARTSWITH](#str-startswith) · [ENDSWITH](#str-endswith) · [LEFTOF](#str-leftof) · [RIGHTOF](#str-rightof) · [EXTRACT](#str-extract) · [NTHWORD](#str-nthword) · [COMMONPFX](#str-commonpfx) · [TEXTJOIN](#str-textjoin) · [LEVENSHTEIN](#str-levenshtein) · [SOUNDEX](#str-soundex) · [URLENCODE](#str-urlencode) · [URLDECODE](#str-urldecode) · [HTMLENCODE](#str-htmlencode) · [HTMLDECODE](#str-htmldecode) · [BASE64ENC](#str-base64enc) · [BASE64DEC](#str-base64dec) · [UUID](#str-uuid) · [RNDSTR](#str-rndstr) · [RNDALPHA](#str-rndalpha) · [RNDNUM](#str-rndnum) · [ISNULLEMPTY](#str-isnullempty) · [ISNULLWS](#str-isnullws) · [COALESCE](#str-coalesce) · [FORMAT](#str-format) · [STRIPHTML](#str-striphtml)

### 示例文本数据

|   | A | B | C | D |
|---|---|---|---|---|
| **1** | Name | Code | Description | URL |
| **2** | Alice Johnson | AL001 | Sales & Marketing | https://example.com/path |
| **3** | Bob Smith | BR002 | R&D Department | https://test.org/query?id=1 |
| **4** | Carol White | CW003 | Customer Support | https://mysite.net/about us |
| **5** | David Brown | DB004 | Engineering | https://api.io/v1/data.json |
| **6** | Eva Martinez | EM005 | Human Resources | https://portal.com/login?r=1 |

---

<a id="str-reverse"></a>

### STR.REVERSE — 反转字符串

**语法**：`=STR.REVERSE(text)`

**示例**：
```
=STR.REVERSE("hello")      → "olleh"
=STR.REVERSE("Excel")      → "lecxE"
```

---

<a id="str-normws"></a>

### STR.NORMWS — 规范化空白

**语法**：`=STR.NORMWS(text)`

去首尾空格，合并连续空格为单个。

**示例**：
```
=STR.NORMWS("  hello   world  ")    → "hello world"
=STR.NORMWS("a   b    c     d")      → "a b c d"
```

---

<a id="str-title"></a>

### STR.TITLE — 首字母大写

**语法**：`=STR.TITLE(text)`

**示例**：
```
=STR.TITLE("hello world")          → "Hello World"
=STR.TITLE("sales & marketing")    → "Sales & Marketing"
```

---

<a id="str-remove"></a>

### STR.REMOVE — 删除字符

**语法**：`=STR.REMOVE(text, old_text)`

删除 text 中所有出现在 old_text 里的字符。

**示例**：
```
=STR.REMOVE("abc123def456", "0123456789")     → "abcdef"
=STR.REMOVE("hello-world_test", "-_")          → "helloworldtest"
```

---

<a id="str-keep"></a>

### STR.KEEP — 保留字符

**语法**：`=STR.KEEP(text, keep_chars)`

仅保留 text 中出现在 keep_chars 里的字符。

**示例**：
```
=STR.KEEP("abc123def456", "0123456789")        → "123456"
=STR.KEEP("Tel: (555) 123-4567", "0123456789") → "5551234567"
```

---

<a id="str-padleft"></a>

### STR.PADLEFT — 左侧填充

**语法**：`=STR.PADLEFT(text, num_chars, [pad_text])`

**示例**：
```
=STR.PADLEFT("42", 5, "0")     → "00042"
=STR.PADLEFT("ABC", 6, "-")    → "---ABC"
=STR.PADLEFT("X", 4)           → "   X"   (默认空格)
```

---

<a id="str-padright"></a>

### STR.PADRIGHT — 右侧填充

**语法**：`=STR.PADRIGHT(text, num_chars, [pad_text])`

**示例**：
```
=STR.PADRIGHT("42", 5, "0")    → "42000"
=STR.PADRIGHT("ABC", 6, ".")   → "ABC..."
```

---

<a id="str-truncate"></a>

### STR.TRUNCATE — 截断

**语法**：`=STR.TRUNCATE(text, num_chars, [suffix])`

若截短则追加后缀（默认 `"..."`）。

**示例**：
```
=STR.TRUNCATE("Hello World", 8)         → "Hello..."
=STR.TRUNCATE("Hello World", 8, "…")    → "Hello W…"
=STR.TRUNCATE("Short", 10)              → "Short"
```

---

<a id="str-countsub"></a>

### STR.COUNTSUB — 子串计数

**语法**：`=STR.COUNTSUB(text, substring, [match_case])`

**示例**：
```
=STR.COUNTSUB("banana", "na")                 → 2
=STR.COUNTSUB("Banana BANANA", "ba", FALSE)   → 2
=STR.COUNTSUB("Banana BANANA", "ba", TRUE)    → 0
```

---

<a id="str-startswith"></a>

### STR.STARTSWITH — 判断前缀

**语法**：`=STR.STARTSWITH(text, prefix, [match_case])`

**示例**：
```
=STR.STARTSWITH("Hello World", "Hello")        → TRUE
=STR.STARTSWITH("Hello World", "hello")        → FALSE
=STR.STARTSWITH("Hello World", "hello", FALSE) → TRUE
```

---

<a id="str-endswith"></a>

### STR.ENDSWITH — 判断后缀

**语法**：`=STR.ENDSWITH(text, suffix, [match_case])`

**示例**：
```
=STR.ENDSWITH("report.pdf", ".pdf")          → TRUE
=STR.ENDSWITH("report.PDF", ".pdf", FALSE)   → TRUE
```

---

<a id="str-leftof"></a>

### STR.LEFTOF — 分隔符左侧

**语法**：`=STR.LEFTOF(text, delimiter, [instance_num])`

instance_num: 1=第1次（默认），-1=最后一次。

**示例**：
```
=STR.LEFTOF("a,b,c,d", ",")       → "a"
=STR.LEFTOF("a,b,c,d", ",", 2)    → "a,b"
=STR.LEFTOF("a,b,c,d", ",", -1)   → "a,b,c"
```

---

<a id="str-rightof"></a>

### STR.RIGHTOF — 分隔符右侧

**语法**：`=STR.RIGHTOF(text, delimiter, [instance_num])`

**示例**：
```
=STR.RIGHTOF("a,b,c,d", ",")       → "b,c,d"
=STR.RIGHTOF("a,b,c,d", ",", 2)    → "c,d"
=STR.RIGHTOF("a,b,c,d", ",", -1)   → "d"
```

---

<a id="str-extract"></a>

### STR.EXTRACT — 分隔符间提取

**语法**：`=STR.EXTRACT(text, start_delimiter, end_delimiter, [instance_num], [include_delimiters])`

**示例**：
```
=STR.EXTRACT("a[b]c[d]e", "[", "]")          → "b"
=STR.EXTRACT("a[b]c[d]e", "[", "]", 2)       → "d"
=STR.EXTRACT("a[b]c[d]e", "[", "]", -1)      → "d"
=STR.EXTRACT("a[b]c[d]e", "[", "]", 1, TRUE) → "[b]"
```

---

<a id="str-nthword"></a>

### STR.NTHWORD — 第 N 个词

**语法**：`=STR.NTHWORD(text, [instance_num])`

空格分隔，1-based。

**示例**：
```
=STR.NTHWORD("The quick brown fox")      → "The"
=STR.NTHWORD("The quick brown fox", 3)   → "brown"
=STR.NTHWORD("The quick brown fox", -1)  → "fox"
```

---

<a id="str-commonpfx"></a>

### STR.COMMONPFX — 最长公共前缀

**语法**：`=STR.COMMONPFX(text1, text2, [match_case])`

**示例**：
```
=STR.COMMONPFX("hello world", "hello there")   → "hello "
=STR.COMMONPFX("prefix_abc", "prefix_xyz")     → "prefix_"
=STR.COMMONPFX("Hello", "hello", FALSE)        → "hello"
=STR.COMMONPFX("Hello", "hello", TRUE)         → ""
```

---

<a id="str-textjoin"></a>

### STR.TEXTJOIN — 文本连接

**语法**：`=STR.TEXTJOIN(delimiter, ignore_empty, text_array)`

对标 Excel TEXTJOIN。ignore_empty=TRUE 跳过空值。

**示例**：
```
=STR.TEXTJOIN(", ", TRUE, A2:A6)
→ "Alice Johnson, Bob Smith, Carol White, David Brown, Eva Martinez"

=STR.TEXTJOIN("|", FALSE, B2:B6)
→ "AL001|BR002|CW003|DB004|EM005"
```

---

<a id="str-levenshtein"></a>

### STR.LEVENSHTEIN — 编辑距离

**语法**：`=STR.LEVENSHTEIN(text1, text2)`

**示例**：
```
=STR.LEVENSHTEIN("kitten", "sitting")    → 3
=STR.LEVENSHTEIN("book", "back")         → 2
=STR.LEVENSHTEIN("hello", "hello")       → 0
```

---

<a id="str-soundex"></a>

### STR.SOUNDEX — Soundex 编码

**语法**：`=STR.SOUNDEX(text)`

**示例**：
```
=STR.SOUNDEX("Robert")    → "R163"
=STR.SOUNDEX("Rupert")    → "R163"
=STR.SOUNDEX("Smith")     → "S530"
```

---

<a id="str-urlencode"></a>

<a id="str-urldecode"></a>

### STR.URLENCODE / STR.URLDECODE — URL 编解码

**示例**：
```
=STR.URLENCODE("hello world")             → "hello+world"
=STR.URLENCODE("a=1&b=2")                 → "a%3d1%26b%3d2"
=STR.URLDECODE("hello+world")             → "hello world"
```

---

<a id="str-htmlencode"></a>

<a id="str-htmldecode"></a>

### STR.HTMLENCODE / STR.HTMLDECODE — HTML 编解码

**示例**：
```
=STR.HTMLENCODE("<div class='x'>")    → "&lt;div class='x'&gt;"
=STR.HTMLDECODE("&lt;div&gt;")        → "<div>"
=STR.HTMLENCODE("a & b")             → "a &amp; b"
```

---

<a id="str-base64enc"></a>

<a id="str-base64dec"></a>

### STR.BASE64ENC / STR.BASE64DEC — Base64 编解码

**示例**：
```
=STR.BASE64ENC("Hello World")    → "SGVsbG8gV29ybGQ="
=STR.BASE64DEC("SGVsbG8=")       → "Hello"
```

---

<a id="str-uuid"></a>

### STR.UUID — 生成 UUID

**语法**：`=STR.UUID()`

**示例**：
```
=STR.UUID()    → "a1b2c3d4-e5f6-7890-abcd-ef1234567890"（随机）
```

---

<a id="str-rndstr"></a> <a id="str-rndalpha"></a> <a id="str-rndnum"></a>

### STR.RNDSTR / RNDALPHA / RNDNUM — 随机字符串

**语法**：
- `=STR.RNDSTR(num_chars, [character_set])` — 从字符集随机生成
- `=STR.RNDALPHA(num_chars)` — 纯字母（A-Z, a-z）
- `=STR.RNDNUM(num_chars)` — 纯数字（0-9）

**示例**：
```
=STR.RNDSTR(8)               → "aB3xK9mQ"（随机）
=STR.RNDSTR(6, "ABC123")     → "C1A3B2"（随机）
=STR.RNDALPHA(5)             → "HgKpL"（随机）
=STR.RNDNUM(4)               → "7291"（随机）
```

---

<a id="str-isnullempty"></a>

<a id="str-isnullws"></a>

### STR.ISNULLEMPTY / STR.ISNULLWS — 空值检测

**示例**：
```
=STR.ISNULLEMPTY("")       → TRUE
=STR.ISNULLEMPTY("hello")  → FALSE
=STR.ISNULLWS("   ")       → TRUE
=STR.ISNULLWS("hello")     → FALSE
```

---

<a id="str-coalesce"></a>

### STR.COALESCE — 取首个非空值

**语法**：`=STR.COALESCE(value1, value2)`

返回第一个非 null/空值。

**示例**：
```
=STR.COALESCE("", "default")     → "default"
=STR.COALESCE("hello", "bye")    → "hello"
```

---

<a id="str-format"></a>

### STR.FORMAT — 格式化值

**语法**：`=STR.FORMAT(value, format_text)`

按 .NET 格式字符串格式化。对标 Excel TEXT。

**示例**：
```
=STR.FORMAT(1234.567, "0.00")                  → "1234.57"
=STR.FORMAT(DATE(2024,6,15), "yyyy-MM-dd")     → "2024-06-15"
=STR.FORMAT(0.25, "0.00%")                     → "25.00%"
```

---

<a id="str-striphtml"></a>

### STR.STRIPHTML — 去除 HTML 标签

**语法**：`=STR.STRIPHTML(text)`

**示例**：
```
=STR.STRIPHTML("<p>Hello <b>World</b></p>")  → "Hello World"
=STR.STRIPHTML("<a href='x'>link</a>")        → "link"
```

---
