# 示例工作簿

[ExcelFormulaLabs-Samples.xlsx](https://github.com/zgrwo/ExcelFormulaLabs/blob/main/samples/ExcelFormulaLabs-Samples.xlsx)
包含 17 个模块各一个 sheet（JSON/XML 各一页，加使用说明共 18 个 sheet）：示例数据 + 可直接复制的公式。

## 使用步骤

1. 先安装加载项（见[首页](index.md)快速开始）；
2. 下载并用 Excel 打开示例工作簿；
3. 公式自动计算——未加载加载项时显示 `#NAME?` 属预期。

## 覆盖模块

| Sheet | 示例 |
| :--- | :--- |
| STATS | 均值 / 标准差 / 中位数 / 描述统计摘要 |
| STR | 反转 / Title Case / TEXTJOIN |
| REGEX | 正则匹配 / 替换 |
| DT | 周岁 / 闰年 / 复活节 |
| ARR | 去重 / 降序 / 切片 |
| JSON / XML | 解析 / XPath / 校验 |
| DICT | 频率统计 |
| LINALG | 行列式 / 解方程组 |
| REGRESS | R² / OLS 报告 |
| SOLVE | 请求行留空反求可调参数 + 模型质量 / 方程 |
| PHYCHEM | 温度 / 分子量 / 密度 |
| DOE | 全因子设计矩阵 + 效应 / ANOVA / Pareto |
| SQL | GROUP BY 聚合 |
| PIVOT | 分组求和 |
| RANGE | Markdown 导出 |
| FS | 路径规范化 / 扩展名 / 文件名 |

## 重新生成

工作簿由脚本生成（修改示例后运行）：

```bash
python scripts/generate-samples.py
```

> 依赖 `openpyxl`（仅生成时需要）；生成物已入库，用户无需安装 Python。
> 说明详见仓库 [samples/README.md](https://github.com/zgrwo/ExcelFormulaLabs/blob/main/samples/README.md)。
