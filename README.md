# Excel 函数增强库

**在 Excel 里直接用 `=STATS.MEAN()`、`=STR.REVERSE()`、`=JSON.PARSE()` 等 214 个函数。** 基于 C# 高性能实现，Python 级精度。自带 IntelliSense 自动补全。

---

## 下载安装

从 [Releases](https://github.com/zgrwo/ExcelVbaLibraries/releases) 下载，或自行构建（见底部「从源码构建」）。

拿到两个 `.xll` 文件：

| 文件 | 包含的函数 |
|------|----------|
| `Analytics-AddIn-packed.xll` | 统计 · 回归 · 线性代数 · 物理化学 |
| `DataToolkit-AddIn-packed.xll` | 字符串 · 日期 · 正则 · JSON · SQL · 数组 · 文件 |

> 两个可以同时加载，也可以只用你需要的。64 位 Excel 选 `64-packed`，32 位选 `packed`。

**在 Excel 里加载：**

```
Excel → 文件 → 选项 → 加载项 → 管理：Excel 加载项 → 转到 → 浏览 → 选择 .xll → 确定
```

看到安全提示点"启用"。搞定。

---

## 试试看

在任意单元格输入：

```
=STATS.MEAN(A1:A100)        → 求均值
=STR.REVERSE("hello")       → "olleh"
=JSON.PARSE(A1)             → 解析 JSON
```

输入 `=STATS.` 时 Excel 自动弹出全部统计函数和说明。214 个函数的完整说明见 [API 参考](docs/api-reference.md)。

---

## 环境

| 场景 | 版本 | 需要安装 |
|------|------|---------|
| 公司电脑、分发给同事 | **.NET Framework 4.8** | 无（Win10/11 自带） |
| 自己用、追求性能 | **.NET 8** | [.NET 8 Runtime](https://dotnet.microsoft.com/download/dotnet/8.0)（约 50 MB） |

两个版本功能完全一致。

---

## 模块速览

> 完整函数签名、参数说明、返回值解读见 **[API 参考](docs/api-reference.md)**。

| 模块 | 做什么 | 试一试 |
|------|--------|-------|
| `STATS.*` (33) | 均值/方差/分位数/t检验/相关… 对标 Python scipy | `=STATS.SUMMARY(A1:A100)` |
| `STR.*` (34) | 反转/提取/编解码/编辑距离/格式化… | `=STR.TEXTJOIN(",", TRUE, A1:A10)` |
| `REGEX.*` (9) | 正则匹配/替换/捕获组（Excel 原生没有） | `=REGEX.MATCH(A1, "\d+")` |
| `DT.*` (25) | ISO 周/工作日/年龄/复活节/时间戳… | `=DT.AGEYEARS(B2, TODAY())` |
| `ARR.*` (22) | 排序/筛选/去重/切片/打乱… | `=ARR.UNIQUE(A1:A100)` |
| `JSON.*` / `XML.*` (8) | 解析 JSON、XPath 查询 | `=JSON.QUERY(A1, "store.book[0].title")` |
| `DICT.*` (8) | 频率统计/交集/并集/键值查找 | `=DICT.FREQUENCY(A1:A100)` |
| `LINALG.*` (14) | 矩阵行列式/求逆/特征值/SVD/QR/LU | `=LINALG.SOLVE(A1:C3, D1:D3)` |
| `REGRESS.*` (7) | OLS/WLS/岭回归/ANOVA/因子重要性 | `=REGRESS.OLS(A1:C100, D1:D100)` |
| `PHYCHEM.*` (16) | 分子量/温度/压力/体积/质量换算 | `=PHYCHEM.C_TO_F(100)` |
| `SQL.*` (3) | 对 Excel 区域写 SQL 查询 | `=SQL.QUERY(A1:D100, "SELECT Col1, AVG(Col3) FROM data GROUP BY Col1")` |
| `PIVOT.*` (4) | 透视表/逆透视/分组聚合/交叉连接 | `=PIVOT.GROUPBY(A1:C100, {1}, 3, "avg")` |
| `RANGE.*` (9) | 导出 HTML/JSON/Markdown/CSV | `=RANGE.TOMD(A1:D10, TRUE)` |
| `FS.*` (22) | 读写文件/列目录/复制删除 | `=FS.READ("C:\data.txt")` |

---

## 质量保证

- **1,700+ 个独立测试**（双 .NET 版本共执行 3,400+ 次） — 覆盖正常路径、退化输入（零值/空值/单元素/全等值），双 TFM 全通过
- **Python 交叉验证** — Stats/Regression 与 numpy/scipy/statsmodels 逐项对照，精度 1e-10；DataToolkit 集成管道测试覆盖 String→Stats 等跨模块组合

---

## 从源码构建

```bash
dotnet restore
dotnet build -c Release
dotnet test
```

构建产物：

| 文件 | .NET 8 | .NET 4.8 |
|------|:---:|:---:|
| `Analytics-AddIn-packed.xll` (32-bit) / `64-packed` (64-bit) | ✅ | ✅ |
| `DataToolkit-AddIn-packed.xll` (32-bit) / `64-packed` (64-bit) | ✅ | ✅ |

> 发布位置：`src/*/bin/Release/{net8.0-windows|net48}/publish/`

---

## 更多文档

- [API 参考](docs/api-reference.md) — 214 个函数的完整签名、参数说明、返回值解读
- [用户指南](docs/user-guide.md)
- [CONTEXT.md](CONTEXT.md) — 领域术语表
