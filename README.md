# Excel 函数增强库

[![CI](https://github.com/zgrwo/ExcelFormulaLabs/actions/workflows/ci.yml/badge.svg)](https://github.com/zgrwo/ExcelFormulaLabs/actions/workflows/ci.yml)
[![Release](https://github.com/zgrwo/ExcelFormulaLabs/actions/workflows/release.yml/badge.svg)](https://github.com/zgrwo/ExcelFormulaLabs/actions/workflows/release.yml)
[![GitHub release](https://img.shields.io/github/v/release/zgrwo/ExcelFormulaLabs)](https://github.com/zgrwo/ExcelFormulaLabs/releases/latest)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

**在 Excel 里直接用 `=STATS.MEAN()`、`=STR.REVERSE()`、`=JSON.PARSE()` 等函数。** 基于 C# 高性能实现，Python 级精度。net48 版本自带 IntelliSense 参数提示（net8.0 版本因 Excel-DNA 已知问题不提供，见[已知限制](#已知限制)），VBA 中可通过 `Application.Run` 直接调用。完整函数清单与数量见 [API 参考](rules/api-reference.md)（数字唯一信源，测试状态见上方 CI 徽章）。

---

## 安装

### 方式一：免安装运行时（推荐）

Win10/11 自带 .NET Framework 4.8，直接加载 net48 版本的 `.xll`：

1. Excel → 文件 → 选项 → 加载项 → 管理：Excel 加载项 → 转到 → 浏览
2. 选择 `.xll` 文件，点击确定
3. 看到安全提示点"启用"

| 文件 | 包含模块 |
|------|---------|
| `Analytics-AddIn-net48-packed.xll` | STATS · LINALG · REGRESS · PHYCHEM · DOE（需 .NET Framework 4.8） |
| `Analytics-AddIn-net8.0-packed.xll` | STATS · LINALG · REGRESS · PHYCHEM · DOE（需 .NET 8 运行时） |
| `DataToolkit-AddIn-net48-packed.xll` | STR · DT · REGEX · ARR · DICT · JSON/XML · PIVOT · SQL · FS · RANGE（需 .NET Framework 4.8） |
| `DataToolkit-AddIn-net8.0-packed.xll` | STR · DT · REGEX · ARR · DICT · JSON/XML · PIVOT · SQL · FS · RANGE（需 .NET 8 运行时） |

> **版本选择**：64 位 Excel 选文件名含 `64` 的 `.xll`，32 位 Excel 选不含的。`-net48` 版本无需额外安装运行时（Win10/11 自带），`-net8.0` 版本性能更优但需安装 .NET 8 运行时。两个加载项可同时加载，也可按需只装一个。

### 方式二：安装 .NET 8 运行时（性能更优）

1. 下载 [.NET Desktop Runtime 8.0](https://dotnet.microsoft.com/download/dotnet/8.0)（约 50 MB），双击安装
2. 验证：命令行运行 `dotnet --list-runtimes`，应出现 `Microsoft.NETCore.App 8.0.x`
3. 加载 net8.0 版本的 `.xll`（路径在 `net8.0-windows/publish/` 下）

### 验证安装

在任意单元格输入 `=STATS.MEAN(`，Excel 弹出函数自动补全即成功。

---

## 模块速览

> 完整签名、参数说明见 **[API 参考](rules/api-reference.md)**；每个函数的详细示例见 **[用户手册](rules/user-manual.md)**。

| 模块 | 做什么 | 试一试 |
|------|------|-------|
| `STATS.*` | 均值/方差/分位数/t检验/相关/相关矩阵… 对标 scipy | `=STATS.SUMMARY(A1:A100)` |
| `STR.*` | 反转/提取/编解码/编辑距离/格式化… | `=STR.TEXTJOIN(",", TRUE, A1:A10)` |
| `REGEX.*` | 正则匹配/替换/捕获组（Excel 原生没有） | `=REGEX.MATCH(A1, "\d+")` |
| `DT.*` | ISO 周/工作日/年龄/复活节/时间戳… | `=DT.AGEYEARS(B2, TODAY())` |
| `ARR.*` | 排序/筛选/去重/切片/打乱… | `=ARR.UNIQUE(A1:A100)` |
| `JSON.*` / `XML.*` | 解析 JSON、XPath 查询 | `=JSON.QUERY(A1, "0.Name")` |
| `DICT.*` | 频率统计/交集/并集/键值查找 | `=DICT.FREQUENCY(A1:A100)` |
| `LINALG.*` | 行列式/求逆/特征值/SVD/QR/LU… | `=LINALG.SOLVE(A1:C3, D1:D3)` |
| `REGRESS.*` | OLS/WLS/岭回归/ANOVA/因子重要性 | `=REGRESS.OLS(A1:A100, B1:C100)` |
| `PHYCHEM.*` | 分子量/温度/压力/体积/质量换算 | `=PHYCHEM.C_TO_F(100)` |
| `DOE.*` | 实验设计矩阵（全因子设计，对齐 Minitab/JMP） | `=DOE.PLAN(2,2,0,2,"full",FALSE)` |
| `SQL.*` | 对 Excel 区域写 SQL 查询 | `=SQL.QUERY(A1:D100, "SELECT Col1, AVG(Col3) FROM data GROUP BY Col1")` |
| `PIVOT.*` | 透视表/逆透视/分组聚合/交叉连接 | `=PIVOT.GROUPBY(A1:C100, {1}, 3, "avg")` |
| `RANGE.*` | 导出 HTML/JSON/Markdown/CSV | `=RANGE.TOMD(A1:D10, TRUE)` |
| `FS.*` | 读写文件/列目录/复制删除 | `=FS.READ("C:\data.txt")` |

---

## VBA 调用

加载 `.xll` 后，所有函数可通过 `Application.Run` 直接调用，无需引用或声明。详见 [API 参考 → VBA 调用](rules/api-reference.md#vba-调用)。

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

函数返回两类错误值：**`#VALUE!`**（输入/执行错误，用户可修正）和 **`#NUM!`**（计算结果无定义，数据本身不满足数学条件）。

- Excel 错误值（`#N/A`、`#DIV/0!` 等）在 MapOver 层透传，在统计函数中被跳过
- 空白单元格：MapOver 函数中透传为空；统计函数中按哨兵 NaN 处理（区域含空 → `#NUM!`，**不**跳过——与 Excel `AVERAGE` 跳过空值不同，见[已知限制](#已知限制)）
- 非数值单元格经类型转换后返回哨兵值（`0`/`false`/`""`），不视为错误
- 所有输入被过滤时返回 `#VALUE!` 或 `NaN`

> 完整错误清单见 **[API 参考 → 错误参考](rules/api-reference.md#错误参考)**（唯一信源）。

---

## 安全

### 文件系统沙箱

> ⚠️ **重要**：`FS.*` 函数默认**无路径限制**（`SandboxRoot` 为 `null`），可访问任意文件系统路径。
> 若分发给不受信任的用户，请务必在 `AddIn.cs` 的 `AutoOpen()` 中启用沙箱：

```csharp
FileSystemCore.Initialize(new SandboxConfig(@"C:\Users\Public\Documents"));
```

配置为不可变 record，启动时一次性设定，消除运行时竞态。越界访问返回 `#VALUE!`。沙箱支持 NTFS 重解析点（junctions/symlinks）逐段检查。

### SQL 注入防护

数据 INSERT 使用参数化查询，列名经字母数字消毒。用户提供的 SQL 语句本身不可参数化——请在可信输入上使用。

### 正则超时

所有 `REGEX.*` 函数内置 5 秒超时，防止 ReDoS 攻击导致 Excel 挂起。

---

## 质量保证

- **双 .NET 版本全量测试**，覆盖正常路径和退化输入（零值/空值/单元素/全等值）
- **Python 交叉验证**：Stats/Regression 与 numpy/scipy 逐项对照，精度 1e-10；DataToolkit 集成管道测试覆盖跨模块组合
- **手册验证**：Python 交叉验证覆盖全部 UDF 示例，确保结果与源码一致

---

## 已知限制

### IntelliSense（参数提示）仅限 net48

- **net48 加载项**：加载后在公式栏输入函数名时显示参数名浮动提示。
- **net8.0 加载项**：无参数提示。这是 Excel-DNA 已知 bug（[Issue #343](https://github.com/Excel-DNA/ExcelDna/issues/343)）——.NET 8 下 `ExcelSynchronizationContext.Post` 内部空引用。UDF 函数计算、公式列表完全不受影响。

> **变通方案**：选中含函数名的单元格后按 `Ctrl+Shift+A` 插入参数名占位符；或使用 Excel 的 `fx` 按钮查看函数参数对话框。

### SyncMacro 间歇性错误（Excel-DNA 上游问题）

极少数情况下 Excel 会报 `Unexpected error trying to run SyncMacro for queued macro execution`（AccessViolationException / TargetInvocationException）——这是 Excel-DNA 框架的已知问题（[Excel-DNA Issue #390](https://github.com/Excel-DNA/ExcelDna/issues/390)，open 状态），与 Excel 语言版本（非英语环境更易触发）、Office Click-to-Run 版本及计算时序相关，**与本加载项的函数逻辑无关**。本地 120 秒压力测试（含 `*_ASYNC` 异步 UDF 持续重算）未复现。

如频繁出现可尝试：① 卸载后重新加载加载项；② 避免在复杂工作簿中反复使用 `*_ASYNC` 函数；③ 临时禁用 net48 IntelliSense（注释 `AddIn.AutoOpen` 中的 `IntelliSenseServer.Install()` 后重新打包）。本加载项仅通过 Excel-DNA 官方机制使用异步队列（net48 IntelliSense 安装 + 异步 UDF 结果回传），未排队任何业务宏。

### 统计函数不跳过空白单元格

`STATS.*`/`REGRESS.*` 等统计函数对空白单元格按哨兵 NaN 传播（区域含空 → `#NUM!`），与 Excel 原生 `AVERAGE`/`SUM` 的"跳过空值"不同。需要跳过时请先用 `FILTER`/`IF` 或 `ARR.FILTER` 清洗数据。

### 双加载项同时卸载

两个加载项（Analytics + DataToolkit）已加载时，建议逐一卸载（先取消勾选一个，确定后再取消另一个）。

---

## 卸载

1. Excel → 文件 → 选项 → 加载项 → Excel 加载项 → 转到
2. 取消勾选加载项，确定
3. 彻底删除：移除 `.xll` 文件；如需卸载 .NET 8 Runtime，在 Windows 设置 → 应用 中操作

---

## 架构特点

```
UDF 层 (public static, [ExcelFunction])  ← 入口：仅分发与适配
  ↓ MapOver / MapOverMulti / V() 分发
Core 层 (internal static, 纯逻辑)       ← 零 Excel 依赖
  ↓ 依赖
Foundation (共享工具)                    ← InputNormalizer, MapOver, OutputWrapper
```

- ✅ UDF 不包含业务逻辑；Core 不引用 `ExcelDna.Integration`
- ❌ 禁止跨层直接调用或反向依赖
- **双 TFM**：net48（免安装，Win10/11 自动可用）+ net8.0（性能更优）
- **哨兵契约 L1-L5**：不可转换值返回零值哨兵，不抛异常
- **MapOver 抽象**：消除 ~3000 行重复样板代码

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

| 文档 | 角色 | 内容 |
|------|------|------|
| [README.en.md](README.en.md) | 英文入口 | English entry for international users |
| [API 参考](rules/api-reference.md) | 数字唯一信源 | 函数完整签名、参数说明、错误表 |
| [用户手册](rules/user-manual.md) | 学习教程 | 每个函数详细示例 + 结果解读指南 |
| [context.md](rules/context.md) | 术语表 | 所有术语唯一定义 |
| [AGENTS.md](AGENTS.md) | 项目宪法 | 架构分层、红线规则、开发流程 |
| [skill: excel-dna-project](skills/excel-dna-project.md) | 编码规范 | MapOver 选型、预防规则、测试模式 |
| [skill: excel-dna-addins](skills/excel-dna-addins.md) | 打包分发 | UDF 声明、黄金法则、.xll 打包 |

---

## 治理体系说明

本项目遵循 [Harmonization 治理规范](https://github.com/zgrwo/Harmonization) 模板体系：

| 文件 | 面向 | 职责 |
|------|------|------|
| `AGENTS.md` | AI 编程助手 | 项目宪法——架构、红线、编码准则、防幻觉铁律 |
| `readme.md` | 人类用户 | 功能指南——安装、模块速览、使用模式（本文件） |
| `rules/` | AI + 人类 | 规范文档——API 参考、用户手册、术语表、治理规范 |
| `skills/` | AI 编码 | 技能定义——语言陷阱、编码模式、重构守则 |

**核心原则**：SSOT（信息只在一处定义）、Skill-first（修改代码前加载技能）、四条核心准则。