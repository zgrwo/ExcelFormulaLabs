# ExcelFormulaLabs 用户手册

> **版本**：2.4.0 | **更新日期**：2026-09-16 <!-- x-release-please-version -->
> 完整签名见 [API 参考](../specification/api-reference.md)；安装说明见 [README](../../README.md)

---

## 目录

1. [STATS — 描述统计](modules/01-stats.md)
2. [LINALG — 线性代数](modules/02-linalg.md)
3. [REGRESS — 回归分析](modules/03-regress.md)
4. [SOLVE — 工艺参数反解](modules/04-solve.md)
5. [PHYCHEM — 物理化学](modules/05-phychem.md)
6. [DOE — 实验设计](modules/06-doe.md)
7. [STR — 字符串处理](modules/07-str.md)
8. [DT — 日期时间](modules/08-dt.md)
9. [REGEX — 正则表达式](modules/09-regex.md)
10. [ARR — 数组操作](modules/10-arr.md)
11. [DICT — 字典集合](modules/11-dict.md)
12. [JSON / XML](modules/12-json-xml.md)
13. [PIVOT — 数据透视](modules/13-pivot.md)
14. [SQL — SQL 查询](modules/14-sql.md)
15. [FS — 文件系统](modules/15-fs.md)
16. [RANGE — 范围导出](modules/16-range.md)
17. [错误参考](#17-错误参考)

---

## VBA 调用

加载 .xll 后，所有函数可通过 `Application.Run` 直接调用，无需引用或声明。详见 [API 参考 → VBA 调用](../specification/api-reference.md#vba-调用)。

---

## 通用约定

- **数组公式**：多数函数支持数组输入。Excel 365 中数组自动溢出（spill），旧版需 `Ctrl+Shift+Enter`
- **错误值**：`#VALUE!` = 输入/执行错误；`#NUM!` = 计算结果无定义（详见[错误参考](#17-错误参考)）
- **空值处理**：空单元格在数值函数中被转换为 `NaN`（逐元素运算返回 `#NUM!`，聚合运算返回 `#VALUE!`）；在字符串函数中视为空串。建议使用 `IF(ISNUMBER(), ...)` 预处理过滤
- **表头行**：带 `hasHeaders` 参数的函数默认将第一行视为表头

---

## `*_ASYNC` 异步变体说明

全部 240 个 UDF 中有 12 个 `*_ASYNC`（LINALG 9 个 + REGRESS 3 个）异步变体，本章各节不单列示例：
与对应同步版**共享同一 Core 实现与数值语义**，仅计算方式改为后台线程 + Excel 异步队列（重算期间 Excel 界面不阻塞），签名与结果完全一致。示例请直接参照同步版（如 `LINALG.SVD` ↔ `LINALG.SVD_ASYNC`）；其中 `LinalgAsyncUdf` / `RegressionAsyncUdf` 的 M/V 参数转换在调用线程完成，lambda 内为纯计算。

## 17. 错误参考

> 完整错误条件与影响范围见 [API 参考 → 错误参考](../specification/api-reference.md#错误参考)。`#VALUE!` = 输入/执行错误，`#NUM!` = 计算结果无定义。

---

## 附录：与 Excel 内置函数对照

| 本库函数 | Excel 内置函数 |
|----------|---------------|
| `STATS.ABS` | `ABS` |
| `STATS.COUNT` | `COUNT` |
| `STATS.COVAR` | `COVARIANCE.S` |
| `STATS.COVARP` | `COVARIANCE.P` |
| `STATS.EXP` | `EXP` |
| `STATS.LN` | `LN` |
| `STATS.LOG10` | `LOG10` |
| `STATS.MEAN` | `AVERAGE` |
| `STATS.MODE` | `MODE.SNGL` |
| `STATS.PEARSON` | `PEARSON` |
| `STATS.PERCENTILE` | `PERCENTILE.INC` |
| `STATS.SIGN` | `SIGN` |
| `STATS.SQRT` | `SQRT` |
| `STATS.STDEV` | `STDEV.S` |
| `STATS.STDEVP` | `STDEV.P` |
| `STATS.VAR` | `VAR.S` |
| `STATS.VARP` | `VAR.P` |
| `LINALG.DET` | `MDETERM` |
| `LINALG.MATMUL` | `MMULT` |
| `LINALG.TRANSPOSE` | `TRANSPOSE` |
| `REGRESS.OLS` | `LINEST` |
| `DT.ADDWKD` | `WORKDAY` |
| `DT.DATEDIFF` | `DATEDIF` |
| `DT.EOM` | `EOMONTH` |
| `DT.ISOWEEK` | `ISOWEEKNUM` |
| `DT.WKDBTWN` | `NETWORKDAYS` |
| `STR.FORMAT` | `TEXT` |
| `STR.TEXTJOIN` | `TEXTJOIN` |
| `STR.URLENCODE` | `ENCODEURL` |
| `ARR.FILTER` | `FILTER` |
| `ARR.FLATTEN` | `TOROW` |
| `ARR.INDEXOF` | `MATCH` |
| `ARR.RANGE` | `SEQUENCE` |
| `ARR.SORT` | `SORT` |
| `ARR.UNIQUE` | `UNIQUE` |
| `RANGE.SELCOLS` | `CHOOSECOLS` |
| `RANGE.SELROWS` | `CHOOSEROWS` |
| `RANGE.TRANSPOSE` | `TRANSPOSE` |
| `XML.XPATH` | `FILTERXML` |

---

*本文档所有示例已通过 Python 3.12 + NumPy + SciPy 交叉验证。详细验证脚本见 `scripts/verify-manual.py`。*

---

## 文档索引

| 文档 | 角色 | 内容 |
|------|------|------|
| [API 参考](../specification/api-reference.md) | 数字唯一信源 | UDF 完整签名、参数说明、错误表 |
| [README](../../README.md) | 用户入口 | 安装、模块速览、安全说明 |
| [AGENTS.md](../../AGENTS.md) | 项目宪法 | 架构分层、红线规则、开发流程 |
| [context.md](../governance/context.md) | 术语表 | 所有术语唯一定义 |
