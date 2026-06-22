# API 参考

> 214 个 UDF 函数的完整签名。按模块组织。用户使用指南见 [user-guide.md](user-guide.md)。

---

## STATS.* -- 描述统计 (33)

| 函数 | 参数 | 返回 |
|------|------|------|
| `STATS.MEAN` | (data) | `double` |
| `STATS.GEOMEAN` | (data) | `double` |
| `STATS.HARMEAN` | (data) | `double` |
| `STATS.MEDIAN` | (data) | `double` |
| `STATS.VARP` | (data) | `double` |
| `STATS.VAR` | (data) | `double` |
| `STATS.STDEVP` | (data) | `double` |
| `STATS.STDEV` | (data) | `double` |
| `STATS.SKEW` | (data) | `double` |
| `STATS.KURT` | (data) | `double` |
| `STATS.MIN` | (data) | `double` |
| `STATS.MAX` | (data) | `double` |
| `STATS.RANGE` | (data) | `double` |
| `STATS.SUM` | (data) | `double` |
| `STATS.PRODUCT` | (data) | `double` |
| `STATS.PERCENTILE` | (data, p) | `double` |
| `STATS.IQR` | (data) | `double` |
| `STATS.SUMMARY` | (data) | `double[9]` |
| `STATS.COUNT` | (data) | `int` |
| `STATS.MODE` | (data) | `double` |
| `STATS.COVARP` | (a, b) | `double` |
| `STATS.COVAR` | (a, b) | `double` |
| `STATS.PEARSON` | (a, b) | `double` |
| `STATS.SPEARMAN` | (a, b) | `double` |
| `STATS.TTEST1` | (data, mu0?) | `double` |
| `STATS.TTEST2` | (a, b) | `double` |
| `STATS.ZSCORE` | (data) | `double[]` |
| `STATS.ABS` | (x) | `double[]` |
| `STATS.SQRT` | (x) | `double[]` |
| `STATS.LN` | (x) | `double[]` |
| `STATS.LOG10` | (x) | `double[]` |
| `STATS.EXP` | (x) | `double[]` |
| `STATS.SIGN` | (x) | `long[]` |

---

## LINALG.* -- 线性代数 (14)

| 函数 | 参数 | 返回 |
|------|------|------|
| `LINALG.DET` | (matrix) | `double` |
| `LINALG.SOLVE` | (A, b) | `double[]` |
| `LINALG.MATMUL` | (A, B) | `double[,]` |
| `LINALG.TRANSPOSE` | (matrix) | `double[,]` |
| `LINALG.TRACE` | (matrix) | `double` |
| `LINALG.RANK` | (matrix, tol?) | `long` |
| `LINALG.COND` | (matrix) | `double` |
| `LINALG.EIGEN` | (matrix) | `double[]` |
| `LINALG.SVD` | (matrix) | `(U, S, Vt)` |
| `LINALG.QR` | (matrix) | `(Q, R)` |
| `LINALG.LU` | (matrix) | `(L, U, P)` |
| `LINALG.PINV` | (matrix) | `double[,]` |
| `LINALG.CHOLESKY` | (matrix) | `double[,]` |
| `LINALG.IDENTITY` | (n) | `double[,]` |

---

## REGRESS.* -- 回归分析 (7)

| 函数 | 参数 | 返回 |
|------|------|------|
| `REGRESS.OLS` | (X, y) | `object[2,n]` (keys row0, values row1) |
| `REGRESS.WLS` | (X, y, w) | `object[2,n]` (keys row0, values row1) |
| `REGRESS.RIDGE` | (X, y, lambda) | `object[2,n]` (keys row0, values row1) |
| `REGRESS.ANOVA1` | (data) | `object[2,n]` (keys row0, values row1) |
| `REGRESS.FACTORIMP` | (X, y) | `long[]` |
| `REGRESS.COEF` | (X, y) | `double[]` |
| `REGRESS.RSQ` | (X, y) | `double` |

---

## PHYCHEM.* -- 物理化学 (16)

| 函数 | 参数 | 返回 |
|------|------|------|
| `PHYCHEM.MOLWT` | (formula) | `double` |
| `PHYCHEM.TEMP` | (value, from_unit, to_unit) | `double` |
| `PHYCHEM.PRESS` | (value, from_unit, to_unit) | `double` |
| `PHYCHEM.VOL` | (value, from_unit, to_unit) | `double` |
| `PHYCHEM.MASS` | (value, from_unit, to_unit) | `double` |
| `PHYCHEM.C_TO_F` | (celsius) | `double` |
| `PHYCHEM.F_TO_C` | (fahrenheit) | `double` |
| `PHYCHEM.KG_TO_LB` | (kg) | `double` |
| `PHYCHEM.LB_TO_KG` | (lb) | `double` |
| `PHYCHEM.L_TO_GAL` | (liters) | `double` |
| `PHYCHEM.GAL_TO_L` | (gallons) | `double` |
| `PHYCHEM.ATM_TO_PSI` | (atm) | `double` |
| `PHYCHEM.PSI_TO_ATM` | (psi) | `double` |
| `PHYCHEM.IDEALGAS` | (P, V, n, T) | `double` |
| `PHYCHEM.GASSTP` | (volume, temp, pressure) | `double` |
| `PHYCHEM.DENSITY` | (mass, volume) | `double` |

---

## STR.* -- 字符串处理 (34)

| 函数 | 参数 | 返回 |
|------|------|------|
| `STR.REVERSE` | (text) | `string` |
| `STR.NORMWS` | (text) | `string` |
| `STR.TITLE` | (text) | `string` |
| `STR.REMOVE` | (text, chars) | `string` |
| `STR.KEEP` | (text, chars) | `string` |
| `STR.PADLEFT` | (text, length, pad_char) | `string` |
| `STR.PADRIGHT` | (text, length, pad_char) | `string` |
| `STR.TRUNCATE` | (text, length, suffix) | `string` |
| `STR.COUNTSUB` | (text, substring, case_sensitive) | `long` |
| `STR.STARTSWITH` | (text, prefix, case_sensitive) | `bool` |
| `STR.ENDSWITH` | (text, suffix, case_sensitive) | `bool` |
| `STR.LEFTOF` | (text, delimiter, nth) | `string` |
| `STR.RIGHTOF` | (text, delimiter, nth) | `string` |
| `STR.EXTRACT` | (text, left, right, nth, include) | `string` |
| `STR.NTHWORD` | (text, n) | `string` |
| `STR.COMMONPFX` | (a, b, case_sensitive) | `string` |
| `STR.TEXTJOIN` | (delimiter, skip_empty, ...values) | `string` |
| `STR.LEVENSHTEIN` | (a, b) | `long` |
| `STR.SOUNDEX` | (text) | `string` |
| `STR.URLENCODE` | (text) | `string` |
| `STR.URLDECODE` | (text) | `string` |
| `STR.HTMLENCODE` | (text) | `string` |
| `STR.HTMLDECODE` | (text) | `string` |
| `STR.BASE64ENC` | (text) | `string` |
| `STR.BASE64DEC` | (text) | `string` |
| `STR.UUID` | () | `string` |
| `STR.RNDSTR` | (length, charset) | `string` |
| `STR.RNDALPHA` | (length) | `string` |
| `STR.RNDNUM` | (length) | `string` |
| `STR.ISNULLEMPTY` | (text) | `bool` |
| `STR.ISNULLWS` | (text) | `bool` |
| `STR.COALESCE` | (primary, fallback) | `string` |
| `STR.FORMAT` | (value, format) | `string` |
| `STR.STRIPHTML` | (text) | `string` |

---

## DT.* -- 日期时间 (25)

| 函数 | 参数 | 返回 |
|------|------|------|
| `DT.ISOWEEK` | (date) | `long` |
| `DT.WEEKDAY` | (date) | `long` |
| `DT.WEEKDAYISO` | (date) | `long` |
| `DT.WEEKDAYNAME` | (date) | `string` |
| `DT.SOW` | (date, start_day) | `double` (OLE date) |
| `DT.EOW` | (date, start_day) | `double` (OLE date) |
| `DT.SOM` | (date) | `double` (OLE date) |
| `DT.EOM` | (date) | `double` (OLE date) |
| `DT.WOM` | (date, start_day) | `long` |
| `DT.DIM` | (year, month) | `long` |
| `DT.AGEYEARS` | (birth_date, ref_date?) | `long` |
| `DT.AGEMONTHS` | (birth_date, ref_date?) | `long` |
| `DT.AGEDAYS` | (birth_date, ref_date?) | `long` |
| `DT.ISWE` | (date) | `bool` |
| `DT.ADDWKD` | (start_date, days) | `double` (OLE date) |
| `DT.WKDBTWN` | (start_date, end_date) | `long` |
| `DT.NEXTWKD` | (date) | `double` (OLE date) |
| `DT.EASTER` | (year) | `double` (OLE date) |
| `DT.QUARTER` | (date) | `long` |
| `DT.SEMESTER` | (date) | `long` |
| `DT.DOY` | (date) | `long` |
| `DT.ISLEAP` | (year) | `bool` |
| `DT.UNIXTS` | (date) | `double` |
| `DT.FROMUNIX` | (timestamp) | `double` (OLE date) |
| `DT.DATEDIFF` | (unit, date1, date2) | `long` |

---

## REGEX.* -- 正则表达式 (9)

| 函数 | 参数 | 返回 |
|------|------|------|
| `REGEX.TEST` | (input, pattern, ignore_case) | `bool` |
| `REGEX.COUNT` | (input, pattern, ignore_case) | `long` |
| `REGEX.MATCH` | (input, pattern, ignore_case) | `string` |
| `REGEX.MATCHALL` | (input, pattern, ignore_case) | `string[]` |
| `REGEX.REPLACE` | (input, pattern, replacement, ignore_case) | `string` |
| `REGEX.SPLIT` | (input, pattern, ignore_case) | `string[]` |
| `REGEX.GROUPS` | (input, pattern, ignore_case) | `string[]` |
| `REGEX.ESCAPE` | (literal) | `string` |
| `REGEX.ISMATCH` | (input, pattern) | `bool` |

---

## ARR.* -- 数组操作 (22)

| 函数 | 参数 | 返回 |
|------|------|------|
| `ARR.SORT` | (array, asc, mode) | `object[]` |
| `ARR.SORTASC` | (array) | `object[]` |
| `ARR.SORTDESC` | (array) | `object[]` |
| `ARR.SORTNUM` | (array) | `object[]` |
| `ARR.SORTTEXT` | (array) | `object[]` |
| `ARR.UNIQUE` | (array) | `object[]` |
| `ARR.INDEXOF` | (array, value) | `long` |
| `ARR.SLICE` | (array, start, length) | `object[]` |
| `ARR.FLATTEN` | (array_2d) | `object[]` |
| `ARR.FILTER` | (array, criteria, operator) | `object[]` |
| `ARR.FILTER_EQ` | (array, criteria) | `object[]` |
| `ARR.FILTER_NE` | (array, criteria) | `object[]` |
| `ARR.FILTER_GT` | (array, criteria) | `object[]` |
| `ARR.FILTER_LT` | (array, criteria) | `object[]` |
| `ARR.CONCAT` | (array_a, array_b) | `object[]` |
| `ARR.REVERSE` | (array) | `object[]` |
| `ARR.COUNT` | (array) | `long` |
| `ARR.CONTAINS` | (array, value) | `bool` |
| `ARR.TOSET` | (array) | `object[]` |
| `ARR.FILL` | (value, count) | `object[]` |
| `ARR.RANGE` | (start, end, step) | `object[]` |
| `ARR.SHUFFLE` | (array) | `object[]` |

---

## DICT.* -- 字典/集合操作 (8)

| 函数 | 参数 | 返回 |
|------|------|------|
| `DICT.FREQUENCY` | (keys) | `object[,]` (key, count) |
| `DICT.INTERSECT` | (a, b) | `object[]` |
| `DICT.UNION` | (a, b) | `object[]` |
| `DICT.EXCEPT` | (a, b) | `object[]` |
| `DICT.DICT` | (keys, values) | `object[,]` (key, value) |
| `DICT.COUNT` | (dict_2d) | `long` |
| `DICT.KEYS` | (dict_2d) | `object[]` |
| `DICT.VALUES` | (dict_2d) | `object[]` |

---

## JSON.* / XML.* -- JSON/XML 处理 (8)

| 函数 | 参数 | 返回 |
|------|------|------|
| `JSON.PARSE` | (json_string) | `object[,]` |
| `JSON.QUERY` | (json_string, json_path) | `object` |
| `JSON.VALIDATE` | (json_string) | `bool` |
| `JSON.PRETTIFY` | (json_string) | `string` |
| `JSON.TOTABLE` | (json_string) | `object[,]` |
| `XML.XPATH` | (xml_string, xpath) | `object` |
| `XML.VALIDATE` | (xml_string) | `bool` |
| `XML.TOTABLE` | (xml_string, row_path) | `object[,]` |

---

## PIVOT.* -- 数据透视 (4)

| 函数 | 参数 | 返回 |
|------|------|------|
| `PIVOT.PIVOT` | (data_2d, key_col, pivot_col, value_col, agg) | `object[,]` |
| `PIVOT.UNPIVOT` | (data_2d, id_cols, value_cols) | `object[,]` |
| `PIVOT.GROUPBY` | (data_2d, group_cols, agg_col, agg) | `object[,]` |
| `PIVOT.CROSSJOIN` | (table_a, table_b) | `object[,]` |

---

## FS.* -- 文件系统 (22)

| 函数 | 参数 | 返回 |
|------|------|------|
| `FS.NORM` | (path) | `string` |
| `FS.COMBINE` | (path_a, path_b) | `string` |
| `FS.FNAME` | (path) | `string` |
| `FS.BNAME` | (path) | `string` |
| `FS.EXT` | (path) | `string` |
| `FS.FOLDER` | (path) | `string` |
| `FS.FEXISTS` | (path) | `bool` |
| `FS.FSIZE` | (path) | `long` |
| `FS.FDEXISTS` | (path) | `bool` |
| `FS.MKDIR` | (path) | `bool` |
| `FS.LS` | (path, pattern) | `string[]` |
| `FS.LSDIR` | (path, pattern) | `string[]` |
| `FS.READ` | (path) | `string` |
| `FS.WRITE` | (path, content) | `bool` |
| `FS.APPEND` | (path, content) | `bool` |
| `FS.COPY` | (source, dest) | `bool` |
| `FS.MOVE` | (source, dest) | `bool` |
| `FS.DELETE` | (path) | `bool` |
| `FS.DELDIR` | (path) | `bool` |
| `FS.DRIVES` | () | `string[]` |
| `FS.PWD` | () | `string` |
| `FS.TEMP` | () | `string` |

---

## SQL.* -- SQL 查询 (3)

| 函数 | 参数 | 返回 |
|------|------|------|
| `SQL.QUERY` | (data_2d, sql) | `object[,]` |
| `SQL.JOIN` | (data_2d, extra_2d, sql) | `object[,]` |
| `SQL.QUERY3` | (a_2d, b_2d, c_2d, sql) | `object[,]` |

---

## RANGE.* -- 范围导出 (9)

| 函数 | 参数 | 返回 |
|------|------|------|
| `RANGE.TOHTML` | (data_2d, headers, css_class) | `string` |
| `RANGE.TOJSON` | (data_2d, headers, pretty) | `string` |
| `RANGE.TOMD` | (data_2d, headers) | `string` |
| `RANGE.TOCSV` | (data_2d, delimiter, quote) | `string` |
| `RANGE.TOCSVTAB` | (data_2d) | `string` |
| `RANGE.TOCSVSEMI` | (data_2d) | `string` |
| `RANGE.TRANSPOSE` | (data_2d) | `object[,]` |
| `RANGE.SELCOLS` | (data_2d, col_indices) | `object[,]` |
| `RANGE.SELROWS` | (data_2d, row_indices) | `object[,]` |

---

## 架构说明

所有 UDF 遵循统一的调用链：

```
UDF ([ExcelFunction]) → InputNormalizer → MapOver/MapOverFlat/MapOverMulti/V() → Core → OutputWrapper.WrapError → Excel
```

- **MapOver<TIn,TOut>**：保持输入形状，null/error/empty 透传
- **MapOverFlat<TIn,TOut>**：始终返回 `object[]`，即使标量输入
- **MapOverMulti**：多参数广播，尺寸不匹配时返回 `ExcelError`
- **V()**：仅统计类函数使用，尺寸不匹配时返回 NaN（非 ExcelError）

Core 层为纯逻辑 `internal static` 类，100% 单元测试覆盖。所有 Excel 交互（COM Range 检测、类型转换、错误包装）完全隔离在 Foundation 层。
