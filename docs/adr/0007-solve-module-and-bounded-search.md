# ADR-0007: SOLVE 模块与有界多起点模式搜索

**日期**: 2026-09-12
**状态**: 已确认

## 上下文

工艺参数反解（给定输出目标反推可调参数）是制造/工艺工程师高频需求，本库此前无对应能力：`REGRESS.*` 只做正向拟合、`LINALG.SOLVE` 只解线性方程组、`PHYCHEM.IDEALGAS` 仅支持单公式解析反解。来源项目「工程分析套件」的 Python `inverse_solve` 引擎（v1.2.6 / feat/inverse-solve）已验证该能力，现迁移为 `SOLVE.*` UDF（见 [实施计划](../plans/2026-09-12-solve-inverse-migration-plan.md)）。

需要决策四件事：模块归属与打包影响、v1 模型范围、优化器选型、角色识别方式。

## 决策

1. **`SOLVE.*` 归属 Analytics 程序集**（新文件 `src/Analytics/SolveCore.cs` + `SolveUdf.cs`）。Excel-DNA 自动扫描 `[ExcelFunction]`，复用现有 Analytics .xll，零 sln/csproj/打包清单改动；用户语义清晰（与 STATS/LINALG 同属科学计算程序集）。
2. **v1 模型范围仅 `linear`/`poly`**（poly = 线性 + 平方 + 两两交互，展开上限 100 项）。明确排除 GPR/GBM（单次拟合成本不适合同步 UDF，且无 ML.NET 依赖）与 `rate` 时间/速率物理模型（v1 将时间列当普通可调列，留待 v2 评估）。
3. **优化器自研「有界多起点模式搜索」**（坐标轮换 + 步长折半，`XorShift64` 确定性采样起点），不使用 MathNet NelderMead：NelderMead 无边界约束、收敛路径依赖初值；自研算法可确定复现（双 TFM 同 seed 同结果）、易与 Python 对拍。
4. **目标优先（target-first）字典序目标函数**：`E(u) = 1e6·F_target(u) + 0.02·Σ((u−u0)/range)²`。其中 `F_target(u)=Σⱼ((ŷⱼ−y*ⱼ)/sⱼ)²`，`u0` = 历史中位数，`range` = 上下界之差。计划文档的原始公式为 `F_target + 0.02·proximity`，但该权重在精确夹具上会把推荐值拉偏约 2%·s（推荐 u=4.02 而非 4.00），与计划自身的验收锚点（推荐 4.0±1e-4、预测 13.0±1e-6）矛盾。字典序处理后：目标可达时精确命中，不可达时取最近可达点，proximity 项仅用于欠定系统在等价解集内选点（越界风险与不可达判定不受影响）。
5. **角色识别用表头前缀约定优先于显式配置**：`Incoming*`/`Variable*`/`Fixed*`/`Output*`（或中文 `来料*`/`可调*`/`变量*`/`固定*`/`输出*`），前缀匹配不区分大小写，未识别列忽略。理由：一个公式出结果（`=SOLVE.INVERSE(A1:C11)`），零配置；显式列角色参数会让公式显著变长。
6. **状态码约定**：Core 数值接口返回 `0` = 可达、`1` = 不可达；UDF 层渲染为 `可达`/`不可达`。可达性 = 目标落在参数边界内采样 2000 点的输出区间（相对容差 `1e-9·量级`），或优化器已达成目标（最大偏差 σ ≤ 1e-6）。

## 原因

1. 复用现有 .xll 使迁移的打包风险为零，符合迁移计划「低平台风险」评估。
2. linear/poly 均为 QR 级成本，同步 UDF 在规模上限（5000 行）内可接受；GPR/GBM 排除避免引入 ML.NET 双 TFM 依赖。
3. 目标优先保证「推荐值 = 用户想达到的工艺目标」这一核心承诺；proximity 仅解决欠定系统解集内的选点（V-2 多参数欠定锚点要求预测达成 ±1e-6）。
4. 前缀约定与 Excel「单变量求解」直觉一致；文档中给出可粘贴示例，无需读文档即可上手（体验验收：粘贴数据到推荐表 ≤3 步）。

## 约束

- `SolveCore` 纯逻辑，零 `ExcelDna` 引用（pre-commit 检查 4）；业务组装在 `SolveUdf`。
- 确定性：优化/采样唯一随机源为 `XorShift64`（项目既有双 TFM 确定性先例），禁止 `System.Random`。
- 数值：回归走 `RegressionCore.FitOLS/FitRidge`（QR，禁正规方程）；判据相对量纲（禁绝对 ε）；NaN/Inf/溢出三路径守卫。
- 规模上限：历史 5000 行 / 请求 200 行 / 特征 50 列 / 可调 20 / 输出 20 / poly 100 项 / max_starts 50 / 单起点评估 4000 次；超限抛异常 → `#VALUE!`。
- `object[,]` 入口 Core 方法含 `bool hasHeaders = true`（表头契约）。
- 数值示例期望硬编码；CrossVal 真对照 C#（Dispatcher + manifest + verify-manual 双通道）。

## 影响

- **正面**：填补「分析→反推→验证」闭环缺口；`SOLVE.INVERSE/PREDICT/QUALITY/EQUATION` 四个 UDF，无新程序集/无新 .xll。
- **负面/代价**：v1 不支持 GPR/GBM/rate，强非线性数据拟合能力弱于 Python 版；poly 高次外推按边界默认历史范围约束，触界推荐需实验确认（文档提示）。
- **需同步**：`docs/specification/api-reference.md`（签名唯一信源）、`specification.md`（模块清单）、`user-manual.md`（SOLVE 章节）、`context.md`（反解/请求行/可达性/最大偏差σ 术语）、`project-structure.md`（文件树）、`README*.md`/`AGENTS.md`/`CONTRIBUTING.md`（散文计数）、`tests/CrossValRunner/Dispatcher.cs` + `test_manifest.json`、`scripts/verify-manual.py`。

## 演进

- **2026-09-12**: 初始确认。v2 如需 GPR/GBM/rate，另立 ADR 评估 ML.NET 或 Python 服务调用；`SOLVE.INVERSE_ASYNC` 仅在 n=5000 同步基准 >3s 时按 `LinalgAsyncUdf.cs` 模式实施。
