# ADR-0001: 双 TFM 目标（net48 + net8.0-windows）

**日期**: 2026-07（项目初始）
**状态**: 已确认

## 上下文

Excel 加载项需要覆盖两类用户：Win10/11 自带 .NET Framework 4.8（零安装即可用）与愿意安装 .NET 8 运行时的性能敏感用户。单框架选择会损失其中一类用户。

## 决策

所有 src 项目采用双目标框架：

- `Foundation`：`net8.0;net48`
- `Analytics` / `DataToolkit`：`net8.0-windows;net48`

每个模块产出两套 `-packed.xll`（net48 免安装版 + net8.0 高性能版），发布时同时提供。

## 原因

1. net48 是 Win10/11 系统自带运行时，用户零安装成本（覆盖最大用户群）
2. net8.0 性能更优，且不受 .NET Framework 生命周期限制
3. 双实现共享同一 Core 层源码，无分叉维护成本

## 约束

- Public 签名/UDF 参数与返回值在两个 TFM 下必须完全一致
- `#if NET48` 条件编译仅限内部实现，禁止出现在签名/参数上
- 新增 NuGet 依赖前必须确认双 TFM 可用（禁止单框架依赖）
- net8.0 构建禁止添加 IntelliSense 代码（Excel-DNA Issue #343；AGENTS.md 红线规则 3）

## 影响

- 正面：免安装 + 高性能双覆盖；CI 必须双 TFM 全量测试（ci.yml test/test-net48/release-build）
- 代价：每次发版构建产物翻倍（4 个 xll：2 模块 × 2 TFM）
- 同步位置：README 安装章节、skills/excel-dna-addins.md、release.yml 产物表

## 演进

- **2026-07**: 初始确认（v1.0.0）
