# 文档职责规范

> 本文档定义项目中各文档的职责边界与维护规则。核心原则：**信息只在一处定义，其余各处链接引用（SSOT）**。

## 文档分工矩阵

| 文档 | 受众 | 核心问题 | 维护触发 |
|------|------|----------|----------|
| `agents.md` | AI 助手 | "项目怎么组织？红线在哪？" | 架构/红线/流程变更 |
| `README.md` | 人类用户 | "这是什么？怎么用？" | 功能新增/安装方式变更 |
| `rules/context.md` | AI + 新人 | "术语什么意思？" | 新概念引入 |
| `rules/api-reference.md` | 开发者/AI | "函数签名是什么？"（**签名唯一信源**） | 任何 Public 接口变更 |
| `rules/user-manual.md` | 最终用户 | "我要做 X，怎么操作？" | 用户可见功能变更 |
| `rules/project-structure.md` | 开发者/AI | "代码在哪？文件干什么？"（**结构唯一信源**） | 文件新增/删除/移动 |
| `rules/code-review-prompt.md` | AI 审查 | "如何系统性审查代码？" | 审查维度演进 |
| `skills/*.md` | AI 编码 | "这个语言/框架有什么陷阱？" | 发现新陷阱/模式 |

## 禁止事项

| ❌ 禁止 | 原因 |
|---------|------|
| 在多处重复定义同一信息 | 更新时必然遗漏，导致不一致 |
| 在 README 中写架构细节 | README 面向用户，架构属于 agents.md |
| 在代码注释中写使用教程 | 教程属于 user-manual.md |
| 在 api-reference 中写实现细节 | api-reference 只写签名和行为契约 |
| 在 agents.md 中写编码细节 | 编码细节属于 skills/ |

## 维护规则

### 同步更新链

```
新增 Public UDF
  → rules/api-reference.md（签名 + 参数 + 错误行为）
  → rules/user-manual.md（示例 + 结果解读）
  → README.md 模块速览（如为新模块）

新增/删除/移动文件
  → rules/project-structure.md（结构树）
  → agents.md 目录树（如为顶层变更）

引入新领域术语
  → rules/context.md（唯一定义）
```

### 数字一致性

- 函数计数、模块计数等数字**仅在 api-reference.md 中维护**
- 其他文档如需引用数字，使用"见 api-reference"链接，不硬编码数字

### 格式规范

- 所有文档使用 UTF-8 编码
- Markdown 标题层级不超过 4 级（####）
- 代码块标注语言（```csharp / ```python）
- 表格对齐，列宽适中
- 中文文档中英文术语首次出现时附原文

## 审查检查项

文档变更时逐项确认：

- [ ] 变更内容属于该文档的职责范围
- [ ] 未在其他文档中重复定义同一信息
- [ ] 引用的链接目标存在且正确
- [ ] 数字/计数与 api-reference.md 一致
- [ ] 新增术语已在 context.md 中定义
