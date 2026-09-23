# 示例工作簿

`ExcelFormulaLabs-Samples.xlsx` — 16 个模块各一个 sheet，含示例数据与可直接复制的公式。

## 使用

1. 先安装加载项（见仓库根 [README.md](../README.md#安装) 或 `scripts/install.ps1`）；
2. 打开工作簿，公式自动计算；未加载加载项时显示 `#NAME?` 属预期。

## 重新生成

示例工作簿由脚本生成，修改示例后运行：

```bash
python scripts/generate-samples.py
```

> 依赖 `openpyxl`（仅生成时需要）。生成物入库，用户无需安装 Python。

## 覆盖范围

| Sheet | 模块 | 示例 |
| :--- | :--- | :--- |
| STATS | 统计 | 均值/标准差/中位数/摘要 |
| STR | 字符串 | 反转/Title/连接 |
| REGEX | 正则 | 匹配/替换 |
| DT | 日期时间 | 周岁/闰年/复活节 |
| ARR | 数组 | 去重/降序/切片 |
| JSON / XML | 结构化数据 | 解析/查询/校验 |
| DICT | 频率统计 | 频次表 |
| LINALG | 线性代数 | 行列式/解方程组 |
| REGRESS | 回归 | R²/OLS 报告 |
| SOLVE | 参数反解 | 请求行留空反求可调参数 + 模型质量/方程 |
| PHYCHEM | 物化换算 | 温度/分子量/密度 |
| DOE | 实验设计 | 全因子矩阵 + 效应/ANOVA/Pareto |
| SQL | SQL 查询 | GROUP BY 聚合 |
| PIVOT | 分组聚合 | 分组求和 |
| RANGE | 区域导出 | Markdown |
| FS | 文件路径 | 扩展名/文件名/规范化 |

完整签名见 [api-reference.md](../docs/specification/api-reference.md)，逐函数示例见 [user-manual.md](../docs/user-manual/user-manual.md)。
