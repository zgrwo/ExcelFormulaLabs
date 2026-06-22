# Excel 函数增强库

**在 Excel 里直接用 `=STATS.MEAN()`、`=STR.REVERSE()`、`=JSON.PARSE()` 等 140+ 个 VBA 没有或难实现的函数。** 基于 C# 高性能实现，Python 级精度。

## 什么时候用这个项目

| 你想做的事 | 适合吗 | 说明 |
|---|---|---|
| Excel 里快速求描述统计（均值/方差/偏度/峰度/分位数…） | ✅ | `STATS.*` 系列，对标 Python scipy |
| 文本处理（反转/提取/正则/编解码/相似度…） | ✅ | `STR.*` + `REGEX.*` 两套函数 |
| 对表格数据执行 SQL 查询 | ✅ | `SQL.QUERY()` 把 Excel 区域当数据表查 |
| JSON 解析、XML XPath 查询 | ✅ | 内置 `JSON.*` 和 `XML.*` |
| 数组排序/筛选/去重/切片 | ✅ | `ARR.*` 系列 |
| 日期计算（ISO 周/工作日/年龄/时间戳…） | ✅ | `DT.*` 系列 |
| 线性代数（矩阵分解/求逆/特征值…） | ✅ | `LINALG.*` 系列 |
| 回归分析（OLS/WLS/岭回归/ANOVA） | ✅ | `REGRESS.*` 系列 |
| 物理化学单位换算（温度/压力/质量/体积） | ✅ | `PHYCHEM.*` 系列 |
| 范围导出为 HTML/JSON/Markdown/CSV | ✅ | `RANGE.*` 系列 |
| 简单的 VBA 内置函数（SUM/COUNT/LEFT…） | ❌ | Excel 自带就很好，本项目不做重复轮子 |

## 你需要什么环境

**只需一样东西：让 Excel 能加载这个插件的运行时。**

| 你的情况 | 用什么版本 | 需要装什么 |
|---|---|---|
| 公司电脑、分发给同事 | **.NET Framework 4.8 版** | **什么都不用装**（Win10/11 自带） |
| 自己用、想用最新技术 | **.NET 8 版** | 装 [.NET 8 Runtime](https://dotnet.microsoft.com/download/dotnet/8.0)（约 50 MB） |

两个版本的功能 **完全一样**，选哪个都不影响你用的函数。

## 3 分钟上手

### 1. 下载 .xll 文件

从 [Releases](https://github.com) 页面下载，或者在项目里自己构建（见底部「从源码构建」）。

你会得到两个文件：
- `Analytics-AddIn-packed.xll` — 统计、回归、线性代数、物理化学
- `DataToolkit-AddIn-packed.xll` — 字符串、日期、正则、JSON、SQL、数组、文件

两个可以同时用，也可以只装你需要的。

### 2. 在 Excel 里加载

```
Excel → 文件 → 选项 → 加载项
→ 管理：Excel 加载项 → 转到
→ 浏览 → 选择 .xll 文件 → 确定
```

看到安全提示点"启用"。

### 3. 输入你的第一条公式

在任意单元格输入：

```
=STATS.MEAN(A1:A100)
=STR.REVERSE("hello")
=JSON.PARSE(A1)
```

按回车，函数就能用了。

---

## 函数速查

> 💡 所有函数都支持**数组公式**：传入一个区域，自动对每个元素进行计算。完整函数签名见 [API 参考](docs/api-reference.md)。

### 字符串 · `STR.*`（33 个函数）

文本反转、提取、编解码、格式化、随机生成……最常用的文本工具集。

```
=STR.REVERSE("hello")               → "olleh"
=STR.LEFTOF("a@b@c", "@", 2)        → "a@b"
=STR.EXTRACT("a[1]b[2]", "[", "]")  → "1"
=STR.TEXTJOIN(", ", TRUE, A1:A10)   → 类似 TEXTJOIN，但跳过空值
=STR.LEVENSHTEIN("kitten", "sitting") → 3   （编辑距离）
=STR.SOUNDEX("Robert")              → "R163"
=STR.UUID()                         → "d4f8c…"  （生成 UUID）
=STR.BASE64ENC("hello")             → "aGVsbG8="
```

### 统计 · `STATS.*`（32 个函数）

描述统计全覆盖，精度对标 Python scipy（差异 < 1e-10）。

```
=STATS.MEAN(A1:A100)               → 均值
=STATS.STDEV(A1:A100)              → 样本标准差
=STATS.STDEVP(A1:A100)             → 总体标准差
=STATS.PERCENTILE(A1:A100, 90)     → 第 90 百分位
=STATS.IQR(A1:A100)                → 四分位距
=STATS.SKEW(A1:A100)               → 偏度
=STATS.KURT(A1:A100)               → 峰度
=STATS.SUMMARY(A1:A100)            → 一键输出 9 项描述统计
=STATS.PEARSON(A1:A100, B1:B100)   → 相关系数
=STATS.TTEST2(A1:A50, B1:B50)      → 双样本 t 检验
=STATS.GEOMEAN(A1:A10)             → 几何均值
=STATS.HARMEAN(A1:A10)             → 调和均值
```

### 日期时间 · `DT.*`（26 个函数）

ISO 周、工作日计算、年龄、时间戳……弥补 Excel 原生日期函数的缺口。

```
=DT.ISOWEEK(A1)                   → ISO 8601 周数
=DT.AGEYEARS(B2, TODAY())         → 精确年龄
=DT.ADDWKD(A1, 5)                 → 5 个工作日后的日期
=DT.WKDBTWN(A1, B1)               → 两日期间工作日数
=DT.EASTER(2025)                  → 2025 年复活节日期
=DT.UNIXTS(A1)                    → Unix 时间戳
=DT.DATEDIFF("M", A1, B1)         → 月份差
```

### 数组 · `ARR.*`（22 个函数）

排序、筛选、去重、切片、合并……不用写 VBA 循环。

```
=ARR.SORT(A1:A100, TRUE, "numeric")  → 升序排列
=ARR.UNIQUE(A1:A100)                 → 去重
=ARR.FILTER(A1:A100, ">10", "gt")    → 筛选大于 10 的值
=ARR.SLICE(A1:A100, 5, 10)           → 取第 5 到第 14 个元素
=ARR.FLATTEN(B2:D10)                 → 二维转一维
=ARR.SHUFFLE(A1:A100)                → 随机打乱
```

### 正则 · `REGEX.*`（9 个函数）

Excel 原生没有正则。这 9 个函数补上了。

```
=REGEX.TEST(A1, "\d{3}-\d{4}")        → 是否匹配
=REGEX.MATCH(A1, "\d+")               → 提取第一个匹配
=REGEX.MATCHALL(A1, "\d+")            → 提取所有匹配
=REGEX.REPLACE(A1, "\s+", " ")        → 替换
=REGEX.SPLIT(A1, ",")                 → 按正则拆分
=REGEX.GROUPS(A1, "(\d+)-(\d+)")      → 捕获组
```

### JSON / XML（8 个函数）

在单元格里解析 JSON、查询 XPath。

```
=JSON.PARSE(A1)                       → JSON 转二维表
=JSON.QUERY(A1, "$.store.book[0].title")  → JSONPath 查询
=JSON.VALIDATE(A1)                    → 检查是否为合法 JSON
=XML.XPATH(A1, "//item/@id")          → XPath 查询
```

### 字典/集合 · `DICT.*`（8 个函数）

频率统计、交集/并集/差集、键值查找。

```
=DICT.FREQUENCY(A1:A100)              → 每个值的出现次数
=DICT.INTERSECT(A1:A10, B1:B10)       → 交集
=DICT.UNION(A1:A10, B1:B10)           → 并集
```

### 文件系统 · `FS.*`（22 个函数）

在 Excel 里读写文件、列目录、查大小。注意：需要宏安全设置允许。

```
=FS.READ("C:\data.txt")              → 读取文件内容
=FS.WRITE("C:\out.txt", A1)          → 写入文件
=FS.LS("C:\logs", "*.csv")           → 列出文件
=FS.FEXISTS("C:\data.txt")           → 检查文件是否存在
```

### 物理化学 · `PHYCHEM.*`（16 个函数）

分子量计算、温度/压力/质量/体积单位换算。

```
=PHYCHEM.MOLWT("H2SO4")              → 98.079
=PHYCHEM.C_TO_F(100)                 → 212（摄氏度转华氏度）
=PHYCHEM.ATM_TO_PSI(1)               → 14.6959
=PHYCHEM.LB_TO_KG(10)                → 4.53592
=PHYCHEM.DENSITY(100, 25)            → 4（密度 = 质量/体积）
```

### 线性代数 · `LINALG.*`（14 个函数）

矩阵行列式、求逆、特征值、SVD/QR/LU 分解、伪逆。

```
=LINALG.DET(A1:C3)                   → 行列式
=LINALG.SOLVE(A1:C3, D1:D3)          → 解线性方程组
=LINALG.MATMUL(A1:C3, E1:G3)         → 矩阵乘法
=LINALG.EIGEN(A1:C3)                 → 特征值
=LINALG.SVD(A1:C3)                   → 奇异值分解
```

### 回归分析 · `REGRESS.*`（7 个函数）

OLS、加权最小二乘、岭回归、ANOVA、因子重要性。

```
=REGRESS.OLS(A1:C100, D1:D100)       → 普通最小二乘法
=REGRESS.RIDGE(A1:C100, D1:D100, 0.1) → 岭回归
=REGRESS.ANOVA1(A1:D100)             → 单因素方差分析
```

### 范围导出 · `RANGE.*`（9 个函数）

把 Excel 范围导出为 HTML、JSON、Markdown、CSV。

```
=RANGE.TOHTML(A1:D10, TRUE)           → HTML 表格
=RANGE.TOMD(A1:D10, TRUE)             → Markdown 表格
=RANGE.TOCSV(A1:D10, ",", TRUE)       → CSV 字符串
=RANGE.TRANSPOSE(A1:D10)              → 转置
```

### 数据透视 · `PIVOT.*`（4 个函数）

透视表、逆透视、分组聚合、交叉连接。

```
=PIVOT.PIVOT(A1:D100, 1, 2, 3, "sum")   → 按第 1 列分组，第 2 列透视，聚合第 3 列
=PIVOT.GROUPBY(A1:C100, {1}, 3, "avg")   → 按第 1 列分组求第 3 列均值
```

### SQL 查询 · `SQL.*`（3 个函数）

把 Excel 区域当数据库表，写 SQL 查询。

```
=SQL.QUERY(A1:D100, "SELECT Col1, AVG(Col3) FROM data GROUP BY Col1")
=SQL.JOIN(A1:D100, E1:F50, "SELECT a.Col1, b.Col2 FROM data a JOIN extra b ON a.Col1=b.Col1")
```

---

## 质量保证

### 测试覆盖

| 模块 | 测试数 | 状态 |
|---|---|---|
| Foundation | 396（198 × 2 TFM） | ✅ |
| Analytics | 524（262 × 2 TFM） | ✅ |
| DataToolkit | 1,784（892 × 2 TFM） | ✅ |
| **总计** | **2,704** | **0 失败** |

两个目标框架（.NET 8 和 .NET Framework 4.8）全部通过同批测试。

### Python 交叉验证

所有统计函数（`STATS.*`）与 **Python numpy/scipy** 做了交叉验证。282 个真实数据点的 Mean、Stdev、Variance、Percentile、IQR、Skewness、Kurtosis 等 14 个指标与 Python 输出一致（精度 1e-10）。

详见 [tests/TestData/Cross_Validation_vs_Python.xlsx](tests/TestData/Cross_Validation_vs_Python.xlsx)。

---

## 从源码构建

如果你想自己编译，或者想修改函数行为。

```bash
# 1. 还原依赖
dotnet restore

# 2. 构建（同时产出 .NET 8 和 .NET Framework 4.8 版本）
dotnet build

# 3. 运行测试
dotnet test
```

构建产物：
```
src/Analytics/bin/Release/net8.0-windows/publish/Analytics-AddIn-packed.xll      (.NET 8)
src/Analytics/bin/Release/net48/publish/Analytics-AddIn-packed.xll               (.NET Framework 4.8)
src/DataToolkit/bin/Release/net8.0-windows/publish/DataToolkit-AddIn-packed.xll  (.NET 8)
src/DataToolkit/bin/Release/net48/publish/DataToolkit-AddIn-packed.xll           (.NET Framework 4.8)
```

## 项目结构

```
ExcelVbaLibraries.sln
├── src/
│   ├── Foundation/         ← 零依赖基础层（类型转换、数组映射、错误包装）
│   ├── Analytics/          ← 统计、回归、线性代数、物理化学
│   └── DataToolkit/        ← 字符串、日期、正则、JSON、XML、SQL、文件
├── tests/
│   ├── Foundation.Tests/   ← xUnit + FluentAssertions
│   ├── Analytics.Tests/
│   └── DataToolkit.Tests/
├── docs/
│   └── api-reference.md    ← 完整函数签名
├── skills/                 ← AI 辅助开发文档
└── README.md
```

## 更多文档

- [API 参考 — 完整函数签名](docs/api-reference.md)
- [用户指南](docs/user-guide.md)
- [Excel-DNA 加载项开发](skills/excel-dna-addins/skill.md)
- [项目编码规范](skills/excel-dna-project/skill.md)
