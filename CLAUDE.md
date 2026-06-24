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

- **Skill 前置**：修改任何代码前，必须加载对应 skill——修改 Core → `/excel-dna-project`，修改 UDF/打包 → `/excel-dna-addins`。
- **接口约束**：禁止修改 Core 类 Public 签名和 UDF 参数/返回值。`#if NET48` 仅限内部实现，方法签名双目标一致。新增 NuGet 须确认双 TFM 可用。
- **静默传播阻断**：任何可能产生 NaN/±Infinity/null/default! 的代码路径须在最近位置显式处理（guard 为 NaN 或 throw）。WrapError 只捕获异常，不捕获静默值——依赖 IEEE 754 传播 = bug。历史：Sum→Inf (1994814)、Reshape截断 (a477475)、Regression NaN (16e7cfc)。
- **防御完整性**：安全/验证机制必须覆盖模块内所有相关方法，禁止遗漏。添加新方法时检查同类方法是否都有相同防护（ValidatePath、Regex Timeout、SQL 参数化）。自检：`grep` 防护调用 vs 方法列表，确认一一对应。历史：Regex无timeout (a477475)、FileExists/FolderExists无ValidatePath (16e7cfc)。
- **异常过滤器统一**：所有 catch 块须加 `when` 过滤器。裸 `catch{}` 视为 bug。模板：`catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)`，COM interop 加 `and not AccessViolationException`。构建前自检：`grep -rn "catch\s*{" src/` 须返回空。历史：STR.FORMAT (a477475)、全项目18处 (3493cfe)。
- **缺陷处理**：发现缺陷 → 复核并追踪根因 → 写入 memory 或记录改进计划。
- **Git**：push 前须用户明确同意。禁止推送 src/tests 外的文件/文件夹。
- **文档**：信息只在一处定义，其余链接引用。数字以 api-reference.md 为准（UDF 数量、函数签名等权威定义唯一信源）。用户文档可引用展示，但维护以 api-reference.md 为准。
- **验证**：修改函数前用 CodeGraph 检查调用者（blast radius），修改后用 CodeGraph 验证一致性。如 skill 或 CodeGraph 冲突，优先 CodeGraph。
- **全量测试** = ①全量单元测试 ②Excel 交叉验证 ③Debug+Release DLL/XLL 双目标打包。具体命令与精度要求见 `/excel-dna-project` skill。

## 架构分层

```
UDF (public static, [ExcelFunction])  →  Excel-DNA 入口
  ↓ 调用
Core (internal static, 纯逻辑)         →  零 Excel 依赖，100% 单元测试
  ↓ 依赖
Foundation (共享工具)                   →  InputNormalizer, ElementWiseMapper, OutputWrapper
```

## 构建

```bash
dotnet restore && dotnet build && dotnet test
```
