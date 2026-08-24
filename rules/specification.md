# ExcelFormulaLabs — 项目规格文档

> 版本：v2.1.0 | 最后更新：2026-08-22 | 状态：稳定发行中

## 1. 项目概述

**ExcelFormulaLabs** 是一个基于 C# / Excel-DNA 的 Excel 函数增强库，提供 232 个高性能 UDF（用户定义函数，数量以 [api-reference.md](api-reference.md) 为唯一信源），覆盖统计分析、线性代数、回归、物理化学、字符串、日期时间、正则、数组、字典、JSON/XML、透视表、SQL、文件系统、范围导出等 14 个模块。

### 核心价值

- 在 Excel 中直接使用 `=STATS.MEAN()`、`=STR.REVERSE()`、`=JSON.PARSE()` 等函数
- C# 高性能实现，Python 级精度（与 scipy/numpy 交叉验证，容差 1e-10）
- 自带 IntelliSense 自动补全（net48）
- VBA 中可通过 `Application.Run` 直接调用
- 双 TFM 支持：net48（免安装）+ net8.0（性能更优）

### 目标用户

- 需要超越 Excel 原生函数能力的数据分析师
- 需要在 Excel 中进行统计/线性代数/回归计算的工程师
- 需要 JSON/SQL/正则等数据处理能力的开发者

## 2. 功能规格

### 2.1 模块清单

| 模块 | 前缀 | 函数数 | 产出 XLL | 说明 |
|------|------|--------|----------|------|
| 统计 | STATS.* | 34 | Analytics | 均值/方差/分位数/t检验/相关/ANOVA |
| 线性代数 | LINALG.* | 28 | Analytics | 行列式/求逆/特征值/SVD/QR/LU |
| 回归 | REGRESS.* | 10 | Analytics | OLS/WLS/岭回归/因子重要性 |
| 物理化学 | PHYCHEM.* | 16 | Analytics | 分子量/温度/压力/气体定律 |
| 字符串 | STR.* | ~34 | DataToolkit | 反转/提取/编解码/编辑距离 |
| 日期时间 | DT.* | 25 | DataToolkit | ISO周/工作日/年龄/时间戳 |
| 正则 | REGEX.* | 9 | DataToolkit | 匹配/替换/捕获组（5秒超时） |
| 数组 | ARR.* | 22 | DataToolkit | 排序/筛选/去重/切片/打乱 |
| 字典 | DICT.* | 8 | DataToolkit | 频率/交集/并集/键值查找 |
| JSON/XML | JSON.*/XML.* | 8 | DataToolkit | 解析/XPath查询 |
| 透视表 | PIVOT.* | 4 | DataToolkit | 透视/逆透视/分组聚合/交叉连接 |
| SQL | SQL.* | 3 | DataToolkit | 对Excel区域写SQL查询 |
| 文件系统 | FS.* | 22 | DataToolkit | 读写文件/列目录（沙箱保护） |
| 范围导出 | RANGE.* | 9 | DataToolkit | 导出HTML/JSON/MD/CSV |

### 2.2 关键技术特性

- **数组公式支持**：所有函数支持数组输入，Excel 365 自动溢出
- **多参数广播**：标量参数自动广播到数组尺寸
- **MapOver 三变体**：MapOver（保持形状）/ MapOverFlat（强制1D）/ MapOverMulti（广播）
- **哨兵契约 L1-L5**：不可转换值返回类型零值，不抛异常
- **闭环验证**：Python ↔ C# ↔ 手册三方交叉验证

## 3. 架构规格

### 3.1 分层架构

```
UDF 层 (public static, [ExcelFunction])  ← 入口：仅分发与适配
  ↓ MapOver / MapOverMulti / V() 分发
Core 层 (internal static, 纯逻辑)       ← 零 Excel 依赖
  ↓ 依赖
Foundation (共享工具)                    ← InputNormalizer, ElementWiseMapper, OutputWrapper
```

### 3.2 产出物

| 产出 | 框架 | 说明 |
|------|------|------|
| Analytics-AddIn-net48-packed.xll | .NET Framework 4.8 | 免安装，Win10/11 直接可用 |
| Analytics-AddIn-net8.0-packed.xll | .NET 8 | 需安装运行时，性能更优 |
| DataToolkit-AddIn-net48-packed.xll | .NET Framework 4.8 | 同上 |
| DataToolkit-AddIn-net8.0-packed.xll | .NET 8 | 同上 |

### 3.3 依赖

- Excel-DNA 1.9.0
- MathNet.Numerics 5.0.0
- System.Data.SQLite (net48) / Microsoft.Data.Sqlite (net8.0)
- ExcelDna.IntelliSense (仅 net48)

## 4. 质量规格

### 4.1 测试体系

- 2,290 单元测试（xUnit + FluentAssertions，[Fact]/[Theory] 实测计数）
- Python 交叉验证（scipy/numpy 独立计算，容差 1e-10）
- 手册示例验证（verify-manual.py 全 UDF 覆盖）
- XLL 加载/卸载自动化测试

### 4.2 安全规格

- 文件系统沙箱（SandboxRoot + 重解析点逐段检查）
- SQL 参数化（列名消毒 + 参数绑定）
- 正则超时（5 秒防 ReDoS）
- 异常过滤器（排除 OOM/StackOverflow/AccessViolation）

### 4.3 已知限制

- IntelliSense 仅限 net48（Excel-DNA Issue #343）
- MathNet QR 不支持宽矩阵 m<n（已用零填充绕过）
- 双加载项同时卸载需逐一操作

## 5. 历史演化摘要

| 阶段 | 时间 |  commits | 关键事件 |
|------|------|----------|----------|
| 初始版本 | 06-22 | 1 | 232 UDF + 2,290 测试（现计数） |
| 审查修复期 | 06-22 ~ 07-05 | ~60 | 多轮深度审查，NaN守卫/安全加固/文档体系 |
| 功能扩展期 | 07-05 ~ 07-15 | ~30 | CORRMATRIX/交叉验证/IntelliSense |
| 稳定发行期 | 07-15 ~ 07-23 | ~47 | v1.0.4→v1.0.7，审查修复+性能优化 |

**总计**：持续演进（2026-06-22 起；历史经多次深度审查迭代，提交数不在此硬编码——以 git 为准）