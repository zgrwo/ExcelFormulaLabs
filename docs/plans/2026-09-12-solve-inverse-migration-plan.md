# SOLVE.* 工艺参数反解迁移实施方案（inverse_solve → ExcelFormulaLabs）

> **来源方法**：`工程分析套件` v1.2.6+ / 分支 `feat/inverse-solve`（Python 引擎 `inverse_solve`，含 Web/CLI 三入口）
> **目标项目**：ExcelFormulaLabs v2.2.6（C# / Excel-DNA，双 TFM net48 + net8.0，现有 236 个已发布函数）
> **文档日期**：2026-09-12
> **文档状态**：待评审（评审通过后按任务逐项执行）

---

## 一、价值评估：高

| 维度 | 结论 |
|------|------|
| 功能空白 | 全库无"逆向求解/目标反推"能力：`REGRESS.*` 只能正向拟合（且不能对新 X 预测）、`LINALG.SOLVE` 只解线性方程组、`PHYCHEM.IDEALGAS` 只支持单公式解析反解。`SOLVE.*` 填补"给定输出目标反推可调参数"这一空白 |
| 场景匹配 | 工艺调机（"要把不良率压到 4.0，模具温度该设多少？"）是制造/工艺工程师高频需求，而 Excel 正是他们的主战场——免学习新软件、数据就在当前表里 |
| 与现有模块协同 | `REGRESS`（拟合诊断）→ `SOLVE`（反解）→ `DOE`（实验验证）形成闭环，用户可在同一加载项内完成"分析→反推→验证" |
| 迁移成熟度 | Python 版已具备：完整测试防线（数值/不变量/边界/差分）、真实批次验收数据（33 历史 + 11 请求）、方程输出、可达性判定、模型质量门控。行为契约明确，迁移有对照锚点 |
| 差异化 | Minitab/JMP 有 inverse prediction 但绑定其软件；Excel 生态此类 UDF 稀缺。`SOLVE.*` 可作为项目新的宣传点写入 README |
| 复用度 | MathNet（已引用）+ 现有 QR/岭回归内核 + DictToReport + XorShift64 + 异步/测试/交叉验证基础设施，真正新写的只有"表解析 + 预测 + 有界优化 + 报告组装" |

**风险提示**：v1 模型仅 `linear`/`poly`（不含 GPR/GBM），强非线性数据（如阶跃、强交互）拟合能力弱于 Python 版；需在文档中明确适用边界。

## 二、难度评估：中等

| 类别 | 内容 | 评价 |
|------|------|------|
| 可直接复用 | MathNet 线性代数/优化/随机数（`Analytics.csproj:46`）；`RegressionCore.FitOLS/FitRidge`（QR 稳定内核，`RegressionCore.cs:36,249`）；`DictToReport`（`AnalyticsHelpers.cs:84`）；`XorShift64`（`DoeCore.cs:721`）；`*_ASYNC` 模式；CrossVal/manifest/verify-manual 门禁；`scaffold-udf.ps1` | 占比 ~60% |
| 需新建 | ① 表角色解析（按表头前缀识别 incoming/variable/fixed/output）；② 前向模型预测（库中无 Predict）；③ K 折交叉验证与 auto 门控；④ 有界多起点模式搜索优化器；⑤ 可达性采样与结果表组装；⑥ Python 独立对照（scipy/sklearn） | 占比 ~40%，均为纯 Core 逻辑，无 ML.NET 依赖 |
| 平台风险 | 低：方案只加 `src/Analytics/` 内文件（Excel-DNA 自动扫描 `[ExcelFunction]`，**无需改 sln/csproj 结构、不加新 .xll、不改 release 打包清单**）；新前缀 `SOLVE.*` 与 STATS/LINALG 同属 Analytics 程序集 | 低 |
| 门禁成本 | 中：新函数触发 UDF 计数、api-reference 参数比对、README/spec 散文计数、CrossVal manifest、手册示例等同步更新（`verify-docs.ps1` 检查 1/2/9/11/16/17/18） | 可预估 |
| 估算工作量 | 新增/修改约 **2000–2800 行**（Core ~800、测试 ~900、CrossVal ~150、文档 ~400）；单人 3.5–5 人日；CrossVal 对照与文档门禁占一半时间 | 中等 |

## 三、Excel 使用体验设计（易懂易用）

### 3.1 设计原则

1. **零配置优先**：数据表头带角色前缀（`Incoming*`/`Variable*`/`Fixed*`/`Output*` 或中文 `来料*`/`可调*`/`固定*`/`输出*`），函数自动识别角色，不用手填一堆列名。
2. **一个公式出结果**：主函数 `SOLVE.INVERSE` 一次返回完整推荐表（推荐参数 + 预测 + 偏差 + 状态），Excel 365 自动溢出。
3. **延续工程师已有心智**：请求行=可调参数列留空 + 输出列填目标值——与 Excel"单变量求解"的直觉一致，只是支持多参数多目标。
4. **可解释**：`SOLVE.QUALITY` 看模型可信度，`SOLVE.EQUATION` 看方程并可手算核对；错误只返回 `#VALUE!`，不弹窗。
5. **三步上手**：① 表头加前缀 → ② 写一行公式 → ③ 读推荐表。

### 3.2 函数清单（v1：4 个同步 UDF）

| UDF | 语法 | 作用 |
|-----|------|------|
| `SOLVE.INVERSE` | `=SOLVE.INVERSE(data, [request], [bounds], [model], [seed], [max_starts])` | 反解：推荐可调参数 + 预测 + 偏差σ + 可达状态 |
| `SOLVE.PREDICT` | `=SOLVE.PREDICT(data, values, [model])` | 正向预测：给定一组完整参数，算输出（用于人工试算） |
| `SOLVE.QUALITY` | `=SOLVE.QUALITY(data, [model], [seed])` | 模型质量：各输出候选模型的交叉验证 R²/MAE |
| `SOLVE.EQUATION` | `=SOLVE.EQUATION(data, [model])` | 方程：前向方程文本 + 单变量线性反解公式 |

参数 Excel 名（与 `[ExcelArgument]` 严格一致，门禁检查 17）：

| 参数 | 必填 | 默认 | 说明 |
|------|------|------|------|
| `data` | 是 | — | 历史表（含表头）；也可混放请求行（可调列留空） |
| `request` | 否 | 空 | 独立请求表（同表头）；提供后 `data` 视为纯历史 |
| `bounds` | 否 | 历史最小/最大 | 边界表：`[变量名或序号, 下界, 上界]`，两列时按变量顺序 |
| `model` | 否 | `auto` | `auto`/`linear`/`poly` |
| `seed` | 否 | `42` | 优化器与采样种子（可复现） |
| `max_starts` | 否 | `10` | 多起点优化起点数（上限 50） |
| `values`（PREDICT） | 是 | — | 一行或多行"非输出列"取值 |

### 3.3 数据约定（关键易用点）

示例表（可直接粘贴到 A1）：

| IncomingA | VariableU1 | OutputY1 |
|-----------|------------|----------|
| 1 | 2 | 9.5 |
| 2 | 4 | 16.5 |
| 3 | 6 | 23.5 |
| …（历史行：可调列+输出列都有值） | | |
| 10 | （留空） | 13 |

- 前缀匹配不区分大小写（`Variable*`、`变量*`、`可调*` 均可）；未识别前缀的列（如日期、批次号）自动忽略。
- **历史行**：可调列与输出列全为数值；**请求行**：任意可调列为空、至少一个输出目标有值。
- 固定参数列留空时按历史中位数处理；来料列留空 → `#VALUE!` 并说明（来料是已知条件）。
- 请求行可调列若填了值，该值作为寻优初值（不报错、不静默丢弃）。

### 3.4 端到端示例

```
=SOLVE.INVERSE(A1:C11)
```

返回（自动溢出）：

| 请求行 | VariableU1 | OutputY1预测 | 最大偏差σ | 状态 |
|--------|-----------|--------------|-----------|------|
| 数据第11行 | 4.000 | 13.000 | 0.00 | 可达 |

模型质量核查：

```
=SOLVE.QUALITY(A1:C11)
```
→ 输出列 / 候选 / CV方案 / CV_R2 / CV_MAE / 选用（auto 在 linear、poly 中按 CV R² 选优）。

方程手算核对：

```
=SOLVE.EQUATION(A1:C11)
```
→ `OutputY1 = 2 + 0.5*IncomingA + 1.5*VariableU1`（与上例精确一致，可直接反算 u=(13−2−5)/1.5=4）。

### 3.5 输出布局契约（写入 api-reference）

**SOLVE.INVERSE**（`object[,]`，含表头行）：

```
[请求行, <可调列名…>, <输出列名>预测…, 最大偏差σ, 状态]
```
- `请求行`：data 内请求行标 `数据第N行`（N 为区间内 1-based 数据行号）；request 表标 `请求N`。
- `状态`：`可达` / `不可达`（目标是否落在参数边界内的采样可达区间；容差=相对 1e-9×量级）。
- `最大偏差σ` = maxⱼ |ŷⱼ−y*ⱼ| / sⱼ（sⱼ=历史输出样本标准差，0 时取 1）。

**SOLVE.QUALITY**：`[输出, 候选, CV方案, CV_R2, CV_MAE, 选用]`；`CV方案` = `5折`（n≥20）或 `LOO`（n<20），`选用` = `是`/`否`。

**SOLVE.EQUATION**：`[输出, 类型, 表达式]`；`类型` = `前向方程` 或 `反解公式`；多变量/高次无解析反解时仅给前向方程。系数格式 `G6`，InvariantCulture。

**SOLVE.PREDICT**：N×M `double[,]`（行=输入行，列=输出列顺序），无表头。

### 3.6 规模上限（防御，超限 `#VALUE!` 并登记错误表）

| 项 | 上限 |
|----|------|
| 历史行数 | 5000 |
| 请求行数 | 200 |
| 特征列（来料+可调+固定） | 50 |
| 可调参数 | 20 |
| 输出 | 20 |
| poly 展开项 | 100（超出时 auto 跳过 poly；显式 poly 报错） |
| max_starts | 50 |
| 单起点评估次数 | 4000 |

## 四、v1 范围

**做**：linear / poly（二次含交互）、auto CV 门控、多输出、多可调参数、边界约束、多起点寻优、可达性判定、方程输出、质量诊断、确定性种子。
**不做（明确排除）**：GPR/GBM（无 ML.NET，且单次拟合成本不适合同步 UDF）；时间/速率物理模型 `rate`（v1 可将时间列当普通可调列处理；rate 模型留待 v2 评估）；Python 版的多起点 DE 完全复刻（改为有界模式搜索，跨验证以"达成目标/目标函数一致"为判据）。

## 五、目标项目落点（文件清单）

| 文件 | 动作 |
|------|------|
| `src/Analytics/SolveCore.cs` | 新建：表解析/模型/交叉验证/优化/可达/报告（纯逻辑，零 ExcelDna） |
| `src/Analytics/SolveUdf.cs` | 新建：4 个 `[ExcelFunction]` 包装（仅分发） |
| `tests/Analytics.Tests/SolveCoreTests.cs` | 新建：数值/边界/确定性测试 |
| `tests/Analytics.Tests/SolveUdfTests.cs` | 新建：注册契约/返回布局/错误透传 |
| `tests/CrossValRunner/Dispatcher.cs` | 修改：注册 3 个 SolveCore 方法 |
| `tests/CrossValRunner/test_manifest.json` | 修改：sharedData 夹具 + tests 条目 |
| `scripts/verify-manual.py` | 修改：SOLVE 手册示例 + 独立 Python 对照 |
| `docs/adr/0007-solve-module-and-bounded-search.md` | 新建 ADR |
| `docs/specification/api-reference.md` | 修改：新增 SOLVE 模块表（签名唯一信源） |
| `docs/specification/specification.md` | 修改：模块清单加 SOLVE 行 |
| `docs/user-manual/user-manual.md` | 修改：SOLVE 章节（可粘贴示例 + 结果解读） |
| `docs/governance/context.md` | 修改：术语（反解/请求行/可达性/最大偏差σ） |
| `docs/governance/project-structure.md` | 修改：目录树登记新文件 |
| `README.md` / `README.en.md` / `AGENTS.md` / `CONTRIBUTING.md` | 修改：模块速览 + 散文 UDF 计数（门禁检查 16） |
| `src/Analytics/Analytics.csproj` | 修改：`<Description>` 的"92 个科学计算函数"计数 |
| `CHANGELOG.md` | 发版时修改（任务不含，另行发版流程） |

## 六、全局约束

- 分层：`SolveCore` 纯逻辑不引用 `ExcelDna.Integration`；`SolveUdf` 不含业务逻辑；错误经 `OutputWrapper.WrapError` 转 `#VALUE!`；异常消息英文（与现有 Core 一致），文档中文。
- 数值三律：判据必须相对（量纲无关）；回归/岭回归走 QR（复用 `RegressionCore`，禁止正规方程）；NaN/Inf/溢出三路径守卫。
- `object[,]` 入口 Core 方法必须含 `bool hasHeaders = true`（表头契约）。
- 确定性：优化/采样使用 `XorShift64` + `seed`，跨 TFM 结果一致（不得用 `System.Random`）。
- 所有新代码必须有测试；数值示例期望值必须硬编码（禁自校验）；CrossVal 必须真对照 C#。
- 命令（Windows PowerShell）：
  - 测试：`dotnet test tests/Analytics.Tests`（双 TFM 全跑）
  - 文档门禁：`powershell -File scripts/verify-docs.ps1`
  - 红线检查：`powershell -File scripts/pre-commit-check.ps1`
  - 全量验证：`powershell -File scripts/verify-all.ps1`

---

## 七、实施任务

### Phase 0：决策与契约冻结

#### Task 0.1：ADR-0007（模块放置 + 优化器 + 范围）

**Files:**
- Create: `docs/adr/0007-solve-module-and-bounded-search.md`（按 `docs/adr/adr-template.md`）

**决策内容（写入 ADR）：**
1. 新前缀 `SOLVE.*` 归属 Analytics 程序集（复用现有 .xll，零打包改动）；理由是用户语义清晰优先于免改 sln。
2. v1 模型范围 linear/poly；GPR/GBM/rate 排除及理由。
3. 优化器自研"有界多起点模式搜索"而非 MathNet NelderMead：NelderMead 无边界约束、参数化后收敛路径依赖初值；自研算法 60 行内、可确定复现、易与 Python 对拍。
4. 角色识别用表头前缀（约定优先配置）。

- [ ] 写 ADR（含备选方案与后果）
- [ ] 在 `docs/governance/project-structure.md` adr 树登记
- [ ] 提交：`docs(adr): 新增 0007 SOLVE 模块与有界模式搜索决策`

#### Task 0.2：生成骨架与确认双 TFM 编译

- [ ] 运行 `powershell -File scripts/scaffold-udf.ps1 -Module Analytics -Name Solve -Prefix SOLVE`
- [ ] 将生成件改名为 `SolveCore.cs`/`SolveUdf.cs`/`SolveCoreTests.cs`，删除 CrossVal 残件（按脚手本文档说明合并到 `verify-manual.py` 后删除）
- [ ] `dotnet build` 确认双 TFM 通过
- [ ] 提交：`chore(solve): 生成 SOLVE 模块骨架`

---

### Phase 1：SolveCore（纯逻辑）

#### Task 1.1：表解析与角色识别

**Files:**
- Create/Modify: `src/Analytics/SolveCore.cs`
- Test: `tests/Analytics.Tests/SolveCoreTests.cs`

**Interfaces:**
- Produces：
```csharp
internal enum SolveRole { Incoming, Variable, Fixed, Output }
internal sealed class SolveSchema
{
    public string[] Headers;              // 原始列名
    public int[] Incoming, Variable, Fixed, Output; // 列索引
    public List<int> HistoryRows, RequestRows;      // 数据行索引（0-based，不含表头）
}
// 识别规则：前缀匹配 OrdinalIgnoreCase；未识别列忽略；
// 行分类：可调列全为数值→历史行；任一可调列为空/非数值→请求行。
internal static SolveSchema ParseSchema(object[,] data, bool hasHeaders = true);
internal static bool IsBlank(object cell);   // null / ExcelEmpty / "" / 空白串
```

**验证锚点：**
- [ ] 失败测试：`ParseSchema_ClassifiesRowsAndRoles`
```csharp
object[,] data = {
    {"IncomingA","VariableU1","OutputY1","备注"},
    {1.0, 2.0, 9.5, "x"},
    {10.0, null, 13.0, null},          // 请求行
};
var s = SolveCore.ParseSchema(data);
s.Incoming.Should().Equal(0); s.Variable.Should().Equal(1); s.Output.Should().Equal(2);
s.HistoryRows.Should().Equal(0); s.RequestRows.Should().Equal(1);
```
- [ ] 失败测试：中文前缀（`来料温度`/`可调*`/`输出*`）等价识别；无 `Variable` 列时 `ArgumentException`（消息含 "Variable"）；无输出列同理
- [ ] 失败测试：请求行来料列留空 → `ArgumentException`
- [ ] 实现最小逻辑 → 全部通过 → 提交：`feat(solve): 表角色解析与请求行分类`

#### Task 1.2：前向模型拟合与预测（linear/poly）

**Files:**
- Modify: `src/Analytics/SolveCore.cs`
- Test: `tests/Analytics.Tests/SolveCoreTests.cs`

**Interfaces:**
```csharp
internal sealed class SolveModel
{
    public string Kind;            // "linear" | "poly"
    public string[] BaseNames;     // 特征名（incoming+variable+fixed）
    public int[][] Powers;         // 每展开项对基础特征的指数向量（linear=单位；poly=1/平方/两两交互）
    public double[] Coef;          // 原始单位系数（已反标准化）
    public double Intercept;       // 原始单位截距
}
internal static SolveModel FitModel(double[][] X, double[] y, string model);
internal static double Predict(SolveModel m, double[] x);
internal static int ExpandedTermCount(int baseCount, string model); // poly = k + k(k+1)/2
```
- 实现要点：poly 展开（`k + k(k+1)/2` 项）→ 列标准化（mean/sd，sd=0→1）→ `RegressionCore.FitRidge`（poly，λ=1e-5）或 `FitOLS`（linear）→ 按 `β_t/scale_t`、`b − Σ β_t·mean_t/scale_t` 反标准化到原始单位。
- 守卫：`X` 行数 < 展开项+1 → 报错；展开项 > 100 → 报错（消息含 "poly" 与上限）；非有限输入用 `NumericGuard.AgainstNonFinite`。

**验证锚点（手算期望）：**
- [ ] 失败测试：linear 精确数据 `y = 2 + 0.5a + 1.5u`（10 行）→ `Predict` 与原值差 ≤1e-10，`Coef≈{0.5,1.5}`、`Intercept≈2`
- [ ] 失败测试：poly 精确数据 `y = 1 + u + 0.5u²`（u=-3..3）→ `Powers.Length==3`，`Predict` 差 ≤1e-6
- [ ] 失败测试：poly 展开项超限（如 15 特征 → 120 项 > 100）→ `ArgumentException`
- [ ] 实现 → 通过 → 提交：`feat(solve): linear/poly 模型拟合与预测`

#### Task 1.3：交叉验证与 auto 门控

**Files:**
- Modify: `src/Analytics/SolveCore.cs`
- Test: `tests/Analytics.Tests/SolveCoreTests.cs`

**Interfaces:**
```csharp
// 返回 (scheme, r2, mae)：scheme = "5折"（n>=20）或 "LOO"（n<20，n>=5）
internal static (string Scheme, double R2, double Mae) CrossValidate(
    double[][] X, double[] y, string model, long seed);
// auto：linear 与 poly 各算 CV；poly 超展开上限时跳过；R² 高者选用（差值 <1e-9 时选 linear）
internal static (SolveModel Model, string Chosen, string Scheme, double R2, double Mae, bool PolySkipped)
    FitAuto(double[][] X, double[] y, long seed);
```
- 实现要点：`XorShift64` 生成确定洗牌；5 折轮流留出；每折在训练折上 `FitModel` 并在留出折预测；汇总 out-of-fold 预测算 R² 与 MAE；每折训练行数必须 > 展开项数，否则回退 LOO 或报错。

**验证锚点：**
- [ ] 失败测试：精确线性数据 n=25 → `Scheme=="5折"`、`R2≥0.999`、`Mae≤1e-6`
- [ ] 失败测试：n=10 → `Scheme=="LOO"`；n=4 → `ArgumentException`
- [ ] 失败测试：小样本 poly 展开超限时 `FitAuto` 返回 `PolySkipped==true` 且 `Chosen=="linear"`
- [ ] 实现 → 通过 → 提交：`feat(solve): 交叉验证与 auto 模型门控`

#### Task 1.4：反解优化、可达性与结果表

**Files:**
- Modify: `src/Analytics/SolveCore.cs`
- Test: `tests/Analytics.Tests/SolveCoreTests.cs`

**Interfaces:**
```csharp
// 单输出/多输出核心（供 CrossVal 直调）：
// X: 历史特征 n×k；Y: 历史输出 n×m；variableCols: 可调列索引；
// requests: r×k（可调列值为初值或 NaN）；targets: r×m（NaN=该输出无目标）；
// bounds: v×2；返回 r×(v+m+2)：推荐值… 预测值… 最大偏差σ 状态(1=可达,0=不可达)
internal static double[,] SolveInverse(
    double[][] X, double[][] Y, int[] variableCols, double[][] requests, double[][] targets,
    double[][] bounds, string model, long seed, int maxStarts);
```
- 目标函数：`F(u)=Σⱼ((ŷⱼ(u)−y*ⱼ)/sⱼ)² + 0.02·Σₖ((uₖ−u0ₖ)/rangeₖ)²`；`sⱼ`=输出样本 sd（0→1）；`u0ₖ`=历史中位数；`rangeₖ`=上界−下界（0→跳过该项）。
- 优化器：起点 = 首点历史中位数，其余在边界内均匀采样（XorShift64）；局部搜索=坐标轮换+步长折半（`step=(hi−lo)/4`，收缩至 `1e-9·range` 或 4000 次评估）；越界用 clamp。
- 可达性：额外均匀采样 2000 点，输出区间 `[min,max]`；`tol = 1e-9·max(|min|,|max|,|max−min|,1e-300)`（**相对容差，禁用绝对 ε**）。
- 结果表组装为 `object[,]`（表头 + r 行，列序见 §3.5）；请求行/输出/参数名由调用方提供（UDF 层组装中文表头；Core 返回数值部分 + 由 `Inverse` 包装补表头）。

**验证锚点（关键回归）:**
- [ ] 失败测试：线性夹具请求 `a=10, target=13`，边界 `u∈[2,20]` → 推荐 4.0（±1e-4）、预测 13.0（±1e-6）、状态 0（可达）
- [ ] 失败测试：目标 1000 → 状态 1（不可达）
- [ ] 失败测试：微尺度输出（y~1e-12）目标超可达上限 50% → 必须"不可达"（移植源审查 R-2 回归）
- [ ] 失败测试：多可调参数欠定（y 同时依赖 u1,u2）→ 预测达成目标（±1e-6），不要求参数唯一
- [ ] 失败测试：同 seed 两次调用结果逐元素一致；固定列留空（NaN）→ 历史中位数参与特征；请求初值 u=3.99 → 结果仍收敛到 4.0
- [ ] 实现 → 通过 → 提交：`feat(solve): 有界多起点反解与可达性判定`

---

### Phase 2：UDF 层

#### Task 2.1：四个 UDF 与注册契约

**Files:**
- Create/Modify: `src/Analytics/SolveUdf.cs`
- Test: `tests/Analytics.Tests/SolveUdfTests.cs`

**Interfaces（`[ExcelArgument]` 名称即 api-reference 参数列，逐字一致）：**

```csharp
[ExcelFunction(Name = "SOLVE.INVERSE", Description = "Invert process settings to hit output targets; returns a recommendation table.")]
public static object UDF_SOLVE_INVERSE(
    [ExcelArgument(Name="data", ...)] object data,
    [ExcelArgument(Name="[request]", ...)] object request = null,
    [ExcelArgument(Name="[bounds]", ...)] object bounds = null,
    [ExcelArgument(Name="[model]", ...)] object model = null,
    [ExcelArgument(Name="[seed]", ...)] object seed = null,
    [ExcelArgument(Name="[max_starts]", ...)] object maxStarts = null)
    => OutputWrapper.WrapError(() => SolveCore.Inverse(
        InputNormalizer.NormalizeTo2D(data)!, Norm2D(request), Norm2D(bounds),
        InputNormalizer.ToString(model) ?? "auto",
        seed==null||seed is ExcelMissing ? 42L : InputNormalizer.ToLong(seed),
        maxStarts==null||maxStarts is ExcelMissing ? 10 : InputNormalizer.ToInt32(maxStarts)));

// SOLVE.PREDICT(data, values, [model])  → double[,] N×M
// SOLVE.QUALITY(data, [model], [seed])  → object[,] 表
// SOLVE.EQUATION(data, [model])         → object[,] 表
```
- `SolveCore.Inverse` 是面向 UDF 的包装：`ParseSchema` → 收集请求行（data 空白行 + request 表）→ 逐输出 `FitAuto` → `SolveInverse` → 返回带中文表头的 `object[,]`；可调列内置上/下界解析（`bounds` 行 `[变量名或1-based序号, lo, hi]`）。
- 初值、固定列中位数、来料缺失报错等行为在此层组装（Core 数值接口保持纯粹）。

**验证锚点：**
- [ ] 失败测试：UDF 返回 `object[,]`，第 0 行 = `{请求行, VariableU1, OutputY1预测, 最大偏差σ, 状态}`；行数=请求数+1
- [ ] 失败测试：空 data / 无 Variable 列 / 无请求行 → `ExcelError.Value`（`WrapError` 契约）
- [ ] 失败测试：`SOLVE.QUALITY` 表头 = `{输出, 候选, CV方案, CV_R2, CV_MAE, 选用}`；`SOLVE.PREDICT` 返回 1×1
- [ ] `dotnet test tests/Analytics.Tests` 双 TFM 全绿
- [ ] 提交：`feat(solve): SOLVE.INVERSE/PREDICT/QUALITY/EQUATION 四个 UDF`

---

### Phase 3：交叉验证（Python ↔ C#）

#### Task 3.1：Dispatcher 注册与 manifest 夹具

**Files:**
- Modify: `tests/CrossValRunner/Dispatcher.cs`
- Modify: `tests/CrossValRunner/test_manifest.json`

**Interfaces / 步骤：**
- [ ] Dispatcher 新增（`Kwarg` 处理可选参数，参考 `FitOLS` 注册）：
```csharp
Register("SolveCore", "FitModel", (a,k) => SolveCore.FitModel(ToDouble2D(a[0]), ToDouble1D(a[1]), ToString(a[2])));
Register("SolveCore", "CrossValidate", (a,k) => SolveCore.CrossValidate(ToDouble2D(a[0]), ToDouble1D(a[1]), ToString(a[2]), Kwarg(k,"seed",42L)));
Register("SolveCore", "SolveInverse", (a,k) => SolveCore.SolveInverse(...));
```
- [ ] `sharedData` 新增夹具（与 C# 测试同源，硬编码）：
  - `solve_X_a` = 10×2 `[[1,2],[2,4],…,[10,20]]`，`solve_y_a` = `[9.5,16.5,…,37]`（y=2+0.5a+1.5u）
  - `solve_request_a`（a=10, u=NaN? JSON 用 `null` + 约定 NaN；若转换不支持则用初值 3.99 表示"请求行"）
  - `solve_target_a` = `[13]`，`solve_bounds_a` = `[[2,20]]`，微尺度夹具一套（y×1e-12）
- [ ] `tests` 新增条目（`id` 用 `SOLVE.FitModel` 等，`tolerance` 按量级 1e-8）：
```json
{"id":"SOLVE.FitModel","module":"SOLVE","coreClass":"SolveCore","coreMethod":"FitModel",
 "args":[{"ref":"solve_X_a"},{"ref":"solve_y_a"},{"lit":"linear"}],"tolerance":1e-9}
```
- [ ] `dotnet build tests/CrossValRunner && dotnet run --project tests/CrossValRunner` 能输出新条目 JSON
- [ ] 提交：`test(solve): CrossVal 注册与夹具`

#### Task 3.2：Python 独立对照与手册示例

**Files:**
- Modify: `scripts/verify-manual.py`
- Modify: `docs/user-manual/user-manual.md`

**步骤：**
- [ ] 在 `verify-manual.py` 新增 `solve_*` 独立实现（顶部已导入 `sklearn`/`scipy`）：
```python
def py_solve_fit(X, y, model):          # numpy + sklearn（独立实现，不复用工程分析套件代码）
def py_solve_inverse(X, y, var_idx, req, target, bounds, seed=42):
    # 1) sklearn LinearRegression / PolynomialFeatures+Ridge 拟合
    # 2) scipy.optimize.minimize(method="Nelder-Mead", bounds=bounds) 最小化同目标函数
    # 3) 返回推荐参数与达成预测
```
- [ ] 新增 `cross_check` 断言（禁止 `check(name, X, X)`）：
  - `SOLVE.FitModel` linear/poly：系数与截距对拍（`tol=1e-8`）
  - `SOLVE.CrossValidate`：5 折 R² 与 MAE（`tol=1e-6`；折划分不同时对比"R² 量级"或用相同 seed 的解释性断言并在注释声明）
  - `SOLVE.SolveInverse`：推荐参数（单变量闭式解直接对拍，`tol=1e-4`）+ 达成预测（`tol=1e-6`）+ 微尺度可达 flag（布尔完全一致）
- [ ] user-manual 增加 SOLVE 章节：**可粘贴示例数据**（§3.3 表）+ 三个公式 + 返回表截图占位（文字表格）+ 结果解读（模型质量决定可信度、参数触界信号），示例期望值由 Python 复算写死
- [ ] `python scripts/verify-manual.py` → 全绿，SOLVE 条目进入 manual + cross 双通道
- [ ] 提交：`test(solve): Python 独立对照与手册示例`

---

### Phase 4：文档与门禁收口

#### Task 4.1：api-reference / specification / README / 术语

**Files:**
- Modify: `docs/specification/api-reference.md`（新增 SOLVE 模块表：4 个函数的参数/返回/错误行为）
- Modify: `docs/specification/specification.md`（模块清单加 `SOLVE.* | 4 | Analytics | 工艺参数反解/正向预测/模型诊断/方程`）
- Modify: `docs/governance/context.md`（反解、请求行、可达性、最大偏差σ）
- Modify: `README.md` 与 `README.en.md`（模块速览行）
- Modify: `src/Analytics/Analytics.csproj`（`<Description>` 计数 92 → 96）

- [ ] 参数名/顺序与源码逐字比对（verify-docs 检查 17）
- [ ] `powershell -File scripts/verify-docs.ps1` 仅剩"散文计数"类 FAIL → 进入 Task 4.3

#### Task 4.2：project-structure 登记

- [ ] `docs/governance/project-structure.md`：Analytics 树补 `SolveCore.cs`/`SolveUdf.cs`；测试树补两个测试文件；adr 树补 0007
- [ ] 提交（与 4.1 合并）：`docs(solve): SOLVE 模块文档与结构登记`

#### Task 4.3：散文计数与全量门禁

- [ ] 按 `verify-docs.ps1` 输出逐条修正 `N UDF`/`N 个 UDF`/分数形式计数（AGENTS.md、README、CONTRIBUTING、specification 等；CHANGELOG 历史行按门禁规则处理）
- [ ] 运行完整 6 步：
```
powershell -File scripts/verify-all.ps1
# ① verify-docs ② Build ③ dotnet test ④ CrossVal(verify-manual.py) ⑤ pre-commit-check ⑥ Release build
```
- [ ] `scripts/test-xll.ps1` 本地加载冒烟（Excel 可用时）
- [ ] 提交：`docs(solve): 同步 UDF 计数与全量门禁`

---

### Phase 5（可选，v1.1）

#### Task 5.1：`SOLVE.INVERSE_ASYNC`

- [ ] 按 `LinalgAsyncUdf.cs` 模式包装同一 Core（`ExcelAsyncUtil.Run` + 128 位内容哈希键；M/V 转换在调用线程完成）
- [ ] 异步注册契约测试入 `AsyncUdfTests.cs`；UDF 计数 +1 同步文档
- [ ] 仅当 n=5000 基准同步耗时 >3s 时实施

#### Task 5.2：示例工作簿与基准

- [ ] 用 openpyxl 生成 `templates/SolveDemo.xlsx`（Sheet1 历史+请求、Sheet2 三个公式与说明），并登记目录树
- [ ] BenchmarkDotNet 增加 `SolveInverse` n=1000 基线（`benchmarks/`）
- [ ] 基准目标：n=1000、3 可调、5 请求 < 2s（net8）

## 八、验收标准

| 项 | 标准 |
|----|------|
| 功能 | §3.4 三步示例在 Excel 中一次成功；推荐值与手工/闭式解一致；不可达目标标注正确 |
| 数值 | 精确夹具误差 ≤1e-6；微尺度可达判定正确（R-2 回归）；同 seed 完全可复现 |
| 测试 | `dotnet test` 双 TFM 全绿；CrossVal SOLVE 条目全部真对照（无 SKIP） |
| 门禁 | verify-docs 19/19、pre-commit 6/6、verify-manual 双通道全绿 |
| 体验 | 从粘贴数据到得到推荐表 ≤3 步；表头前缀即唯一约定；无需读写文档即可读懂返回表 |
| 性能 | n=5000/10 特征/5 请求同步完成 ≤5s（net8），超限显式 `#VALUE!` |

## 九、风险与对策

| 风险 | 对策 |
|------|------|
| 优化器陷入局部最优 | 多起点（默认 10）+ 可达性采样表让用户看到全局范围；文档说明触界意义 |
| poly 外推危险 | 边界默认历史范围；推荐值触界时在文档中提示"需实验确认" |
| CV 折划分与 Python 不一致导致对拍失败 | 对拍以"单变量闭式解/达成预测/目标函数"为主判据，R² 用量级断言并注释说明 |
| Excel 同步 UI 阻塞 | 规模上限 + 可选 async；默认 auto 在 n≤5000 下成本可控（linear/poly 均为 QR 级） |
| 文档门禁反复红 | 先跑 verify-docs 拿到全量 FAIL 清单再一次性修复；计数只在 api-reference 改 |
| net48/net8 数值差异 | 唯一随机源 `XorShift64`（项目已有跨 TFM 确定性先例）；CI 双 TFM + cross-val 覆盖 |

## 十、假设与待决策

1. **假设**：v1 不迁移 GPR/GBM/rate；若业务强依赖非线性/时间速率模型，另立 v2 方案（可评估 ML.NET 或调用 Python 服务的可行性）。
2. **假设**：Excel 用户可接受"表头前缀"约定；若不可接受，备选方案是增加显式角色参数（`incoming_cols` 字符串），但公式显著变长，不推荐。
3. **待决策**：`SOLVE.*` 前缀最终命名（备选 `INVERSE.*`）；默认模型 `auto` 是否改为 `linear`（前者更准、后者更快）。
4. **待决策**：示例工作簿是否入库（二进制文件）或改为文档内可粘贴数据 + openpyxl 生成脚本。
