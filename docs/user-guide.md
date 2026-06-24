# Excel VBA Libraries .NET 版 -- 用户指南

## 概述

Excel VBA Libraries 是一个基于 .NET 的 Excel-DNA 加载项，将原 VBA 函数库完整移植到 C# 平台。提供 **214 个工作表函数 (UDF)**（数量以 [api-reference.md](api-reference.md) 为准），按功能划分为 **14 个类别**，涵盖统计、线性代数、回归、物理化学、字符串、日期时间、正则表达式、数组、字典/集合、JSON/XML、数据透视、文件系统、SQL 查询和范围导出等常用领域。

### 技术架构

```
Excel 工作表 → UDF Layer ([ExcelFunction]) → MapOver/MapOverMulti/V() 分发 → Core(纯逻辑) → OutputWrapper → 返回 Excel
```

所有函数均支持数组公式（自动），错误值（`#VALUE!`）和空白单元格（`ExcelEmpty`）在传输过程中自动透传过滤。

### 14 个函数类别

> 各分类具体函数数量以 [api-reference.md](api-reference.md) 为准。

| # | 类别前缀 | 函数数量 | 说明 | 示例 |
|---|---------|---------|------|------|
| 1 | `STATS.*` | — | 描述统计、假设检验、数学函数 | `=STATS.MEAN(A1:A100)` |
| 2 | `LINALG.*` | — | 矩阵运算、线性代数分解 | `=LINALG.DET(A1:D4)` |
| 3 | `REGRESS.*` | — | OLS/WLS/Ridge 回归、方差分析 | `=REGRESS.OLS(X,Y)` |
| 4 | `PHYCHEM.*` | — | 物理化学单位换算、理想气体、分子量 | `=PHYCHEM.C_TO_F(25)` |
| 5 | `STR.*` | — | 字符串处理、编码、格式化 | `=STR.REVERSE("hello")` |
| 6 | `DT.*` | — | 日期时间计算、工作日、年龄 | `=DT.AGEYEARS(B2,TODAY())` |
| 7 | `REGEX.*` | — | 正则匹配、替换、捕获分组 | `=REGEX.MATCH(A1,"\d+")` |
| 8 | `ARR.*` | — | 数组排序、过滤、切片、填充 | `=ARR.SORT(A1:A100,,TRUE)` |
| 9 | `DICT.*` | — | 字典/集合操作（频率、交/并/差集） | `=DICT.FREQUENCY(A1:A100)` |
| 10 | `JSON.*` / `XML.*` | — | JSON 解析/查询，XML XPath/验证 | `=JSON.QUERY(A1,"$.name")` |
| 11 | `PIVOT.*` | — | 数据透视/逆透视、分组、交叉连接 | `=PIVOT.PIVOT(A1:D100,1,2,3,"SUM")` |
| 12 | `FS.*` | — | 文件系统操作（读/写/删除/列表） | `=FS.READ("C:\data.txt")` |
| 13 | `SQL.*` | — | SQL 查询 Excel 范围 | `=SQL.QUERY(A1:D100,"SELECT * FROM data")` |
| 14 | `RANGE.*` | — | 范围导出（HTML/JSON/MD/CSV） | `=RANGE.TOJSON(A1:D10,TRUE)` |

> 函数数量以 [api-reference.md](api-reference.md) 为准。

## 安装

### 方式一：无需安装运行时（推荐 Windows 10/11 用户）

Windows 10/11 自带 .NET Framework 4.8，可直接加载 net48 版本的 .xll：

1. 打开 Excel → 文件 → 选项 → 加载项 → 管理：Excel 加载项 → 转到 → 浏览
2. 选择对应 .xll：
   - `src\Analytics\bin\Release\net48\publish\Analytics-AddIn-packed.xll`
   - `src\DataToolkit\bin\Release\net48\publish\DataToolkit-AddIn-packed.xll`
3. 64 位 Excel 选 `64-packed`，32 位选 `packed`

### 方式二：安装 .NET 8 运行时

如需使用 .NET 8 版本（性能更优），先安装运行时：

- 下载地址：https://dotnet.microsoft.com/download/dotnet/8.0
- 选择 **.NET Desktop Runtime 8.0.x** (Windows x64)
- 下载后双击安装，默认选项即可

验证：命令行运行 `dotnet --list-runtimes`，应出现 `Microsoft.NETCore.App 8.0.x`。

然后加载 net8.0 版本的 .xll：
   - `src\Analytics\bin\Release\net8.0-windows\publish\Analytics-AddIn-packed.xll`
   - `src\DataToolkit\bin\Release\net8.0-windows\publish\DataToolkit-AddIn-packed.xll`

> **注意**：如果 Excel 提示"此加载项可能包含病毒"或类似安全警告，请确保文件来自可信来源，并点击"启用"。

### 3. 验证安装

在任意单元格输入 `=STATS.MEAN(`，Excel 应显示函数自动完成提示，表明加载成功。

## 常用用法模式

### 数组公式

所有函数天然支持数组输入。在 Excel 365/2021 中，数组会自动溢出 (spill)。在旧版 Excel 中，需按 `Ctrl+Shift+Enter` 输入数组公式。

```
// 计算一列数据的均值
=STATS.MEAN(A1:A100)

// 对数组中每个元素取绝对值，返回数组
=STATS.ABS(A1:A10)

// 矩阵乘法（返回二维数组）
=LINALG.MATMUL(A1:C3, E1:G3)
```

### 多参数函数

多个参数的函数会自动进行广播（broadcast）：

```
// 标量参数自动广播——对 A1:A10 中每个元素与 B1 比较
=STR.STARTSWITH(A1:A10, B1)

// 两个等长数组逐元素配对计算
=STATS.COVAR(A1:A10, B1:B10)  // 两组数据协方差
=STR.STARTSWITH(A1:A10, B1)   // 标量第二参广播
```

### 错误处理

- 输入中的 `#N/A`、`#VALUE!` 等 Excel 错误值在计算前被过滤
- 空白单元格被跳过（不参与统计计算）
- 如果所有输入都被过滤（全部为空或错误），函数返回 `#VALUE!`
- 多数组参数尺寸不匹配时，统计类函数（`STATS.COVAR`、`STATS.COVARP`、`STATS.PEARSON`、`STATS.SPEARMAN` 等）返回 `NaN`（在 Excel 中可能显示为 `#NUM!`）

### 典型场景示例

```
// 统计摘要：一键输出 count/min/Q1/median/Q3/max/mean/stdev/IQR
=STATS.SUMMARY(A1:A100)
// 返回 1x9 水平数组，包含 9 个统计量

// 日期计算：计算年龄（年）
=DT.AGEYEARS(DATE(1990,5,15), TODAY())

// JSON 查询：从 API 返回的 JSON 中提取字段
=JSON.QUERY(WEBSERVICE("https://api.example.com/data"), "$.results[0].name")

// 正则提取：提取所有数字
=REGEX.MATCHALL(A1, "\d+")

// 文件操作：读取文本文件内容
=FS.READ("C:\Users\Public\Documents\data.txt")

// SQL 查询：对 Excel 表格执行 SQL
=SQL.QUERY(A1:D500, "SELECT Dept, AVG(Salary) FROM data GROUP BY Dept")
```

## 卸载

### 从 Excel 中移除加载项

1. Excel → **文件 → 选项 → 加载项**
2. 底部选择 **Excel 加载项**，点击 **转到...**
3. 在列表中取消勾选 `Analytics-packed` 和 `DataToolkit-packed`
4. 点击 **确定**

### 彻底删除

- 移除加载项后，可以删除对应的 `.xll` 文件和项目文件夹
- 如果不再需要 .NET 8 Runtime，可通过 Windows **设置 → 应用 → 已安装的应用** 找到并卸载 `Microsoft .NET Runtime 8.0.x`

## 更多参考

- API 完整签名：[api-reference.md](api-reference.md)
- 项目架构与编码规范：[../skills/excel-dna-project/skill.md](../skills/excel-dna-project/skill.md)
- 源码与测试：[../README.md](../README.md)
