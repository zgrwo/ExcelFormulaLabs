# ADR-0003: MapOver 三变体抽象（标量/数组/多参数广播）

**日期**: 2026-07
**状态**: 已确认

## 上下文

Excel 传入参数形态复杂：标量、一维数组、二维区域、多参数尺寸不匹配。若每个 UDF 手写循环，236 个函数将产生 ~3000 行重复样板代码，且错误处理（哨兵、错误透传）极易不一致。

## 决策

在 Foundation 的 `ElementWiseMapper` 中实现 MapOver 三变体，统一分发：

- `MapOver`：单参数，标量 → 标量，数组 → 逐元素数组
- `MapOverFlat`：展平二维输入
- `MapOverMulti`：多参数广播（标量广播到数组尺寸，等长数组逐元素配对，尺寸不匹配 → `#VALUE!`）

配套 `AnalyticsHelpers.M()/V()/D()` 与 `OutputWrapper.WrapError` 异常 → Excel 错误值转换。

## 原因

1. 消除重复样板（~3000 行），错误语义单点定义
2. 用户文档中统一描述广播行为（README「多参数广播」章节）
3. 单元测试聚焦三变体本身，而非每个 UDF 重复测分发

## 约束

- UDF 入口必须选择正确的 MapOver 变体（skills/excel-dna-project.md 选型表）
- 尺寸不匹配统一返回 `#VALUE!`（不抛异常）

## 影响

- 正面：新 UDF 骨架由 scaffold-udf.ps1 自动生成，样板归零
- 代价：ElementWiseMapper 是全项目最关键类，修改需全量回归（BenchmarkDotNet 有 MapOverBenchmarks 基线）
- 同步位置：README 使用模式、skills/excel-dna-project.md、templates/NewModule/{Name}Udf.cs.template

## 演进

- **2026-07**: 初始确认（v1.0.0）
