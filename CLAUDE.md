# CLAUDE.md

项目宪法。编码细节按需加载对应 skill。

## 语言

所有 Markdown 文档默认优先中文。

## 技能

- `/excel-dna-project` — 编码规范、架构、MapOver 变体、测试模式、项目结构、全量测试命令
- `/excel-dna-addins` — Excel-DNA 加载项开发（UDF、.xll 打包、分发）

## 领域术语

见 [CONTEXT.md](CONTEXT.md)。

## 规则

- **Skill 前置**：修改代码前必须加载对应 skill（Core → `/excel-dna-project`，UDF/打包 → `/excel-dna-addins`）。
- **接口约束**：禁止修改 Core Public 签名和 UDF 参数/返回值。`#if NET48` 仅限内部实现，方法签名双目标一致。新增 NuGet 须确认双 TFM 可用。
- **防错三原则**（详细 ❌/✅ 示例见 skill）：
  1. 静默传播阻断 — NaN/Inf/null/default! 须显式 guard，WrapError 不兜底
  2. 防御完整性 — 安全机制（ValidatePath/Regex Timeout/SQL 参数化）须覆盖模块所有方法
  3. 异常过滤器统一 — 裸 `catch{}` = bug，须加 `when` 过滤器；自检 `grep -rn "catch\s*{" src/` 须返回空
- **缺陷处理**：发现缺陷 → 复核并追踪根因 → 写入 memory 或记录改进计划。
- **Git**：push 前须用户明确同意。禁止推送 src/tests 外的文件/文件夹。
- **文档**：信息只在一处定义，其余链接引用。数字以 api-reference.md 为准。
- **验证**：修改函数前用 CodeGraph 检查调用者，修改后验证一致性。CodeGraph 优先于 skill。
- **全量测试** = ①文档验证 ②全量单元测试 ③Excel 交叉验证 ④Debug+Release 双目标打包。具体命令见 skill。

## 架构分层

```
UDF (public static, [ExcelFunction])  →  Excel-DNA 入口
  ↓ MapOver/MapOverMulti/V() 分发
Core (internal static, 纯逻辑)         →  零 Excel 依赖
  ↓ 依赖
Foundation (共享工具)                   →  InputNormalizer, ElementWiseMapper, OutputWrapper
```

## 构建

```bash
dotnet restore && dotnet build && dotnet test
```
