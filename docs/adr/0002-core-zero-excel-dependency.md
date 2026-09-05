# ADR-0002: Core 层零 Excel 依赖（UDF → Core → Foundation 三层）

**日期**: 2026-07（来源 commit 1d06e3f，后由 VibeCodingTemplate 收录为通用经验）
**状态**: 已确认

## 上下文

初版 UDF 直接在入口方法中编写业务逻辑，导致：无法脱离 Excel 进行单元测试、逻辑与封送/线程模型耦合、统计核心难以与 Python 独立实现交叉验证。

## 决策

强制三层结构：

- **UDF 层**（public static，`[ExcelFunction]`）：仅分发与适配，不含业务逻辑
- **Core 层**（internal static）：纯逻辑，**禁止引用 `ExcelDna.Integration`**
- **Foundation 层**：共享工具（InputNormalizer / ElementWiseMapper / OutputWrapper）

UDF → Core 的分发统一走 MapOver 三变体（见 ADR-0003）。

## 原因

1. Core 零依赖 → 可独立单元测试（全部测试方法在无 Excel 环境下运行）
2. 统计函数可与 Python numpy/scipy 独立实现比对（闭环验证红线 6）
3. 分层边界可被 CI 静态检查（pre-commit-check.ps1 检查 4：`*Core.cs` 内出现 `ExcelDna` 即 FAIL）

## 约束

- 禁止跨层直接调用或反向依赖（Core 引用 UDF 层 = 红线违规）
- UDF 文件（`*Udf.cs`）不得出现 `internal static` 业务实现

## 影响

- 正面：测试性、可验证性、可移植性大幅提升
- 代价：每个新函数多一层分发样板（由 MapOver 抽象与 scaffold-udf.ps1 模板消化）
- 同步位置：AGENTS.md 架构分层、skills/excel-dna-project.md

## 演进

- **2026-07**: 初始确认（v1.0.0）
- **2026-08**: 被 VibeCodingTemplate 提炼为跨项目通用分层规则
