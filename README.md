# Excel 函数增强库

**在 Excel 里直接用 `=STATS.MEAN()`、`=STR.REVERSE()`、`=JSON.PARSE()` 等函数。** 基于 C# 高性能实现，Python 级精度。自带 IntelliSense 自动补全，VBA 中可通过 `Application.Run` 直接调用。

---

## 安装

### 方式一：免安装运行时（推荐）

Win10/11 自带 .NET Framework 4.8，直接加载 net48 版本的 `.xll`：

1. Excel → 文件 → 选项 → 加载项 → 管理：Excel 加载项 → 转到 → 浏览
2. 选择 `.xll` 文件，点击确定
3. 看到安全提示点"启用"

| 文件 | 包含模块 |
|------|---------|
| `Analytics-AddIn-packed.xll` | STATS · LINALG · REGRESS · PHYCHEM |
| `DataToolkit-AddIn-packed.xll` | STR · DT · REGEX · ARR · DICT · JSON/XML · PIVOT · SQL · FS · RANGE |

> **版本选择**：64 位 Excel 选文件名含 `64` 的 `.xll`，32 位 Excel 选不含的。Analytics 和 DataToolkit 两个加载项可同时加载，也可按需只装一个。

### 方式二：安装 .NET 8 运行时（性能更优）

1. 下载 [.NET Desktop Runtime 8.0](https://dotnet.microsoft.com/download/dotnet/8.0)（约 50 MB），双击安装
2. 验证：命令行运行 `dotnet --list-runtimes`，应出现 `Microsoft.NETCore.App 8.0.x`
3. 加载 net8.0 版本的 `.xll`（路径在 `net8.0-windows/publish/` 下）

### 验证安装

在任意单元格输入 `=STATS.MEAN(`，Excel 弹出函数自动补全即成功。

---

## 模块速览

> 完整签名、参数说明见 **[API 参考](docs/api-reference.md)**；每个函数的详细示例见 **[用户手册](docs/user-manual.md)**。

| 模块 | 数量 | 做什么 | 试一试 |
|------|:--:|------|-------|
| `STATS.*` | 33 | 均值/方差/分位数/t检验/相关… 对标 scipy | `=STATS.SUMMARY(A1:A100)` |
| `STR.*` | 34 | 反转/提取/编解码/编辑距离/格式化… | `=STR.TEXTJOIN(",", TRUE, A1:A10)` |
| `REGEX.*` | 9 | 正则匹配/替换/捕获组（Excel 原生没有） | `=REGEX.MATCH(A1, "\d+")` |
| `DT.*` | 25 | ISO 周/工作日/年龄/复活节/时间戳… | `=DT.AGEYEARS(B2, TODAY())` |
| `ARR.*` | 22 | 排序/筛选/去重/切片/打乱… | `=ARR.UNIQUE(A1:A100)` |
| `JSON.*` / `XML.*` | 8 | 解析 JSON、XPath 查询 | `=JSON.QUERY(A1, "0.Name")` |
| `DICT.*` | 8 | 频率统计/交集/并集/键值查找 | `=DICT.FREQUENCY(A1:A100)` |
| `LINALG.*` | 19 | 行列式/求逆/特征值/SVD/QR/LU… | `=LINALG.SOLVE(A1:C3, D1:D3)` |
| `REGRESS.*` | 7 | OLS/WLS/岭回归/ANOVA/因子重要性 | `=REGRESS.OLS(A1:A100, B1:C100)` |
| `PHYCHEM.*` | 16 | 分子量/温度/压力/体积/质量换算 | `=PHYCHEM.C_TO_F(100)` |
| `SQL.*` | 3 | 对 Excel 区域写 SQL 查询 | `=SQL.QUERY(A1:D100, "SELECT Col1, AVG(Col3) FROM data GROUP BY Col1")` |
| `PIVOT.*` | 4 | 透视表/逆透视/分组聚合/交叉连接 | `=PIVOT.GROUPBY(A1:C100, {1}, 3, "avg")` |
| `RANGE.*` | 9 | 导出 HTML/JSON/Markdown/CSV | `=RANGE.TOMD(A1:D10, TRUE)` |
| `FS.*` | 22 | 读写文件/列目录/复制删除 | `=FS.READ("C:\data.txt")` |

---

## VBA 调用

加载 `.xll` 后，所有函数可通过 `Application.Run` 直接调用，无需引用或声明。详见 [API 参考 → VBA 调用](docs/api-reference.md#vba-调用)。

---

## 使用模式

### 数组公式

所有函数支持数组输入。Excel 365 中自动溢出（spill），旧版按 `Ctrl+Shift+Enter`。

```
=STATS.MEAN(A1:A100)            ' 标量结果
=STATS.ABS(A1:A10)              ' 逐元素，返回数组
=LINALG.MATMUL(A1:C3, E1:G3)    ' 矩阵乘法，返回二维数组
```

### 多参数广播

多参数函数自动广播（broadcast）。标量参数广播到数组尺寸，等长数组逐元素配对。尺寸不匹配返回 `#VALUE!`。

```
=STR.STARTSWITH(A1:A10, B1)          ' 标量 B1 广播到整个数组
=STATS.COVAR(A1:A10, B1:B10)          ' 等长数组逐元素配对
```

### 典型场景

```
=STATS.SUMMARY(A1:A100)              ' 一键输出 count/mean/stdev/min/Q1/median/Q3/max/IQR
=DT.AGEYEARS(DATE(1990,5,15), TODAY())  ' 计算年龄
=REGEX.MATCHALL(A1, "\d+")           ' 提取所有数字
=JSON.QUERY(A1, "results[0].name")   ' 从 JSON 中取字段
=SQL.QUERY(A1:D500, "SELECT Dept, AVG(Salary) FROM data GROUP BY Dept")
=FS.READ("C:\Users\Public\Documents\data.txt")
```

---

## 错误处理

| 返回值 | Excel 显示 | 含义 | 常见原因 |
|--------|-----------|------|---------|
| `#VALUE!` | `#VALUE!` | 输入/执行错误 | 参数类型错误、矩阵维度不匹配、正则超时、文件不存在、除零 |
| `NaN` | `#NUM!` | 计算结果无定义 | 空数据集、方差为零的相关性、常数响应变量的回归 |
| `0` / `false` / `""` | 对应显示 | 哨兵值 | 非数值单元格类型转换后的占位值 |

**输入过滤**：Excel 错误值（`#N/A`、`#DIV/0!` 等）在 MapOver 层**透传**，在统计函数中被跳过。空白单元格跳过不计。所有输入被过滤时返回 `#VALUE!` 或 `NaN`。

完整错误清单见 [API 参考 → 错误参考](docs/api-reference.md#错误参考)。

---

## 安全

### 文件系统沙箱

`FS.*` 默认无路径限制。分发场景下可在 `AddIn.cs` 的 `AutoOpen()` 中启用沙箱：

```csharp
FileSystemCore.SandboxRoot = @"C:\Users\Public\Documents";
```

越界访问返回 `#VALUE!`。

### SQL 注入防护

数据 INSERT 使用参数化查询，列名经字母数字消毒。用户提供的 SQL 语句本身不可参数化——请在可信输入上使用。

### 正则超时

所有 `REGEX.*` 函数内置 5 秒超时，防止 ReDoS 攻击导致 Excel 挂起。

---

## 质量保证

- **4,132+ 个测试**（双 .NET 版本各约 2,066 个），覆盖正常路径和退化输入（零值/空值/单元素/全等值）
- **Python 交叉验证**：Stats/Regression 与 numpy/scipy 逐项对照，精度 1e-10；DataToolkit 集成管道测试覆盖跨模块组合
- **手册验证**：Python 交叉验证覆盖全部 UDF 示例，确保结果与源码一致

---

## 已知限制

### IntelliSense（参数提示）仅限 net48

- **net48 加载项**：加载后在公式栏输入函数名时显示参数名浮动提示。
- **net8.0 加载项**：无参数提示。这是 Excel-DNA 已知 bug（[Issue #343](https://github.com/Excel-DNA/ExcelDna/issues/343)）——.NET 8 下 `ExcelSynchronizationContext.Post` 内部空引用。UDF 函数计算、公式列表完全不受影响。

> **变通方案**：选中含函数名的单元格后按 `Ctrl+Shift+A` 插入参数名占位符；或使用 Excel 的 `fx` 按钮查看函数参数对话框。

### 双加载项同时卸载

两个加载项（Analytics + DataToolkit）已加载时，建议逐一卸载（先取消勾选一个，确定后再取消另一个）。

---

## 卸载

1. Excel → 文件 → 选项 → 加载项 → Excel 加载项 → 转到
2. 取消勾选加载项，确定
3. 彻底删除：移除 `.xll` 文件；如需卸载 .NET 8 Runtime，在 Windows 设置 → 应用 中操作

---

## 从源码构建

```bash
dotnet restore
dotnet build -c Release
dotnet test
```

产物：`src/*/bin/Release/{net8.0-windows|net48}/publish/`

---

## 文档索引

| 文档 | 内容 |
|------|------|
| [API 参考](docs/api-reference.md) | 函数完整签名、参数说明、错误表 |
| [用户手册](docs/user-manual.md) | 每个函数详细示例（4+ 列 × 5+ 行数据） |
| [CONTEXT.md](docs/CONTEXT.md) | 领域术语表 |
| [CLAUDE.md](CLAUDE.md) | 项目宪法：架构分层、红线规则、开发流程 |
| [skill: excel-dna-project](skills/excel-dna-project/skill.md) | 编码规范、MapOver 选型、测试模式 |
| [skill: excel-dna-addins](skills/excel-dna-addins/skill.md) | Excel-DNA UDF 声明、打包、分发 |
