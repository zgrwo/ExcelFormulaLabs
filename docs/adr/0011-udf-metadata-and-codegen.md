# ADR-0011: UDF 元数据化与源生成

**日期**: 2026-10-07
**状态**: 已确认
**关联**: [ADR-0003](0003-mapover-abstraction.md)（MapOver 抽象）、[AGENTS.md](../../AGENTS.md)（UDF 层仅分发）

## 上下文

`src/**/*Udf.cs` 共 19 个文件、240 个 `[ExcelFunction]` 方法，其中 **238 个是同构体**：

```csharp
[ExcelFunction(Name="STATS.MEAN", Description="Arithmetic mean of a numeric array")]
public static object UDF_STAT_MEAN([ExcelArgument(Name="number1", Description="A range or array of numeric values")] object d)
    => OutputWrapper.WrapError(() => StatsCore.Mean(V(d)));
```

这段样板同时承载四类事实，且每一类都在别处**再写一遍**：

| 事实 | 副本 1 | 副本 2 | 副本 3 |
| :--- | :--- | :--- | :--- |
| 函数名 | `[ExcelFunction(Name=…)]` | `api-reference.md` 表格行 | `user-manual` 小节标题 |
| 语义描述 | `Description=`（英文） | `api-reference.md`（中文） | `user-manual`（中文） |
| 参数名/序 | `[ExcelArgument(Name=…)]` + 形参 | `api-reference.md` 参数列 | 手册语法行 |
| 归属分类 | **不存在** | 模块章节 | 模块章节 |

后果（2026-10-07 评审实测）：

1. **维护成本**：新增一个函数需改动 6–7 处（Core / Udf / 测试 / `verify-manual.py` 手工合并 / api-reference / user-manual / samples 工作簿）。
2. **样板书写的分裂**：同文件混用 `Name = "` 与 `Name="`；`ElementWiseMapper` 转发助手 `M`/`V` 逐字复制 6 份。
3. **可读性**：231 行超过 200 字符，最长 1207 字符（`PhyChemUdf.cs:24`）——真实排障时无法断点阅读。
4. **文档一致性只靠对账**：`verify-docs` 检查 1/2/16/17 是"源码 ↔ 文档"的**事后比对**，两个副本仍会各自漂移（历史已发生 236→999 漂移事故）。
5. **发现性缺失**：240 个函数**全部落在 Excel 插入函数对话框的默认分类**里，没有 `Category`。

## 决策

1. **建立 UDF 元数据单一真源** `udf-metadata/<Module>.json`，逐函数记录：
   `excel`（函数名）、`method`（C# 方法名）、`desc`（`Description` 原文）、
   `category`、`args[]`（name / desc / type / default，含可选标记与原文转义）、
   `expr`（函数体表达式**原文**）、`target`（首个调用目标，供文档检索）。
2. **生成** `src/<Module>/<UdfFile>.g.cs`：由元数据产出属性 + 签名 + 委托体。
   生成文件**入库**（非 `obj/`），以保证既有门禁（`verify-docs` 按 `src/**/*.cs` 计数）
   继续有效，并让 diff 可审查。
3. **生成器用 Python 实现**（`tools/udfgen.py`），**不引入 Roslyn 源生成器项目**。理由见"原因"。
4. **行为不变由构造保证**：抽取阶段即做**逐函数 token 级往返校验**——把元数据重新生成回 C#，
   与原文**删除全部空白后逐字符比对**；不一致的函数**不进入生成集合**，保留手写，
   仅把其属性元数据纳入真源（供文档与门禁使用）。
5. **一致性由"重生成即 diff"门禁强制**：新增 `scripts/verify-udfgen.ps1`，接入
   `ci.yml` 的 redline job：重新生成并与入库文件比对，任何差异即 FAIL（既防手改生成物，
   也防元数据与代码脱节）。**不接入 `pre-commit-check.ps1`**——那会把文档中的
   "提交前红线 6 项"扩成 7 项并引发连串同步；CI 每个 PR 都跑，本地按需手动运行即可。
6. **`Category` 落地**：按模块赋予分类（如 `Statistics`、`Linear Algebra`、`Solve`），
   使 240 个函数在插入函数对话框中分组可寻。
7. **api-reference 的参数列与函数名集合由元数据校验**（后续阶段：直接生成整张表）。

## 原因

1. **为什么不引入 Roslyn 源生成器**：
   - 本项目双 TFM（net48 + net8.0-windows）+ Excel-DNA 打包，引入 analyzer 项目需新增
     `netstandard2.0` 工程、`OutputItemType="Analyzer"` 引用、8 份 `packages.lock.json`
     与 locked-mode 联动——**基础设施风险远高于收益**；
   - `verify-docs` 检查 1/11/17 **按源码文本**统计 `[ExcelFunction]`/`[ExcelArgument]`，
     生成物若落在 `obj/`（Roslyn 默认）则门禁全部失明，需重写数个检查；
   - 项目已有成熟的 Python 工具链先例（`generate-samples.py`、`update_excel_arguments.py`、
     `verify-manual.py`），脚本生成 + 入库存档 + 门禁校验与既有工程模式一致。
2. **为什么生成物入库**：可审查 diff（重构阶段尤其重要）+ 既有门禁零改动即可继续生效 +
   构建不依赖 Python（CI/本地均可只跑 `dotnet build`）。
3. **为什么用"表达式原文"而非语义 DSL**：238 个函数体形态各异（纯委托 / `MapOver` 元素级
   lambda / 多参数广播 / 强制转换 / 常量参数）。用语义 DSL 需要重新建模这些形态，
   **等价性要靠推理**；存表达式原文则等价性**可机械验证**（token 级比对）。
   元数据化的目标是消除**样板与副本**，不是把实现语言换成配置语言。
4. **为什么先校验再生成**：把"能不能无损重建"变成**测量结果**而不是设计假设——
   实测 238/240 可通过，2 个保留手写。

## 约束

- **公共签名与行为零变更**：UDF 名、参数名/序/默认值、返回值、错误语义全部保持。
  唯一有意的行为**新增**是 `Category`（影响插入函数对话框的分组，不影响计算）。
- 生成文件必须标注"机器生成，勿手改"与再生成命令。
- 手写 UDF 的属性（`Name`/`Description`/参数名）必须与元数据一致——由
  `verify-udfgen.ps1` 的"重生成 + 属性比对"覆盖。
- 生成器不改变 `using` 与命名空间约定；生成的类与原手写类同为 `partial`。
- UDF 层"仅分发"红线不变：生成体仍只有一层委托。

## 影响

- **正面**：新增 UDF 由"改 6–7 处"降为"改元数据 1 处 + 加 1 个 Core 方法"；
  `Name=` 写法统一；超长行由生成器的固定格式消除；`api-reference` 参数列与函数集合
  可机械对账；240 个函数获得分类。
- **代价**：多一层生成步骤（改元数据后须跑 `udfgen.py generate`）；生成文件入库使
  `src/` 文件数 +19（须同步 `project-structure.md` 目录树）。
- **需同步**：`AGENTS.md` 与 `docs/governance/project-structure.md` 双树（新增
  `udf-metadata/`、`tools/` 与 17 个 `.g.cs`；`templates/` 模板清单随之更新）、
  `CONTRIBUTING.md`（开发流程加生成步骤）、`skills/excel-dna-addins.md` 与
  `skills/excel-dna-project.md`（声明模板改为元数据形态）、
  新增 `scripts/verify-udfgen.ps1` 并接入 `ci.yml`（见决策 5：不接入 pre-commit）。

## 演进

- **2026-10-07**: 初始确认。后续阶段评估：api-reference 表格由元数据**直接生成**
  （而非校验），并退役 `verify-docs` 检查 17 的双向文本比对。
- **2026-10-07（同日落地）**: api-reference 表体改为**直接生成**——元数据新增三个字段
  `returns`（返回列）、`doc`（中文说明列）、`doc_section`（所属章节），由 `udfgen.py extract-api`
  从既有文档一次性抽取（引导工具，带 `--force` 保护），此后由 `generate-api` 渲染。
  16 张表 / 240 个函数，块级标记；`verify-api` 重渲染比对，已并入 `scripts/verify-udfgen.ps1`。
  **不退役** `verify-docs` 检查 1/2/17：它们是**针对生成物的独立对账**（检查 2 反向遍历源码侧，
  与检查 17 的文档侧遍历互补），对"生成器自身出 bug"这类失效模式仍有价值——
  与生成物同源的校验无法发现生成器错误。实际收益是 240 条中文说明与返回类型从此有了唯一出处。
  排序取舍：表内行序由元数据决定（文件名字典序 + 文件内原序），故首次生成有 4 节 17 行移位；
  实测剔除标记行后**行多重集完全相同**，零文案改动。
