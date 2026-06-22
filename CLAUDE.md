# CLAUDE.md

本文件是 AI 的项目宪法，始终在上下文中。编码细节按需从 [skills/excel-dna-project/skill.md](skills/excel-dna-project/skill.md) 加载。

## 语言

所有 Markdown 文档默认优先中文。

## 项目架构

```
UDF ([ExcelFunction]) → InputNormalizer → MapOver/MapOverFlat/MapOverMulti/V() → Core → WrapError → Excel
```
Core 层 100% 测试覆盖。UDF 214 个方法，各模块职责见 [skills/excel-dna-project/skill.md](skills/excel-dna-project/skill.md)。

## 编码规范

修改代码前：先加载 [skills/excel-dna-project/skill.md](skills/excel-dna-project/skill.md) 了解该模块的编码模式和关键行为差异。

## 接口约束

**禁止修改 VBA-Core 类（Foundation/Analytics/DataToolkit 中的 internal static class）的 Public 接口**，除非用户明确要求并确认影响范围。

## 缺陷处理

发现缺陷 → 复核并追踪根因 → 将根因写入 memory 系统或记录为改进计划。

## 上下文管理

上下文膨胀时主动建议开新会话。新会话自动继承本文件和 [skills/excel-dna-project/skill.md](skills/excel-dna-project/skill.md)，无需重复加载。

## 会话进度

会话结束前将进度和待办写入 memory 系统，不写入本文件（避免 git 噪音）。

## Git

- `git push` 前必须获得用户明确同意
- 禁止推送本项目结构（src/tests）之外的文件或文件夹

## 文档原则

信息只在一处定义，其余各处链接引用。按任务类型查找对应文档：编码规范→[skills/excel-dna-project/skill.md](skills/excel-dna-project/skill.md)、测试数据→tests/TestData/
