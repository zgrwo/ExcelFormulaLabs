# ADR-0005: SandboxConfig 不可变 record + 一次性初始化

**日期**: 2026-07（v1.0.8）
**状态**: 已确认

## 上下文

`FS.*` 文件系统函数的 `SandboxRoot` 曾是可变的静态属性，多线程环境下存在竞态（一个 UDF 修改沙箱根路径时另一 UDF 正在校验路径），审查发现为安全竞态问题。

## 决策

- `SandboxConfig` 定义为**不可变 record**（net48 由 `IsExternalInit` polyfill 支持）
- 通过 `FileSystemCore.Initialize(config)` **一次性初始化**，启动时设定后不可再变
- 越界访问返回 `#VALUE!`；沙箱支持 NTFS 重解析点（junctions/symlinks）逐段检查
- 默认 `SandboxRoot = null`（无路径限制）；分发给不受信任用户时必须在 `AutoOpen()` 中显式启用

## 原因

1. 不可变 record 从类型层面消除运行时竞态（v1.0.7 审查实证 2 处竞态）
2. 一次性初始化把配置时机前移到启动期，行为可预测
3. 默认开放 + 文档警示：平衡易用性与安全（README「安全」章节）

## 约束

- `Initialize` 只能在启动期调用一次（重复调用抛异常或忽略）
- 沙箱路径校验必须覆盖重解析点逐段检查（防 symlink 逃逸）

## 影响

- 正面：FS 沙箱线程安全、行为确定
- 代价：运行时无法动态调整沙箱（需重启加载项）
- 同步位置：README 安全章节、skills/excel-dna-addins.md、docs/guard-checklist.md

## 演进

- **2026-07-24**: 初始确认（v1.0.8）
