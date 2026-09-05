# skills/ — AI Skill 定义

> 项目级技能**单一信源**。`.qoder/skills/` 为 Qoder 本地工具镜像（**不入库**，本地保留），
> 由 `scripts/sync-qoder-skills.ps1` 同步；`verify-docs.ps1` 检查 13 在 .qoder 存在时校验一致性。
> 结构唯一定义见 [project-structure.md](../docs/governance/project-structure.md)。

## 项目级技能（本目录）

| 技能 | 触发时机 | 内容 |
| :--- | :--- | :--- |
| [excel-dna-project.md](excel-dna-project.md) | 修改 Foundation/Analytics/DataToolkit 代码前（必须） | 编码规范、架构、MapOver 选型、表头/哨兵契约、测试模式 |
| [excel-dna-addins.md](excel-dna-addins.md) | UDF 声明 / .xll 打包 / 分发 | Excel-DNA 黄金法则、打包流程 |
| [architecture-reviewer.md](architecture-reviewer.md) | 新增组件/层级/依赖前 | 架构审查（YAGNI 四问） |
| [refactoring-guardian.md](refactoring-guardian.md) | 重构 Phase 开始/结束时 | 重构守卫（安全网） |
| [project-plan-review.md](project-plan-review.md) | 里程碑复盘/规划评审 | 项目规划审查 |
| [project-experience.md](project-experience.md) | 修改代码/依赖/门禁/发版前（速查） | 高频陷阱与铁律（版本臆测、pwsh7 差异、8.3 短路径、数值溢出等） |

## 过程技能策略（不 vendored）

通用过程技能（brainstorming / writing-plans / test-driven-development /
systematic-debugging / subagent-driven-development / verification-before-completion）
由 AI 工具链（harness/IDE 的 skill 系统）提供，**不复制进本仓库**：
避免第三方英文内容与中文项目文档混仓、避免随模板二次分发产生版本漂移。
本仓库只维护**项目特有**技能，保证每个文件都有明确的所有者与维护触发。

## 维护规则

- 修改任何 `skills/*.md` 后**必须**运行 `scripts/sync-qoder-skills.ps1` 同步镜像
- verify-docs.ps1 检查 13 强制镜像一致（未同步 = CI 红）
- 新技能文件必须登记 `docs/governance/project-structure.md` 目录树
