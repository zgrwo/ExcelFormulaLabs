# Excel VBA Libraries .NET 版 -- 用户指南

## 概述

Excel VBA Libraries 是一个基于 .NET 的 Excel-DNA 加载项，将原 VBA 函数库完整移植到 C# 平台。提供 **219 个工作表函数 (UDF)**（数量以 [api-reference.md](api-reference.md) 为准），按功能划分为 **14 个类别**，涵盖统计、线性代数、回归、物理化学、字符串、日期时间、正则表达式、数组、字典/集合、JSON/XML、数据透视、文件系统、SQL 查询和范围导出等常用领域。

所有函数均支持数组公式（自动），错误值（`#VALUE!`）和空白单元格（`ExcelEmpty`）在传输过程中自动透传过滤。

219 个函数分为 14 个类别。完整列表和签名见 **[API 参考](api-reference.md)**。

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

#### 返回值含义

| 返回值 | Excel 显示 | 含义 | 常见原因 |
|--------|-----------|------|---------|
| `#VALUE!` | `#VALUE!` | 输入/执行错误 | 参数类型错误、矩阵维度不匹配、正则超时、文件不存在、除零 |
| `NaN` | `#NUM!` | 计算结果无定义 | 空数据集、方差为零的相关性、常数响应变量的回归 |
| `0` / `false` / `""` | `0` / `FALSE` / (空) | 哨兵值 | 非数值单元格经类型转换后的占位值（不参与计算） |

#### 输入过滤规则

- Excel 错误值（`#N/A`、`#VALUE!`、`#DIV/0!` 等）在 MapOver 调度层**透传**（输入是错误则输出也是错误），或在统计函数中被**跳过**
- 空白单元格（`ExcelEmpty`）被跳过，不参与统计计算
- 非数值单元格被转换为类型哨兵值后参与数组运算（如字符串 → `""`、布尔 → `false`）
- 如果所有输入都被过滤，函数返回 `#VALUE!` 或 `NaN`（取决于函数类型）

#### 常见错误示例

```
// 矩阵维度不匹配 → #VALUE!
=LINALG.MATMUL(A1:C3, E1:E10)

// 除零 → NaN (#NUM!)
=PHYCHEM.DENSITY(5, 0)

// 常数数据求相关 → NaN (#NUM!)
=STATS.PEARSON({5,5,5}, {1,2,3})

// 不支持的聚合函数名 → #VALUE!
=PIVOT.PIVOT(A1:D100, 0, 1, 2, "MEDIAN")

// 文件不存在 → #VALUE!
=FS.READ("C:\\missing.txt")
```

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
