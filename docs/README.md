# docs/ — 设计文档

> 设计文档/审查报告存放处。**规则文档**（术语、API、手册）在 `rules/`，本目录只放"非规范类"设计文档。
> 职责划分见 [documentation.md](../rules/documentation.md)。

| 文档 | 内容 |
| :--- | :--- |
| [cross-validation.md](cross-validation.md) | 交叉验证方法论（C# ↔ Python ↔ 手册闭环） |
| [guard-checklist.md](guard-checklist.md) | NaN/Inf 守卫逐项确认清单 |

## 约定

- 新增设计文档须登记 `rules/project-structure.md` 目录树
- 文档数字/计数一律链接到 `rules/api-reference.md`，禁止硬编码（verify-docs 检查 9/11 强制）
