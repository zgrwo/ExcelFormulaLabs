# API 参考

> **214 个 UDF 函数**的完整签名，按 14 个模块组织。使用指南见 [user-guide.md](user-guide.md)。

---

## STATS.* -- 描述统计

> 对标 Python scipy，精度 1e-10。元素级函数（ABS/SQRT/LN/LOG10/EXP/SIGN）支持数组公式。

| 函数 | 参数 | 返回 | 说明 |
|------|------|------|------|
| `STATS.MEAN` | (data) | `double` | 算术平均值 |
| `STATS.GEOMEAN` | (data) | `double` | 几何平均值（正数数组） |
| `STATS.HARMEAN` | (data) | `double` | 调和平均值（正数数组） |
| `STATS.MEDIAN` | (data) | `double` | 中位数 |
| `STATS.VARP` | (data) | `double` | 总体方差（除以 n） |
| `STATS.VAR` | (data) | `double` | 样本方差（除以 n-1） |
| `STATS.STDEVP` | (data) | `double` | 总体标准差（除以 n） |
| `STATS.STDEV` | (data) | `double` | 样本标准差（除以 n-1） |
| `STATS.SKEW` | (data) | `double` | 样本偏度 |
| `STATS.KURT` | (data) | `double` | 样本超额峰度 |
| `STATS.MIN` | (data) | `double` | 最小值 |
| `STATS.MAX` | (data) | `double` | 最大值 |
| `STATS.RANGE` | (data) | `double` | 极差（max - min） |
| `STATS.SUM` | (data) | `double` | 求和 |
| `STATS.PRODUCT` | (data) | `double` | 求积 |
| `STATS.PERCENTILE` | (data, p) | `double` | p 分位数（0-100），R7 算法 |
| `STATS.IQR` | (data) | `double` | 四分位距（Q3 - Q1），R7 算法 |
| `STATS.SUMMARY` | (data) | `double[9]` | 描述统计摘要：`[n, mean, stdev, min, q1, median, q3, max, iqr]`。R7 分位数（对标 Python scipy）。 |
| `STATS.COUNT` | (data) | `long` | 元素个数 |
| `STATS.MODE` | (data) | `double` | 众数。全唯一返回 NaN（对标 Excel MODE.SNGL） |
| `STATS.COVARP` | (a, b) | `double` | 总体协方差（除以 n） |
| `STATS.COVAR` | (a, b) | `double` | 样本协方差（除以 n-1） |
| `STATS.PEARSON` | (a, b) | `double` | Pearson 线性相关系数 r。范围 -1~1 |
| `STATS.SPEARMAN` | (a, b) | `double` | Spearman 秩相关系数。范围 -1~1 |
| `STATS.TTEST1` | (data, mu0) | `double` | 单样本双侧 t 检验 p 值（H₀: mean = mu0）。`p<0.05` = 均值与 mu0 差异显著 |
| `STATS.TTEST2` | (a, b) | `double` | Welch 双样本双侧 t 检验 p 值（不等方差）。`p<0.05` = 两样本均值差异显著 |
| `STATS.ZSCORE` | (data) | `double[]` | 标准化 z 值：(x - mean) / stdev |
| `STATS.ABS` | (x) | `double[]` | 逐元素绝对值 |
| `STATS.SQRT` | (x) | `double[]` | 逐元素平方根 |
| `STATS.LN` | (x) | `double[]` | 逐元素自然对数 ln(x) |
| `STATS.LOG10` | (x) | `double[]` | 逐元素常用对数 log₁₀(x) |
| `STATS.EXP` | (x) | `double[]` | 逐元素指数函数 eˣ |
| `STATS.SIGN` | (x) | `long[]` | 逐元素符号：-1, 0, 1 |

---

## LINALG.* -- 线性代数

| 函数 | 参数 | 返回 | 说明 |
|------|------|------|------|
| `LINALG.DET` | (matrix) | `double` | 矩阵行列式值 |
| `LINALG.SOLVE` | (A, b) | `double[]` | 解线性方程组 Ax = b |
| `LINALG.MATMUL` | (A, B) | `double[,]` | 矩阵乘法 |
| `LINALG.TRANSPOSE` | (matrix) | `double[,]` | 矩阵转置 |
| `LINALG.TRACE` | (matrix) | `double` | 矩阵迹（对角线元素之和） |
| `LINALG.RANK` | (matrix, tol) | `long` | 数值秩（默认容差 1e-10） |
| `LINALG.COND` | (matrix) | `double` | 条件数（2-范数） |
| `LINALG.EIGEN` | (matrix) | `double[]` | 特征值。要求对称矩阵，非对称输入返回错误 |
| `LINALG.SVD` | (matrix) | `1×3 {U, S, Vt}` | 奇异值分解 A = U·diag(S)·Vt。返回水平数组：第1个 U 矩阵，第2个 S 向量，第3个 Vt 矩阵 |
| `LINALG.QR` | (matrix) | `1×2 {Q, R}` | QR 分解 A = Q·R。返回：第1个 Q，第2个 R 上三角 |
| `LINALG.LU` | (matrix) | `1×3 {L, U, P}` | LU 分解 PA = LU（部分主元）。返回：第1个 L 下三角，第2个 U 上三角，第3个 P 置换矩阵 |
| `LINALG.PINV` | (matrix) | `double[,]` | Moore-Penrose 伪逆 |
| `LINALG.CHOLESKY` | (matrix) | `double[,]` | Cholesky 分解 |
| `LINALG.IDENTITY` | (n) | `double[,]` | 生成 n×n 单位矩阵 |

---

## REGRESS.* -- 回归分析

> 返回 2×n 表格（row0 = 字段名, row1 = 值）。统计解读：**p < 0.05 = 显著**，**R² 越接近 1 拟合越好**。

| 函数 | 参数 | 返回 | 说明 |
|------|------|------|------|
| `REGRESS.OLS` | (X, y) | `object[2,11]` | **普通最小二乘法**。返回：`coefficients`(系数)、`sse`(残差平方和)、`r_squared`(R²)、`adj_r_squared`(调整R²)、`residuals`(残差)、`fitted_values`(拟合值)、`standard_errors`(标准误)、`t_stats`(t值)、`p_values`(p值)、`n`(样本量)、`df`(自由度)。`p<0.05` 该系数显著。 |
| `REGRESS.WLS` | (X, y, w) | `object[2,11]` | **加权最小二乘法**（异方差数据）。返回同 OLS 的 11 个字段。 |
| `REGRESS.RIDGE` | (X, y, lambda) | `object[2,8]` | **岭回归**（L2 正则化，防过拟合）。λ 默认 1.0。返回：`coefficients`、`sse`、`r_squared`、`residuals`、`fitted_values`、`lambda`(惩罚参数)、`n`、`df`。**不返回**标准误/t值/p值（正则化下推断无效）。 |
| `REGRESS.ANOVA1` | (data) | `object[2,12]` | **单因素方差分析**。数据按列分组（每列一组）。返回：`ss_between`(组间平方和)、`ss_within`(组内平方和)、`ss_total`、`df_between`、`df_within`、`df_total`、`ms_between`、`ms_within`、`f_stat`(F值)、`p_value`(p值)、`group_means`(各组均值)、`group_counts`(各组样本量)。`p<0.05` = 至少有一组均值显著不同。 |
| `REGRESS.FACTORIMP` | (X, y) | `long[]` | **因子重要性排名**。按标准化后的 \|t\| 降序排列，返回 0-based 列索引数组。 |
| `REGRESS.COEF` | (X, y) | `double[]` | OLS 回归系数向量（仅 beta）。 |
| `REGRESS.RSQ` | (X, y) | `double` | OLS 决定系数 R²。范围 0-1，1 = 完美拟合。 |

---

## PHYCHEM.* -- 物理化学

| 函数 | 参数 | 返回 | 说明 |
|------|------|------|------|
| `PHYCHEM.MOLWT` | (formula) | `double` | 分子量计算，如 `"H2SO4"` → 98.079 |
| `PHYCHEM.TEMP` | (value, from, to) | `double` | 温度换算：C, F, K |
| `PHYCHEM.PRESS` | (value, from, to) | `double` | 压力换算：ATM, PSI, PA, KPA, BAR, MMHG, TORR |
| `PHYCHEM.VOL` | (value, from, to) | `double` | 体积换算：L, GAL, ML, M3, QT, FT3 |
| `PHYCHEM.MASS` | (value, from, to) | `double` | 质量换算：KG, G, LB, OZ, TON, MG |
| `PHYCHEM.C_TO_F` | (celsius) | `double` | 摄氏度 → 华氏度 |
| `PHYCHEM.F_TO_C` | (fahrenheit) | `double` | 华氏度 → 摄氏度 |
| `PHYCHEM.KG_TO_LB` | (kg) | `double` | 千克 → 磅 |
| `PHYCHEM.LB_TO_KG` | (lb) | `double` | 磅 → 千克 |
| `PHYCHEM.L_TO_GAL` | (liters) | `double` | 升 → 美制加仑 |
| `PHYCHEM.GAL_TO_L` | (gallons) | `double` | 美制加仑 → 升 |
| `PHYCHEM.ATM_TO_PSI` | (atm) | `double` | 大气压 → PSI |
| `PHYCHEM.PSI_TO_ATM` | (psi) | `double` | PSI → 大气压 |
| `PHYCHEM.IDEALGAS` | (P, V, n, T) | `double` | 理想气体状态方程 PV=nRT。将待求量填 `*` |
| `PHYCHEM.GASSTP` | (vol, temp, press) | `double` | 气体体积换算标况（STP） |
| `PHYCHEM.DENSITY` | (mass, volume) | `double` | 密度 = 质量 / 体积 |

---

## STR.* -- 字符串处理

> 除 TEXTJOIN/UUID/RND* 外，其他函数支持数组公式（逐元素处理）。

| 函数 | 参数 | 返回 | 说明 |
|------|------|------|------|
| `STR.REVERSE` | (text) | `string` | 反转字符串。`"hello"` → `"olleh"` |
| `STR.NORMWS` | (text) | `string` | 规范化空白：去首尾空格，合并连续空格为单个 |
| `STR.TITLE` | (text) | `string` | 转为首字母大写（Title Case） |
| `STR.REMOVE` | (text, chars) | `string` | 删除 text 中所有出现在 chars 里的字符 |
| `STR.KEEP` | (text, chars) | `string` | 仅保留 text 中出现在 chars 里的字符 |
| `STR.PADLEFT` | (text, len, pad) | `string` | 左侧填充至指定长度 |
| `STR.PADRIGHT` | (text, len, pad) | `string` | 右侧填充至指定长度 |
| `STR.TRUNCATE` | (text, len, sfx) | `string` | 截断至指定长度，若截短则追加后缀（默认 `"..."`） |
| `STR.COUNTSUB` | (text, sub, cs) | `long` | 子串出现次数。cs=true 区分大小写 |
| `STR.STARTSWITH` | (text, pfx, cs) | `bool` | 是否以指定前缀开头 |
| `STR.ENDSWITH` | (text, sfx, cs) | `bool` | 是否以指定后缀结尾 |
| `STR.LEFTOF` | (text, del, n) | `string` | 第 n 次出现的分隔符左侧内容 |
| `STR.RIGHTOF` | (text, del, n) | `string` | 第 n 次出现的分隔符右侧内容 |
| `STR.EXTRACT` | (text, l, r, n, inc) | `string` | 第 n 对左右分隔符之间的内容。inc=true 含分隔符 |
| `STR.NTHWORD` | (text, n) | `string` | 第 n 个空格分隔的词（1-based） |
| `STR.COMMONPFX` | (a, b, cs) | `string` | 两字符串的最长公共前缀 |
| `STR.TEXTJOIN` | (del, skip, vals) | `string` | 用分隔符连接数组值。skip=true 跳过空值（对标 Excel TEXTJOIN） |
| `STR.LEVENSHTEIN` | (a, b) | `long` | 编辑距离（Levenshtein distance） |
| `STR.SOUNDEX` | (text) | `string` | Soundex 语音编码（4 字符） |
| `STR.URLENCODE` | (text) | `string` | URL 百分号编码 |
| `STR.URLDECODE` | (text) | `string` | URL 百分号解码 |
| `STR.HTMLENCODE` | (text) | `string` | HTML 实体编码 |
| `STR.HTMLDECODE` | (text) | `string` | HTML 实体解码 |
| `STR.BASE64ENC` | (text) | `string` | Base64 编码（UTF-8） |
| `STR.BASE64DEC` | (text) | `string` | Base64 解码 |
| `STR.UUID` | () | `string` | 生成随机 UUID/GUID |
| `STR.RNDSTR` | (len, cs) | `string` | 从字符集 cs 随机生成长度 len 的字符串 |
| `STR.RNDALPHA` | (len) | `string` | 随机生成字母串（A-Z, a-z） |
| `STR.RNDNUM` | (len) | `string` | 随机生成数字串（0-9） |
| `STR.ISNULLEMPTY` | (text) | `bool` | 是否为 null 或空串 |
| `STR.ISNULLWS` | (text) | `bool` | 是否为 null、空串或纯空白 |
| `STR.COALESCE` | (primary, fb) | `string` | 取第一个非 null/空值，否则返回 fallback |
| `STR.FORMAT` | (value, fmt) | `string` | 按 .NET 格式字符串格式化。如 `"0.00"`, `"yyyy-MM-dd"` |
| `STR.STRIPHTML` | (text) | `string` | 去除 HTML 标签，仅保留文本 |

---

## DT.* -- 日期时间

> 日期参数接受 Excel 日期序列号。start_day: 0=Sun, 1=Mon, ... (默认 1=Mon)。

| 函数 | 参数 | 返回 | 说明 |
|------|------|------|------|
| `DT.ISOWEEK` | (date) | `long` | ISO 8601 周数（1-53） |
| `DT.WEEKDAY` | (date) | `long` | 星期几（Sun=0, Sat=6） |
| `DT.WEEKDAYISO` | (date) | `long` | 星期几 ISO（Mon=1, Sun=7） |
| `DT.WEEKDAYNAME` | (date) | `string` | 英文星期名（"Monday" 等） |
| `DT.SOW` | (date, sd) | `double` | 所在周的第一天。sd 默认 1=周一，0=周日（Excel 日期值） |
| `DT.EOW` | (date, sd) | `double` | 所在周的最后一天。sd 默认 1=周一，0=周日（Excel 日期值） |
| `DT.SOM` | (date) | `double` | 当月第一天 |
| `DT.EOM` | (date) | `double` | 当月最后一天 |
| `DT.WOM` | (date, sd) | `long` | 当月第几周（1-5） |
| `DT.DIM` | (y, m) | `long` | 指定年月的天数 |
| `DT.AGEYEARS` | (birth, ref?) | `long` | 周岁。参考日期默认今天 |
| `DT.AGEMONTHS` | (birth, ref?) | `long` | 足月数。参考日期默认今天 |
| `DT.AGEDAYS` | (birth, ref?) | `long` | 总天数。参考日期默认今天 |
| `DT.ISWE` | (date) | `bool` | 是否为周六或周日 |
| `DT.ADDWKD` | (date, n) | `double` | 加 n 个工作日（跳过周末） |
| `DT.WKDBTWN` | (start, end) | `long` | 两个日期间的工作日数 |
| `DT.NEXTWKD` | (date) | `double` | 下一个工作日 |
| `DT.EASTER` | (year) | `double` | 该年复活节日期（公历算法） |
| `DT.QUARTER` | (date) | `long` | 日历季度（1-4） |
| `DT.SEMESTER` | (date) | `long` | 半年度（1 或 2） |
| `DT.DOY` | (date) | `long` | 一年中的第几天（1-366） |
| `DT.ISLEAP` | (year) | `bool` | 是否为闰年 |
| `DT.UNIXTS` | (date) | `double` | Excel 日期 → Unix 时间戳（秒） |
| `DT.FROMUNIX` | (ts) | `double` | Unix 时间戳 → Excel 日期 |
| `DT.DATEDIFF` | (unit, d1, d2) | `long` | 日期差：`"d"`=天, `"m"`=月, `"y"`=年, `"w"`=周 |

---

## REGEX.* -- 正则表达式

> .NET 正则引擎。支持数组公式（逐元素处理），超时 5 秒自动取消。

| 函数 | 参数 | 返回 | 说明 |
|------|------|------|------|
| `REGEX.TEST` | (input, pattern, ic) | `bool` | 是否匹配正则 |
| `REGEX.COUNT` | (input, pattern, ic) | `long` | 非重叠匹配次数 |
| `REGEX.MATCH` | (input, pattern, ic) | `string` | 第一个匹配子串，无匹配返回 `""` |
| `REGEX.MATCHALL` | (input, pattern, ic) | `string[]` | 所有正则匹配为数组 |
| `REGEX.REPLACE` | (input, pattern, replacement, ic) | `string` | 替换所有正则匹配 |
| `REGEX.SPLIT` | (input, pattern, ic) | `string[]` | 按正则分隔符拆分 |
| `REGEX.GROUPS` | (input, pattern, ic) | `object[2,n]` | 捕获组。row0=组名, row1=值。`[0]`=完整匹配 |
| `REGEX.ESCAPE` | (literal) | `string` | 转义正则特殊字符 |
| `REGEX.ISMATCH` | (input, pattern) | `bool` | 区分大小写匹配（REGEX.TEST ic=true 的别名） |

---

## ARR.* -- 数组操作

| 函数 | 参数 | 返回 | 说明 |
|------|------|------|------|
| `ARR.SORT` | (array, asc, mode) | `object[]` | 排序。mode=`"auto"/"text"/"numeric"` |
| `ARR.SORTASC` | (array) | `object[]` | 升序排列（自动检测类型） |
| `ARR.SORTDESC` | (array) | `object[]` | 降序排列（自动检测类型） |
| `ARR.SORTNUM` | (array) | `object[]` | 按数值升序排列 |
| `ARR.SORTTEXT` | (array) | `object[]` | 按文本升序排列（不区分大小写） |
| `ARR.UNIQUE` | (array) | `object[]` | 去重，保留首次出现顺序 |
| `ARR.INDEXOF` | (array, value) | `long` | 值首次出现的 0-based 索引，未找到返回 -1 |
| `ARR.SLICE` | (array, start, len) | `object[]` | 从索引 start 起取 len 个元素 |
| `ARR.FLATTEN` | (array_2d) | `object[]` | 二维区域按行展为一维 |
| `ARR.FILTER` | (array, criteria, op) | `object[]` | 按比较运算符过滤：`=`, `<>`, `>`, `<`, `>=`, `<=` |
| `ARR.FILTER_EQ` | (array, criteria) | `object[]` | 筛选等于 c 的元素 |
| `ARR.FILTER_NE` | (array, criteria) | `object[]` | 筛选不等于 c 的元素 |
| `ARR.FILTER_GT` | (array, criteria) | `object[]` | 筛选大于 c 的元素 |
| `ARR.FILTER_LT` | (array, criteria) | `object[]` | 筛选小于 c 的元素 |
| `ARR.CONCAT` | (a, b) | `object[]` | 拼接两个数组 |
| `ARR.REVERSE` | (array) | `object[]` | 反转数组顺序 |
| `ARR.COUNT` | (array) | `long` | 元素个数 |
| `ARR.CONTAINS` | (array, value) | `bool` | 是否包含指定值 |
| `ARR.TOSET` | (array) | `object[]` | 去重（ARR.UNIQUE 的别名） |
| `ARR.FILL` | (value, n) | `object[]` | 生成长度 n 的数组，全部填充 value |
| `ARR.RANGE` | (start, end, step) | `object[]` | 生成数值序列 s → e，步长 step |
| `ARR.SHUFFLE` | (array) | `object[]` | 随机打乱（Fisher-Yates） |

---

## DICT.* -- 字典/集合操作

| 函数 | 参数 | 返回 | 说明 |
|------|------|------|------|
| `DICT.FREQUENCY` | (keys) | `object[2,n]` | 频率统计。返回两列：value, count |
| `DICT.INTERSECT` | (a, b) | `object[]` | 交集：两个数组都有的值 |
| `DICT.UNION` | (a, b) | `object[]` | 并集：两个数组所有不重复值 |
| `DICT.EXCEPT` | (a, b) | `object[]` | 差集：在 a 但不在 b 的值 |
| `DICT.DICT` | (keys, values) | `object[2,n]` | 用并行 key/value 数组构建双列表格 |
| `DICT.COUNT` | (dict_2d) | `long` | 字典行数 |
| `DICT.KEYS` | (dict_2d) | `object[]` | 提取字典第一列（键） |
| `DICT.VALUES` | (dict_2d) | `object[]` | 提取字典第二列（值） |

---

## JSON.* / XML.* -- JSON/XML 处理

| 函数 | 参数 | 返回 | 说明 |
|------|------|------|------|
| `JSON.PARSE` | (json_string) | `object[,]` | 解析 JSON 为嵌套表/数组 |
| `JSON.QUERY` | (json_string, path) | `object` | 点路径查询。如 `"store.book[0].title"` |
| `JSON.VALIDATE` | (json_string) | `bool` | 是否为合法 JSON |
| `JSON.PRETTIFY` | (json_string) | `string` | JSON 格式化（缩进美化） |
| `JSON.TOTABLE` | (json_string) | `object[,]` | JSON 对象数组转二维表（含表头） |
| `XML.XPATH` | (xml_string, xpath) | `string[]` | XPath 查询，返回匹配元素的值 |
| `XML.VALIDATE` | (xml_string) | `bool` | 是否为合法 XML |
| `XML.TOTABLE` | (xml_string, row_path) | `object[,]` | XML 转二维表，row_path 定义行节点 |

---

## PIVOT.* -- 数据透视

| 函数 | 参数 | 返回 | 说明 |
|------|------|------|------|
| `PIVOT.PIVOT` | (data, kc, pc, vc, agg) | `object[,]` | 透视表。kc=行标签列, pc=列标签列, vc=值列。agg=`sum/avg/count/min/max` |
| `PIVOT.UNPIVOT` | (data, ids, vals) | `object[,]` | 逆透视：宽列转为键值行 |
| `PIVOT.GROUPBY` | (data, gcs, ac, agg) | `object[,]` | 分组聚合。gcs=分组列号数组, ac=聚合列 |
| `PIVOT.CROSSJOIN` | (a, b) | `object[,]` | 笛卡尔积交叉连接 |

---

## FS.* -- 文件系统

> 需宏安全设置允许。元素级函数（除 LS/LSDIR/DRIVES/PWD/TEMP）支持数组公式。

| 函数 | 参数 | 返回 | 说明 |
|------|------|------|------|
| `FS.NORM` | (path) | `string` | 规范化路径（统一斜杠，解析 . 和 ..） |
| `FS.COMBINE` | (a, b) | `string` | 拼接路径 |
| `FS.FNAME` | (path) | `string` | 取文件名（含扩展名） |
| `FS.BNAME` | (path) | `string` | 取文件名（不含扩展名） |
| `FS.EXT` | (path) | `string` | 取扩展名（含点号） |
| `FS.FOLDER` | (path) | `string` | 取父目录路径 |
| `FS.FEXISTS` | (path) | `bool` | 文件是否存在 |
| `FS.FSIZE` | (path) | `long` | 文件大小（字节） |
| `FS.FDEXISTS` | (path) | `bool` | 文件夹是否存在 |
| `FS.MKDIR` | (path) | `bool` | 创建文件夹（含父目录） |
| `FS.LS` | (path, pattern) | `string[]` | 列出文件。`*` 匹配所有 |
| `FS.LSDIR` | (path, pattern) | `string[]` | 列出子文件夹 |
| `FS.READ` | (path) | `string` | 读取文本文件全部内容 |
| `FS.WRITE` | (path, content) | `bool` | 写入文本文件（覆盖） |
| `FS.APPEND` | (path, content) | `bool` | 追加写入文本文件 |
| `FS.COPY` | (src, dest) | `bool` | 复制文件 |
| `FS.MOVE` | (src, dest) | `bool` | 移动/重命名文件 |
| `FS.DELETE` | (path) | `bool` | 永久删除文件 |
| `FS.DELDIR` | (path) | `bool` | 删除文件夹及全部内容 |
| `FS.DRIVES` | () | `string[]` | 列出所有逻辑驱动器 |
| `FS.PWD` | () | `string` | 当前工作目录 |
| `FS.TEMP` | () | `string` | 系统临时文件夹路径 |

---

## SQL.* -- SQL 查询

> 数据通过参数化 INSERT 插入临时表。列名经字母数字消毒。请在可信输入上使用。

| 函数 | 参数 | 返回 | 说明 |
|------|------|------|------|
| `SQL.QUERY` | (data, sql) | `object[,]` | 对区域 `data` 执行 SQL。表名 = `data` |
| `SQL.JOIN` | (data, extra, sql) | `object[,]` | 两个表 `data` + `extra` 的 SQL |
| `SQL.QUERY3` | (a, b, c, sql) | `object[,]` | 三个表 `data` + `b` + `c` 的 SQL |

---

## RANGE.* -- 范围导出

| 函数 | 参数 | 返回 | 说明 |
|------|------|------|------|
| `RANGE.TOHTML` | (data, hdrs, css) | `string` | 导出为 HTML 表格 |
| `RANGE.TOJSON` | (data, hdrs, pretty) | `string` | 导出为 JSON |
| `RANGE.TOMD` | (data, hdrs) | `string` | 导出为 Markdown 表格 |
| `RANGE.TOCSV` | (data, delim, quote) | `string` | 导出为 CSV（自定义分隔符与字段引用） |
| `RANGE.TOCSVTAB` | (data) | `string` | 导出为 TSV（制表符分隔） |
| `RANGE.TOCSVSEMI` | (data) | `string` | 导出为分号分隔 CSV |
| `RANGE.TRANSPOSE` | (data) | `object[,]` | 行列转置 |
| `RANGE.SELCOLS` | (data, cols) | `object[,]` | 选取指定列 |
| `RANGE.SELROWS` | (data, rows) | `object[,]` | 选取指定行 |

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
