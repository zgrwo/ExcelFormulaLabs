# API 参考

> 全部 UDF 函数的完整签名。使用指南见 [README.md](../README.md)，每函数详细示例见 [用户手册](user-manual.md)。

---

## VBA 调用

加载 .xll 后，所有函数均可通过 `Application.Run` 在 VBA 中直接调用，无需引用或声明。

**关键规则**：跳过的可选参数用**逗号占位**。例如 `REGEX.MATCH(text, pattern, [ignore_case], [instance_num])` 跳过第三个参数时需保留逗号：

```vba
Dim result As Variant
result = Application.Run("REGEX.MATCH", "Order #12345 placed on 2024-06-15", "\d+")
' → "12345"（第 1 个匹配）

result = Application.Run("REGEX.MATCH", "Order #12345 placed on 2024-06-15", "\d+", , 2)
' → "2024"（第 2 个匹配，第三个参数忽略大小写用逗号占位跳过）

result = Application.Run("REGEX.MATCH", "Order #12345 placed on 2024-06-15", "\d+", , -1)
' → "15"（最后一个匹配）
```

---

## STATS.* -- 描述统计

> 对标 Python scipy，精度 1e-10。元素级函数（ABS/SQRT/LN/LOG10/EXP/SIGN）支持数组公式。

| 函数 | 参数 | 返回 | 说明 |
|------|------|------|------|
| `STATS.MEAN` | (number1) | `double` | 算术平均值 |
| `STATS.GEOMEAN` | (number1) | `double` | 几何平均值（正数数组） |
| `STATS.HARMEAN` | (number1) | `double` | 调和平均值（正数数组） |
| `STATS.MEDIAN` | (number1) | `double` | 中位数 |
| `STATS.VARP` | (number1) | `double` | 总体方差（除以 n） |
| `STATS.VAR` | (number1) | `double` | 样本方差（除以 n-1） |
| `STATS.STDEVP` | (number1) | `double` | 总体标准差（除以 n） |
| `STATS.STDEV` | (number1) | `double` | 样本标准差（除以 n-1） |
| `STATS.SKEW` | (number1) | `double` | 样本偏度 |
| `STATS.KURT` | (number1) | `double` | 样本超额峰度 |
| `STATS.MIN` | (number1) | `double` | 最小值 |
| `STATS.MAX` | (number1) | `double` | 最大值 |
| `STATS.RANGE` | (number1) | `double` | 极差（max - min） |
| `STATS.SUM` | (number1) | `double` | 求和 |
| `STATS.PRODUCT` | (number1) | `double` | 求积 |
| `STATS.PERCENTILE` | (array, k) | `double` | k 分位数（0-100），R7 算法。对标 Excel PERCENTILE.EXC |
| `STATS.IQR` | (number1) | `double` | 四分位距（Q3 - Q1），R7 算法 |
| `STATS.SUMMARY` | (number1) | `double[9]` | 描述统计摘要：`[n, mean, stdev, min, q1, median, q3, max, iqr]`。R7 分位数（对标 Python scipy）。 |
| `STATS.COUNT` | (number) | `long` | 元素个数 |
| `STATS.MODE` | (number1) | `double` | 众数。全唯一返回 NaN（对标 Excel MODE.SNGL） |
| `STATS.COVARP` | (array1, array2) | `double` | 总体协方差（除以 n）。对标 Excel COVARIANCE.P |
| `STATS.COVAR` | (array1, array2) | `double` | 样本协方差（除以 n-1）。对标 Excel COVARIANCE.S |
| `STATS.PEARSON` | (array1, array2) | `double` | Pearson 线性相关系数 r。范围 -1~1。对标 Excel PEARSON |
| `STATS.SPEARMAN` | (array1, array2) | `double` | Spearman 秩相关系数。范围 -1~1 |
| `STATS.TTEST1` | (array, x) | `double` | 单样本双侧 t 检验 p 值（H₀: mean = x）。`p<0.05` = 均值与 x 差异显著 |
| `STATS.TTEST2` | (array1, array2) | `double` | Welch 双样本双侧 t 检验 p 值（不等方差）。`p<0.05` = 两样本均值差异显著 |
| `STATS.ZSCORE` | (number1) | `double[]` | 标准化 z 值：(x - mean) / stdev |
| `STATS.ABS` | (number) | `double[]` | 逐元素绝对值。对标 Excel ABS |
| `STATS.SQRT` | (number) | `double[]` | 逐元素平方根。对标 Excel SQRT |
| `STATS.LN` | (number) | `double[]` | 逐元素自然对数 ln(x)。对标 Excel LN |
| `STATS.LOG10` | (number) | `double[]` | 逐元素常用对数 log₁₀(x)。对标 Excel LOG10 |
| `STATS.EXP` | (number) | `double[]` | 逐元素指数函数 eˣ。对标 Excel EXP |
| `STATS.SIGN` | (number) | `long[]` | 逐元素符号：-1, 0, 1。对标 Excel SIGN |

---

## LINALG.* -- 线性代数

| 函数 | 参数 | 返回 | 说明 |
|------|------|------|------|
| `LINALG.DET` | (array) | `double` | 矩阵行列式值。对标 Excel MDETERM |
| `LINALG.SOLVE` | (array1, array2) | `double[]` | 解线性方程组 Ax = b |
| `LINALG.MATMUL` | (array1, array2) | `double[,]` | 矩阵乘法。对标 Excel MMULT |
| `LINALG.TRANSPOSE` | (array) | `double[,]` | 矩阵转置。对标 Excel TRANSPOSE |
| `LINALG.TRACE` | (array) | `double` | 矩阵迹（对角线元素之和） |
| `LINALG.RANK` | (array, [tolerance]) | `long` | 数值秩（默认容差 1e-10） |
| `LINALG.COND` | (array) | `double` | 条件数（2-范数） |
| `LINALG.EIGEN` | (array) | `double[]` | 特征值。要求对称矩阵，非对称输入返回错误 |
| `LINALG.SVD_U` | (array) | `double[,]` | SVD 左奇异向量矩阵 U。A = U·diag(S)·Vt |
| `LINALG.SVD_S` | (array) | `double[]` | SVD 奇异值向量 S（降序排列） |
| `LINALG.SVD_VT` | (array) | `double[,]` | SVD 右奇异向量转置 Vt。A = U·diag(S)·Vt |
| `LINALG.QR_Q` | (array) | `double[,]` | QR 分解正交矩阵 Q。A = Q·R |
| `LINALG.QR_R` | (array) | `double[,]` | QR 分解上三角矩阵 R。A = Q·R |
| `LINALG.LU_L` | (array) | `double[,]` | LU 分解下三角矩阵 L。PA = LU |
| `LINALG.LU_U` | (array) | `double[,]` | LU 分解上三角矩阵 U。PA = LU |
| `LINALG.LU_P` | (array) | `double[,]` | LU 分解置换矩阵 P。PA = LU |
| `LINALG.PINV` | (array) | `double[,]` | Moore-Penrose 伪逆 |
| `LINALG.CHOLESKY` | (array) | `double[,]` | Cholesky 分解 |
| `LINALG.IDENTITY` | (size) | `double[,]` | 生成 n×n 单位矩阵 |

---

## REGRESS.* -- 回归分析

> 返回 N×(maxLen+1) 纵向报告表（col0 = 字段名, col1.. = 标量值或数组展开）。统计解读：**p < 0.05 = 显著**，**R² 越接近 1 拟合越好**。

| 函数 | 参数 | 返回 | 说明 |
|------|------|------|------|
| `REGRESS.OLS` | (known_y, known_x) | `object[11,?]` | **普通最小二乘法**。对标 Excel LINEST。返回 11 行报告：`coefficients`(系数)、`sse`(残差平方和)、`r_squared`(R²)、`adj_r_squared`(调整R²)、`residuals`(残差)、`fitted_values`(拟合值)、`standard_errors`(标准误)、`t_stats`(t值)、`p_values`(p值)、`n`(样本量)、`df`(自由度)。数组字段横向展开到多列。`p<0.05` 该系数显著。 |
| `REGRESS.WLS` | (known_y, known_x, weights) | `object[11,?]` | **加权最小二乘法**（异方差数据）。返回同 OLS 的 11 行报告。 |
| `REGRESS.RIDGE` | (known_y, known_x, [lambda]) | `object[8,?]` | **岭回归**（L2 正则化，防过拟合）。λ 默认 1.0。返回 8 行：`coefficients`、`sse`、`r_squared`、`residuals`、`fitted_values`、`lambda`(惩罚参数)、`n`、`df`。**不返回**标准误/t值/p值（正则化下推断无效）。 |
| `REGRESS.ANOVA1` | (input_range) | `object[12,?]` | **单因素方差分析**。数据按列分组（每列一组）。返回 12 行：`ss_between`(组间平方和)、`ss_within`(组内平方和)、`ss_total`、`df_between`、`df_within`、`df_total`、`ms_between`、`ms_within`、`f_stat`(F值)、`p_value`(p值)、`group_means`(各组均值)、`group_counts`(各组样本量)。数组字段横向展开到多列。`p<0.05` = 至少有一组均值显著不同。 |
| `REGRESS.FACTORIMP` | (known_y, known_x) | `long[]` | **因子重要性排名**。按标准化后的 \|t\| 降序排列，返回 0-based 列索引数组。 |
| `REGRESS.COEF` | (known_y, known_x) | `double[]` | OLS 回归系数向量（仅 beta）。 |
| `REGRESS.RSQ` | (known_y, known_x) | `double` | OLS 决定系数 R²。范围 0-1，1 = 完美拟合。 |

---

## PHYCHEM.* -- 物理化学

| 函数 | 参数 | 返回 | 说明 |
|------|------|------|------|
| `PHYCHEM.MOLWT` | (formula_text) | `double` | 分子量计算，如 `"H2SO4"` → 98.079 |
| `PHYCHEM.TEMP` | (number, from_unit, to_unit) | `double` | 温度换算：C, F, K。对标 Excel CONVERT |
| `PHYCHEM.PRESS` | (number, from_unit, to_unit) | `double` | 压力换算：ATM, PSI, PA, KPA, BAR, MMHG, TORR |
| `PHYCHEM.VOL` | (number, from_unit, to_unit) | `double` | 体积换算：L, GAL, ML, M3, QT, FT3 |
| `PHYCHEM.MASS` | (number, from_unit, to_unit) | `double` | 质量换算：KG, G, LB, OZ, TON, MG |
| `PHYCHEM.C_TO_F` | (celsius) | `double` | 摄氏度 → 华氏度 |
| `PHYCHEM.F_TO_C` | (fahrenheit) | `double` | 华氏度 → 摄氏度 |
| `PHYCHEM.KG_TO_LB` | (kg) | `double` | 千克 → 磅 |
| `PHYCHEM.LB_TO_KG` | (lb) | `double` | 磅 → 千克 |
| `PHYCHEM.L_TO_GAL` | (liters) | `double` | 升 → 美制加仑 |
| `PHYCHEM.GAL_TO_L` | (gallons) | `double` | 美制加仑 → 升 |
| `PHYCHEM.ATM_TO_PSI` | (atm) | `double` | 大气压 → PSI |
| `PHYCHEM.PSI_TO_ATM` | (psi) | `double` | PSI → 大气压 |
| `PHYCHEM.IDEALGAS` | (pressure, volume, moles, temperature) | `double` | 理想气体状态方程 PV=nRT。将待求量填 `*` |
| `PHYCHEM.GASSTP` | (volume, temperature, pressure) | `double` | 气体体积换算标况（STP） |
| `PHYCHEM.DENSITY` | (mass, volume) | `double` | 密度 = 质量 / 体积 |

---

## STR.* -- 字符串处理

> 除 TEXTJOIN/UUID/RND* 外，其他函数支持数组公式（逐元素处理）。

| 函数 | 参数 | 返回 | 说明 |
|------|------|------|------|
| `STR.REVERSE` | (text) | `string` | 反转字符串。`"hello"` → `"olleh"` |
| `STR.NORMWS` | (text) | `string` | 规范化空白：去首尾空格，合并连续空格为单个 |
| `STR.TITLE` | (text) | `string` | 转为首字母大写（Title Case） |
| `STR.REMOVE` | (text, old_text) | `string` | 删除 text 中所有出现在 old_text 里的字符 |
| `STR.KEEP` | (text, keep_chars) | `string` | 仅保留 text 中出现在 keep_chars 里的字符 |
| `STR.PADLEFT` | (text, num_chars, [pad_text]) | `string` | 左侧填充至指定长度 |
| `STR.PADRIGHT` | (text, num_chars, [pad_text]) | `string` | 右侧填充至指定长度 |
| `STR.TRUNCATE` | (text, num_chars, [suffix]) | `string` | 截断至指定长度，若截短则追加后缀（默认 `"..."`） |
| `STR.COUNTSUB` | (text, substring, [match_case]) | `long` | 子串出现次数。match_case=true 区分大小写 |
| `STR.STARTSWITH` | (text, prefix, [match_case]) | `bool` | 是否以指定前缀开头 |
| `STR.ENDSWITH` | (text, suffix, [match_case]) | `bool` | 是否以指定后缀结尾 |
| `STR.LEFTOF` | (text, delimiter, [instance_num]) | `string` | 第 instance_num 次出现的分隔符左侧内容 |
| `STR.RIGHTOF` | (text, delimiter, [instance_num]) | `string` | 第 instance_num 次出现的分隔符右侧内容 |
| `STR.EXTRACT` | (text, start_delimiter, end_delimiter, [instance_num], [include_delimiters]) | `string` | 第 instance_num 对左右分隔符之间的内容。include_delimiters=true 含分隔符 |
| `STR.NTHWORD` | (text, [instance_num]) | `string` | 第 instance_num 个空格分隔的词（1-based） |
| `STR.COMMONPFX` | (text1, text2, [match_case]) | `string` | 两字符串的最长公共前缀 |
| `STR.TEXTJOIN` | (delimiter, ignore_empty, text_array) | `string` | 用分隔符连接数组值。ignore_empty=true 跳过空值（对标 Excel TEXTJOIN） |
| `STR.LEVENSHTEIN` | (text1, text2) | `long` | 编辑距离（Levenshtein distance） |
| `STR.SOUNDEX` | (text) | `string` | Soundex 语音编码（4 字符） |
| `STR.URLENCODE` | (text) | `string` | URL 百分号编码。对标 Excel ENCODEURL |
| `STR.URLDECODE` | (text) | `string` | URL 百分号解码 |
| `STR.HTMLENCODE` | (text) | `string` | HTML 实体编码 |
| `STR.HTMLDECODE` | (text) | `string` | HTML 实体解码 |
| `STR.BASE64ENC` | (text) | `string` | Base64 编码（UTF-8） |
| `STR.BASE64DEC` | (text) | `string` | Base64 解码 |
| `STR.UUID` | () | `string` | 生成随机 UUID/GUID |
| `STR.RNDSTR` | (num_chars, [character_set]) | `string` | 从字符集 character_set 随机生成长度 num_chars 的字符串（默认 A-Za-z0-9） |
| `STR.RNDALPHA` | (num_chars) | `string` | 随机生成字母串（A-Z, a-z） |
| `STR.RNDNUM` | (num_chars) | `string` | 随机生成数字串（0-9） |
| `STR.ISNULLEMPTY` | (text) | `bool` | 是否为 null 或空串 |
| `STR.ISNULLWS` | (text) | `bool` | 是否为 null、空串或纯空白 |
| `STR.COALESCE` | (value1, value2) | `string` | 取第一个非 null/空值，否则返回 value2 |
| `STR.FORMAT` | (value, format_text) | `string` | 按 .NET 格式字符串格式化。如 `"0.00"`, `"yyyy-MM-dd"`。对标 Excel TEXT |
| `STR.STRIPHTML` | (text) | `string` | 去除 HTML 标签，仅保留文本 |

---

## DT.* -- 日期时间

> 日期参数接受 Excel 日期序列号。start_day: 0=Sun, 1=Mon, ... (默认 1=Mon)。

| 函数 | 参数 | 返回 | 说明 |
|------|------|------|------|
| `DT.ISOWEEK` | (serial_number) | `long` | ISO 8601 周数（1-53）。对标 Excel ISOWEEKNUM |
| `DT.WEEKDAY` | (serial_number) | `long` | 星期几（Sun=1, Sat=7，对标 VBA Weekday） |
| `DT.WEEKDAYISO` | (serial_number) | `long` | 星期几 ISO（Mon=1, Sun=7）。对标 Excel WEEKDAY |
| `DT.WEEKDAYNAME` | (serial_number) | `string` | 英文星期名（"Monday" 等） |
| `DT.SOW` | (serial_number, [start_day]) | `double` | 所在周的第一天。start_day 默认 1=周一，0=周日（Excel 日期值） |
| `DT.EOW` | (serial_number, [start_day]) | `double` | 所在周的最后一天。start_day 默认 1=周一，0=周日（Excel 日期值） |
| `DT.SOM` | (serial_number) | `double` | 当月第一天 |
| `DT.EOM` | (serial_number) | `double` | 当月最后一天。对标 Excel EOMONTH |
| `DT.WOM` | (serial_number, [start_day]) | `long` | 当月第几周（1-5） |
| `DT.DIM` | (year, month) | `long` | 指定年月的天数 |
| `DT.AGEYEARS` | (start_date, [end_date]) | `long` | 周岁。end_date 默认今天。对标 Excel DATEDIF |
| `DT.AGEMONTHS` | (start_date, [end_date]) | `long` | 足月数。end_date 默认今天 |
| `DT.AGEDAYS` | (start_date, [end_date]) | `long` | 总天数。end_date 默认今天 |
| `DT.ISWE` | (serial_number) | `bool` | 是否为周六或周日 |
| `DT.ADDWKD` | (start_date, workdays) | `double` | 加 workdays 个工作日（跳过周末）。对标 Excel WORKDAY |
| `DT.WKDBTWN` | (start_date, end_date) | `long` | 两个日期间的工作日数。对标 Excel NETWORKDAYS |
| `DT.NEXTWKD` | (serial_number) | `double` | 下一个工作日 |
| `DT.EASTER` | (year) | `double` | 该年复活节日期（公历算法） |
| `DT.QUARTER` | (serial_number) | `long` | 日历季度（1-4） |
| `DT.SEMESTER` | (serial_number) | `long` | 半年度（1 或 2） |
| `DT.DOY` | (serial_number) | `long` | 一年中的第几天（1-366） |
| `DT.ISLEAP` | (year) | `bool` | 是否为闰年 |
| `DT.UNIXTS` | (serial_number) | `double` | Excel 日期 → Unix 时间戳（秒） |
| `DT.FROMUNIX` | (unix_timestamp) | `double` | Unix 时间戳 → Excel 日期 |
| `DT.DATEDIFF` | (date_unit, start_date, end_date) | `long` | 日期差：`"d"`=天, `"m"`=月, `"y"`=年, `"w"`=周。对标 Excel DATEDIF |

---

## REGEX.* -- 正则表达式

> .NET 正则引擎。支持数组公式（逐元素处理），超时 5 秒自动取消。

| 函数 | 参数 | 返回 | 说明 |
|------|------|------|------|
| `REGEX.TEST` | (text, pattern, [ignore_case]) | `bool` | 是否匹配正则 |
| `REGEX.COUNT` | (text, pattern, [ignore_case]) | `long` | 非重叠匹配次数 |
| `REGEX.MATCH` | (text, pattern, [ignore_case], [instance_num]) | `string` | 第 instance_num 个匹配子串。1=第一个（默认），-1=最后一个，无匹配或越界返回 `""` |
| `REGEX.MATCHALL` | (text, pattern, [ignore_case]) | `string[]` | 所有正则匹配为数组 |
| `REGEX.REPLACE` | (text, pattern, replacement, [ignore_case], [instance_num]) | `string` | 替换第 instance_num 个正则匹配。0/省略=全部（默认），1=第一个，-1=最后一个 |
| `REGEX.SPLIT` | (text, pattern, [ignore_case], [instance_num]) | `string[]` | 按正则分隔符拆分。0/省略=全部（默认），>0=最多拆 instance_num 次（得 instance_num+1 段） |
| `REGEX.GROUPS` | (text, pattern, [ignore_case]) | `object[2,n]` | 捕获组。row0=组名, row1=值。`[0]`=完整匹配 |
| `REGEX.ESCAPE` | (text) | `string` | 转义正则特殊字符 |
| `REGEX.ISMATCH` | (text, pattern) | `bool` | 不区分大小写匹配（REGEX.TEST ignore_case=true 的别名） |

---

## ARR.* -- 数组操作

| 函数 | 参数 | 返回 | 说明 |
|------|------|------|------|
| `ARR.SORT` | (array, [sort_order], [sort_mode]) | `object[]` | 排序。sort_order=TRUE 升序（默认），sort_mode=`"auto"/"text"/"numeric"`。对标 Excel SORT |
| `ARR.SORTASC` | (array) | `object[]` | 升序排列（自动检测类型） |
| `ARR.SORTDESC` | (array) | `object[]` | 降序排列（自动检测类型） |
| `ARR.SORTNUM` | (array) | `object[]` | 按数值升序排列 |
| `ARR.SORTTEXT` | (array) | `object[]` | 按文本升序排列（不区分大小写） |
| `ARR.UNIQUE` | (array) | `object[]` | 去重，保留首次出现顺序。对标 Excel UNIQUE |
| `ARR.INDEXOF` | (array, lookup_value) | `long` | 值首次出现的 0-based 索引，未找到返回 -1。对标 Excel MATCH |
| `ARR.SLICE` | (array, start_index, num_elements) | `object[]` | 从索引 start_index 起取 num_elements 个元素 |
| `ARR.FLATTEN` | (array) | `object[]` | 二维区域按行展为一维。对标 Excel TOROW |
| `ARR.FILTER` | (array, criteria, comparison_operator) | `object[]` | 按比较运算符过滤：`=`, `<>`, `>`, `<`, `>=`, `<=`。对标 Excel FILTER |
| `ARR.FILTER_EQ` | (array, criteria) | `object[]` | 筛选等于 criteria 的元素 |
| `ARR.FILTER_NE` | (array, criteria) | `object[]` | 筛选不等于 criteria 的元素 |
| `ARR.FILTER_GT` | (array, criteria) | `object[]` | 筛选大于 criteria 的元素 |
| `ARR.FILTER_LT` | (array, criteria) | `object[]` | 筛选小于 criteria 的元素 |
| `ARR.CONCAT` | (array1, array2) | `object[]` | 拼接两个数组。对标 Excel VSTACK/HSTACK |
| `ARR.REVERSE` | (array) | `object[]` | 反转数组顺序 |
| `ARR.COUNT` | (array) | `long` | 元素个数。对标 Excel COUNT |
| `ARR.CONTAINS` | (array, lookup_value) | `bool` | 是否包含指定值 |
| `ARR.TOSET` | (array) | `object[]` | 去重（ARR.UNIQUE 的别名） |
| `ARR.FILL` | (value, count) | `object[]` | 生成长度 count 的数组，全部填充 value |
| `ARR.RANGE` | (start, end, step) | `object[]` | 生成数值序列 start → end，步长 step。对标 Excel SEQUENCE |
| `ARR.SHUFFLE` | (array) | `object[]` | 随机打乱（Fisher-Yates） |

---

## DICT.* -- 字典/集合操作

| 函数 | 参数 | 返回 | 说明 |
|------|------|------|------|
| `DICT.FREQUENCY` | (key_array) | `object[2,n]` | 频率统计。返回两列：value, count |
| `DICT.INTERSECT` | (array1, array2) | `object[]` | 交集：两个数组都有的值 |
| `DICT.UNION` | (array1, array2) | `object[]` | 并集：两个数组所有不重复值 |
| `DICT.EXCEPT` | (array1, array2) | `object[]` | 差集：在 array1 但不在 array2 的值 |
| `DICT.DICT` | (key_array, value_array) | `object[2,n]` | 用并行 key/value 数组构建双列表格 |
| `DICT.COUNT` | (dict_table) | `long` | 字典行数 |
| `DICT.KEYS` | (dict_table) | `object[]` | 提取字典第一列（键） |
| `DICT.VALUES` | (dict_table) | `object[]` | 提取字典第二列（值） |
---

## JSON.* / XML.* -- JSON/XML 处理

| 函数 | 参数 | 返回 | 说明 |
|------|------|------|------|
| `JSON.PARSE` | (json_text) | `object[,]` | 解析 JSON 为嵌套表/数组 |
| `JSON.QUERY` | (json_text, json_path) | `object` | 点路径查询。如 `"store.book[0].title"` |
| `JSON.VALIDATE` | (json_text) | `bool` | 是否为合法 JSON |
| `JSON.PRETTIFY` | (json_text) | `string` | JSON 格式化（缩进美化） |
| `JSON.TOTABLE` | (json_text) | `object[,]` | JSON 对象数组转二维表（含表头） |
| `XML.XPATH` | (xml_text, xpath_text) | `string[]` | XPath 查询，返回匹配元素的值。对标 Excel FILTERXML |
| `XML.VALIDATE` | (xml_text) | `bool` | 是否为合法 XML |
| `XML.TOTABLE` | (xml_text, row_xpath) | `object[,]` | XML 转二维表，row_xpath 定义行节点 |

---

## PIVOT.* -- 数据透视

| 函数 | 参数 | 返回 | 说明 |
|------|------|------|------|
| `PIVOT.PIVOT` | (source_range, row_field, col_field, value_field, [aggregation]) | `object[,]` | 透视表。row_field=行标签列, col_field=列标签列, value_field=值列。aggregation=`sum/avg/count/min/max`（默认 SUM） |
| `PIVOT.UNPIVOT` | (source_range, id_fields, value_fields) | `object[,]` | 逆透视：宽列转为键值行 |
| `PIVOT.GROUPBY` | (source_range, group_fields, agg_column, [aggregation]) | `object[,]` | 分组聚合。group_fields=分组列号数组, agg_column=聚合列（默认 SUM） |
| `PIVOT.CROSSJOIN` | (table1, table2) | `object[,]` | 笛卡尔积交叉连接 |

---

## FS.* -- 文件系统

> 需宏安全设置允许。元素级函数（除 LS/LSDIR/DRIVES/PWD/TEMP）支持数组公式。

| 函数 | 参数 | 返回 | 说明 |
|------|------|------|------|
| `FS.NORM` | (file_path) | `string` | 规范化路径（统一斜杠，解析 . 和 ..） |
| `FS.COMBINE` | (path1, path2) | `string` | 拼接路径 |
| `FS.FNAME` | (file_path) | `string` | 取文件名（含扩展名） |
| `FS.BNAME` | (file_path) | `string` | 取文件名（不含扩展名） |
| `FS.EXT` | (file_path) | `string` | 取扩展名（含点号） |
| `FS.FOLDER` | (file_path) | `string` | 取父目录路径 |
| `FS.FEXISTS` | (file_path) | `bool` | 文件是否存在 |
| `FS.FSIZE` | (file_path) | `long` | 文件大小（字节） |
| `FS.FDEXISTS` | (file_path) | `bool` | 文件夹是否存在 |
| `FS.MKDIR` | (file_path) | `bool` | 创建文件夹（含父目录） |
| `FS.LS` | (file_path, [search_pattern]) | `string[]` | 列出文件。`*` 匹配所有 |
| `FS.LSDIR` | (file_path, [search_pattern]) | `string[]` | 列出子文件夹 |
| `FS.READ` | (file_path) | `string` | 读取文本文件全部内容 |
| `FS.WRITE` | (file_path, file_content) | `bool` | 写入文本文件（覆盖） |
| `FS.APPEND` | (file_path, file_content) | `bool` | 追加写入文本文件 |
| `FS.COPY` | (source_path, destination_path) | `bool` | 复制文件 |
| `FS.MOVE` | (source_path, destination_path) | `bool` | 移动/重命名文件 |
| `FS.DELETE` | (file_path) | `bool` | 永久删除文件 |
| `FS.DELDIR` | (file_path) | `bool` | 删除文件夹及全部内容 |
| `FS.DRIVES` | () | `string[]` | 列出所有逻辑驱动器 |
| `FS.PWD` | () | `string` | 当前工作目录 |
| `FS.TEMP` | () | `string` | 系统临时文件夹路径 |

---

## SQL.* -- SQL 查询

> 数据通过参数化 INSERT 插入临时表。列名经字母数字消毒。请在可信输入上使用。

| 函数 | 参数 | 返回 | 说明 |
|------|------|------|------|
| `SQL.QUERY` | (source_range, sql_query) | `object[,]` | 对区域执行 SQL。表名 = `data` |
| `SQL.JOIN` | (source_range, join_table, sql_query) | `object[,]` | 两个表 `data` + `join_table` 的 SQL |
| `SQL.QUERY3` | (table1, table2, table3, sql_query) | `object[,]` | 三个表 `data` + `table2` + `table3` 的 SQL |

---

## RANGE.* -- 范围导出

| 函数 | 参数 | 返回 | 说明 |
|------|------|------|------|
| `RANGE.TOHTML` | (source_range, has_headers, [css_class]) | `string` | 导出为 HTML 表格 |
| `RANGE.TOJSON` | (source_range, has_headers, [pretty_print]) | `string` | 导出为 JSON |
| `RANGE.TOMD` | (source_range, has_headers) | `string` | 导出为 Markdown 表格 |
| `RANGE.TOCSV` | (source_range, [delimiter], [quote_fields]) | `string` | 导出为 CSV（自定义分隔符与字段引用） |
| `RANGE.TOCSVTAB` | (source_range) | `string` | 导出为 TSV（制表符分隔） |
| `RANGE.TOCSVSEMI` | (source_range) | `string` | 导出为分号分隔 CSV |
| `RANGE.TRANSPOSE` | (source_range) | `object[,]` | 行列转置。对标 Excel TRANSPOSE |
| `RANGE.SELCOLS` | (source_range, column_indices) | `object[,]` | 选取指定列。对标 Excel CHOOSECOLS |
| `RANGE.SELROWS` | (source_range, row_indices) | `object[,]` | 选取指定行。对标 Excel CHOOSEROWS |

---

## 错误参考

函数在以下情况返回错误值。`#VALUE!` (= 输入/执行错误) 和 `#NUM!` (= 计算结果无定义) 的含义不同。

### 通用错误

| 条件 | 结果 | 影响范围 |
|------|------|---------|
| 参数为 null（空引用） | MapOverMulti → `Empty`，直接 Core → 视函数而定 | 全部 |
| Excel 错误值（`#N/A`, `#DIV/0!` 等）作为输入 | MapOver → 透传原错误，直接 Core → 可能吞没 | 全部 |
| 多数组参数尺寸不匹配 | MapOverMulti → `#VALUE!`，V() → `#NUM!` | STATS 二元函数、REGEX、STR 部分 |
| 所有输入被过滤（全空/全错误） | `#VALUE!` 或 `NaN` | 统计类 |
| 正则超时（5 秒） | `#VALUE!` | REGEX.* |
| 文件路径越界（沙箱模式） | `#VALUE!` | FS.* |
| SQL 语法错误 | `#VALUE!` | SQL.* |

### 模块特定错误

| 模块 | 条件 | 结果 |
|------|------|------|
| **STATS** | 空数组 → Mean/Median/... | `#NUM!` |
| **STATS** | 常数数组求相关（Pearson/Spearman） | `#NUM!` |
| **STATS** | 方差为零的 t 检验 | `#NUM!` 或 1.0 |
| **STATS** | 几何平均含负数 | `#NUM!` |
| **LINALG** | 矩阵含 NaN/Inf | `#VALUE!` |
| **LINALG** | 非方阵求特征值/Cholesky | `#VALUE!` |
| **LINALG** | 奇异矩阵求 Solve | `#VALUE!` |
| **LINALG** | QR 宽矩阵超过 2000 列 | `#VALUE!` |
| **REGRESS** | 常数响应变量 y（TSS=0） | `#VALUE!` |
| **REGRESS** | n ≤ p（自由度不足） | `#VALUE!` |
| **REGRESS** | 权重含负数/NaN/Inf | `#VALUE!` |
| **REGRESS** | 岭回归 lambda=NaN/Inf | `#VALUE!` |
| **REGRESS** | ANOVA 少于 2 组 | `#VALUE!` |
| **PHYCHEM** | 分子式含未知元素 | `#NUM!` |
| **PHYCHEM** | 未知换算单位 | `#NUM!` |
| **PHYCHEM** | 理想气体方程待求量 ≠ 1 个 | `#NUM!` |
| **PHYCHEM** | 理想气体方程除零 | `#NUM!` |
| **STR** | Base64 解码非法输入 | `#VALUE!` |
| **DT** | 非法日期值（MinValue） | `#VALUE!` |
| **REGEX** | 非法正则表达式 | `#VALUE!` |
| **ARR** | RANGE 超过 100,000 元素 | `#VALUE!` |
| **PIVOT** | 不支持的聚合函数名 | `#VALUE!` |
| **PIVOT** | CrossJoin 超过 1,000,000 单元格 | `#VALUE!` |
| **FS** | 文件不存在 | `#VALUE!` |
| **FS** | 路径含非法字符 | `#VALUE!` |

> **提示**：`#VALUE!` 表示输入/执行错误（用户可修正输入），`#NUM!` 表示计算结果无定义（数据本身不满足数学条件）。详细行为见 [README.md](../README.md#错误处理)。

---

> **架构**：UDF 调用链与分层详见 [CLAUDE.md](../CLAUDE.md#架构分层)。
