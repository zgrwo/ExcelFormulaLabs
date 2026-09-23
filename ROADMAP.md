# ExcelFormulaLabs 路线图

> 本文件是项目的**公开路线图**：当前状态、决策门（什么能进、什么不进）与可认领任务。
> 内部实施计划见 [docs/plans/](docs/plans/)，技术规格见 [docs/specification/specification.md](docs/specification/specification.md)。
> 版本节奏：release-please 自动维护 Release PR；最新版本见 [CHANGELOG.md](CHANGELOG.md)。

## 项目定位

Excel 函数增强库：240 UDF，基于 C# / Excel-DNA，双 TFM（net48 + net8.0），
以 Excel 加载项（.xll）形式分发。覆盖统计分析、回归、线性代数、工艺参数反解、
物理化学换算、实验设计、字符串/日期/正则/JSON/XML/SQL/文件/数组/字典/透视/范围导出。

- 用户入口：[README.md](README.md) · [在线文档](https://zgrwo.github.io/ExcelFormulaLabs/)
- 快速上手：`scripts/install.ps1` 一键安装；[samples/](samples/) 示例工作簿开箱即用
- 贡献指南：[CONTRIBUTING.md](CONTRIBUTING.md) · 提交规范：Conventional Commits

## 决策门

新提案（Issue / PR）须通过对应门槛才会被合入。门槛不满足时，维护者会引导补齐或转为
"Not planned"（并说明理由）。

| 变更类型 | 准入条件（全部满足） |
| :--- | :--- |
| 新 UDF | ① 在 [api-reference.md](docs/specification/api-reference.md) 登记签名（数字唯一信源）② UDF 层仅分发、逻辑落 Core（零 Excel 依赖）③ 双 TFM 编译通过 ④ 单元测试含边界/NaN/空值 ⑤ 数值类须有 Python 交叉验证 |
| 新依赖 | ① net48 与 net8.0 双 TFM 可用 ② 更新 `packages.lock.json`（CI locked mode）③ 漏洞审计通过 ④ 无单框架依赖 |
| 性能优化 | 附 BenchmarkDotNet 前后对比（`benchmarks/`） |
| 行为变更 | 更新 specification + user-manual + CHANGELOG；破坏性变更须 ADR + 主版本号 |
| 文档 | 数字链接 api-reference，禁止硬编码；`verify-docs.ps1` 全绿 |

> 红线规则（接口兼容、防错三原则、表头/哨兵契约、闭环验证）见
> [AGENTS.md](AGENTS.md) 与 [CONTRIBUTING.md](CONTRIBUTING.md)。

## 里程碑

### 当前：v2.3.x（质量底线已建立）

- 覆盖率门禁 80/85/85（CI 同口径），测试质量守卫（零断言/恒真断言 FAIL，弱断言预算 0）
- 真 C# 交叉对照 157/240；依赖锁定 + 漏洞审计进 CI；release-please 自动发版
- 文档站上线；示例工作簿 + 一键安装脚本

### 近期：v2.4（工程质量）

- [ ] 静态分析补盲（IDE0051 未用私有成员 / CA1812 未实例化内部类）接入 CI
- [ ] 覆盖率继续爬坡至 90+（Foundation 优先）
- [ ] 英文 API 摘要页
- [ ] Excel COM E2E（`scripts/test-load-unload.py`）定期化评估结论落地

### 中期：v2.5（用户价值）

- [x] 性能基准回归阈值告警（benchmark-action 150% 阈值 + 历史缓存，只告警不阻断）
- [x] 手册按模块拆页（16 模块分页 + 总览/错误参考页，文档站导航）
- [x] 示例工作簿扩展（SOLVE 模型质量/方程、DOE 效应/ANOVA/Pareto 进阶示例）

> Excel COM E2E 定期化评估结论（2026-09-23）：暂不纳入 CI，保持发版前手工检查——
> 详见 [ADR-0010](docs/adr/0010-excel-com-e2e-scheduling.md)。

### 远期：v3.0（稳定性承诺）

- [ ] 公共 API 稳定性承诺（弃用策略 + 语义化版本边界）
- [ ] Excel COM E2E 定期化（依赖 self-hosted runner，见评估结论）

## 可认领任务（good first issue 候选）

适合首次贡献者的任务（在 Issue 中认领时请注明"good first issue"）：

| 任务 | 难度 | 涉及文件 |
| :--- | :--- | :--- |
| 补充 user-manual 缺失示例（以 verify-manual 缺口清单为准，`VERIFY_MANUAL_SHOW_GAPS=1`） | 低 | `docs/user-manual/user-manual.md` |
| 为 8 个未映射引用补交叉验证（DICT.FromKeys、DOE.TAGUCHI_L8、REGRESS.R²/SSE 等） | 中 | `tests/CrossValRunner/`、`scripts/verify-manual.py` |
| 示例工作簿新增"参数反解"场景 sheet | 中 | `samples/`、`scripts/generate-samples.py` |
| README.en / 英文 API 摘要翻译校对 | 低 | `README.en.md`、`docs/` |
| 为覆盖率报告中的未覆盖分支补测试（以 `scripts/coverage.ps1` 输出为准） | 中 | `tests/` |

> 认领前请阅读 [CONTRIBUTING.md](CONTRIBUTING.md)（开发环境、测试命令、提交规范）。
> 不确定业务语义时**先提问**，不要猜测——spec 是唯一信源。
