# tools/ — 辅助工具

> 存放项目自用的辅助工具/脚本（非 CI 门禁、非模块脚手架）。结构唯一定义见 [project-structure.md](../rules/project-structure.md)。

## 用途

- 一次性数据迁移/清理脚本
- 本地开发提效小工具（生成器、转换器等）
- 不属于 `scripts/`（治理门禁）也不属于 `templates/`（模块脚手架）的杂项脚本

## 约定

- 工具应「小、独立、可删除」——不承载项目核心逻辑
- 若工具演变为可复用治理能力，迁移到 `scripts/` 并登记 `rules/project-structure.md`
- 当前无实际工具（空目录保留本 README 说明用途；需要时再补，YAGNI）
