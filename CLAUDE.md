# CLAUDE.md

项目宪法。编码细节按需加载对应 skill。

## 语言

所有 Markdown 文档默认优先中文。

## 技能

- `/excel-dna-project` — 编码规范、架构、MapOver 变体、测试模式
- `/excel-dna-addins` — Excel-DNA 加载项开发（UDF、.xll 打包、分发）

## 领域术语

见 [CONTEXT.md](CONTEXT.md)。

## 规则

- **接口约束**：禁止修改 Core 类 Public 签名和 UDF 参数/返回值。`#if NET48` 仅限内部实现，方法签名双目标一致。新增 NuGet 须确认双 TFM 可用。
- **缺陷处理**：发现缺陷 → 复核并追踪根因 → 写入 memory 或记录改进计划。
- **Git**：push 前须用户明确同意。禁止推送 src/tests 外的文件/文件夹。
- **文档**：信息只在一处定义，其余链接引用。数字仅存 api-reference.md。
- **上下文**：膨胀时主动建议开新会话。新会话自动继承本文件。

## 构建

```bash
dotnet restore && dotnet build && dotnet test
```
