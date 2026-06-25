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
  3. 异常过滤器统一 — 裸 `catch{}` = bug，须加 `when` 过滤器；自检 `grep -rn "catch\s*{" src/` 须返回空。`catch (Exception ex) when` 过滤器须统一排除 `OutOfMemoryException`、`StackOverflowException`、`AccessViolationException`。
- **InputNormalizer 转换哨兵契约** — 所有 `ToXxx` 方法遵循 L1-L4：
  - **L1 必须守卫**：类型转换前显式检查 `double.IsNaN(d)` / `double.IsInfinity(d)`，绝不依赖 CLR 未定义转换行为（如 `(long)NaN`）。违反 = bug。
  - **L2 哨兵定义**：不可转换值返回类型对应的零值哨兵，语义为「此单元格不参与计算」：`double`→NaN、`long`→0、`int`→0、`bool`→false、`DateTime`→MinValue、`string`→""。
  - **L3 Excel 信号**：null / DBNull / ExcelEmpty / ExcelError / ExcelMissing → 返回哨兵（语义：「无有效值，跳过」）。已正确实现。
  - **L4 已知取舍**：long/bool 的哨兵（0/false）与真实值不可区分。这是「保持简单 + 不抛异常」的已知取舍。依赖方不应依赖「0/false 表示错误」的语义。若需区分，调用方自行在转换前检查 `IsNumericCell`。
  - **L5 ConvertValue 边界**：`ElementWiseMapper.ConvertValue<T>` 是最后的类型转换边界。6 种已知类型委托给 `InputNormalizer.ToXxx`。未知类型的 `Convert.ChangeType` 失败时：`double`→NaN，其他类型**必须 `throw`**（绝不返回 `default(T)` 静默替代），由 `WrapError` 转为 `#VALUE!`。
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

快速构建（日常开发循环）：

```bash
dotnet restore && dotnet build && dotnet test
```

全量测试（4 轮）命令见 skill。`-c Release` 生成分发用 .xll。
