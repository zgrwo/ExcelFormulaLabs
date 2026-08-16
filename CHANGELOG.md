# Changelog

本文件记录 ExcelFormulaLabs 各版本的变更。

格式基于 [Keep a Changelog](https://keepachangelog.com/zh-CN/1.1.0/)，版本号遵循 [Semantic Versioning](https://semver.org/lang/zh-CN/)。

> 版本一致性：每个 `v*` git tag 必须在本文档有对应条目（`verify-docs.ps1` 强制检查，见规则 [documentation.md](rules/documentation.md)）。

## [2.1.0] - 2026-08-16

### Changed
- ExcelDna.AddIn / Integration / IntelliSense 升级 1.8.0 → 1.9.0（框架修复，重新打包 .xll）
- 测试栈升级：Microsoft.NET.Test.Sdk 18.9.0 / xunit 2.9.3 / xunit.runner.visualstudio 4.0.0 / FluentAssertions 8.10.0 / coverlet.msbuild 10.0.1 / ClosedXML 0.105.1 / BenchmarkDotNet 0.15.8
- GitHub Actions 升级：checkout v7 / setup-dotnet v6 / setup-python v7 / codeql-action v4 / action-gh-release v3 / stale v11

### Added
- CI 覆盖率门禁（coverlet，Foundation ≥75% / Analytics ≥50% / DataToolkit ≥42% 行覆盖）
- CodeQL 安全扫描（security.yml）、dependabot 依赖自动更新（nuget + github-actions）、stale 僵尸清理
- 结构化 Issue 模板（bug/feature/docs/refactor 四类 yml）

### Fixed
- System.Text.Json 8.0.4 → 8.0.5：**修复 CVE-2024-43485（DoS 漏洞）**（net48 兼容；10.x 不支持 netstandard2.0）
- CI 覆盖率门禁误判：显式 ThresholdStat=total（coverlet 默认 minimum 取最低模块）
- verify-docs 对不入库目录（logs/）的误报豁免
- CI 提交规范检查跳过 dependabot 自动提交

## [2.0.1] - 2026-08-06

### Added
- CI 接入红线检查门禁（pre-commit-check.ps1：裸 catch / 自校验 / IntelliSense 隔离 / Core 零依赖 / NaN 守卫 / hasHeaders 契约）
- Release 流水线补全 net48 测试覆盖；项目级 Skills 体系（skills/ 单一信源 + .qoder 本地镜像）

### Fixed
- Release 流水线 NuGet push 通配符在 pwsh 下不展开的问题
- verify-manual.py 交叉验证路径兼容 Debug/Release 构建，更新 RIDGE fallback 期望值
- 发版前全量审查修复（P1×3 / P2×4 / P3×9 + 三视角复审项）
- net48 构建限定支持项目 + ResultSerializer 补 string[] 序列化

## [2.0.0] - 2026-08-01

### Changed
- 主分支由 master 更名为 main，与 GitHub 保持一致
- LINALG.RANK UDF 描述更新，明确 tolerance 为绝对阈值语义
- README 架构图使用用户面向术语 MapOver 替代内部类名
- 用户手册和规格文档版本号升级至 2.0.0

### Added
- verify-docs.ps1 文档一致性验证脚本（PowerShell 版本，10 项检查）

### Fixed
- verify-docs.sh 中 skill 文件路径引用错误（skills/excel-dna-project/skill.md → skills/excel-dna-project.md）

## [1.0.8] - 2026-07-24

### Added
- SandboxConfig 不可变 record + 一次性初始化，消除 SandboxRoot 竞态
- ExceptionFilters.IsCatchable 集中式异常过滤器（25+ 处统一）
- ErrorMessages.resx + ErrorMsg.Get() 错误消息国际化基础设施
- scaffold-udf.ps1 UDF 代码生成器（4 文件模板展开）
- verify-all.ps1 一键 5 步验证门
- BenchmarkDotNet 性能基线项目（MapOver 基准）
- NuGet GitHub Packages 发布管线（release.yml pack+push）
- IsExternalInit polyfill（net48 record 支持）

### Changed
- 项目迁移至 ExcelAddin函数库/ SSOT 主目录
- DecompCache 审计确认线程安全（lock + double-check + LRU-32）
- 硬编码错误消息替换为 ErrorMsg.Get() 资源引用

## [1.0.7] - 2026-07-23

### Fixed
- 审查修复 17 项：安全竞态（SandboxRoot/DecompCache）+ 防御加固 + 文档一致性
- 审查修复 4 项：裸 catch 红线清零 + ANOVA1 分层设计 + CI Release 测试 + SandboxRoot 竞态
- 审查修复 10 项：Soundex NARA 标准 + 竞态防御 + 哨兵守卫 + 架构下沉

## [1.0.6] - 2026-07-12

### Fixed
- SQLite 原生 DLL 改为嵌入资源，消除外部文件依赖
- 打包验证流程加固
- CrossVal 竞态修复

## [1.0.5] - 2026-07-11

### Fixed
- 审查修复 12 项：正确性 + 安全加固 + 一致性
- CI cross-val job 补充 scikit-learn 依赖

## [1.0.4] - 2026-07-09

### Added
- 新增 XLL PowerShell 测试脚本（test-xll.ps1）

### Fixed
- 审查修复 12 项：安全加固 + 正确性 + 代码可读性 + 测试覆盖扩展
- ToBool 默认值哨兵与 Core 默认值失配 — 新增 ToBool(value, defaultValue) 重载

### Changed
- 调整负载测试轮数

## [1.0.3] - 2026-07-05

### Fixed
- 加固多 TFM Release 构建 — DNA 清理改用通配符消除增量污染
- 审查修复 3 项：FitWLS 原始尺度 R² + ToDateTime >=0 + CI net48
- 审查修复 13 项：QR 宽矩阵 + 代理对 + 安全加固 + CI + 验证脚本

## [1.0.2] - 2026-07-04

### Added
- STATS.CORRMATRIX UDF — 多列 Pearson 相关矩阵
- C#↔Python 交叉验证基础设施（CrossValRunner）
- 闭环验证覆盖扩展到 LINALG 模块

### Fixed
- 审查修复 4 项：CorrelationMatrix rows<2 + 自校验消除 + 文档修正
- 审查修复 6 项：net8.0 IntelliSense 排除 + 自校验 + ToDateTime 整数 + SQL 白名单 + Pivot COUNT 类型
- 审查修复 7 项：GasToSTP 守卫 + CSV 负数 + DecompCache 模板 + hasHeaders 统一
- REGRESS.FACTORIMP 返回值 long[] → double[] 修复 Excel-DNA 封送失败
- 全面代码审查修复 10 项 + 16 项

### Changed
- CorrelationMatrix 性能优化 O(N²×R) → O(N×R+N²)
- PivotCore 聚合验证与累积逻辑去重
- 闭环验证从检查清单升级为强制规则

## [1.0.1] - 2026-07-01

### Fixed
- PivotCore 聚合验证漏洞 + 测试加固 18 项
- 手册数值 14 处错误修正
- REGRESS 三函数添加 addIntercept 参数
- 2 处 Bug 修复 + 5 处文档/测试修正

### Changed
- 文档体系重构：明确分工、消除重复、统一格式
- .xll 文件名区分 TFM 和位数

## [1.0.0] - 2026-06-27

### Added
- 初始版本：220+ UDF，14 个模块
- 双 TFM 支持（net48 + net8.0-windows）
- 三层架构：UDF → Core → Foundation
- 1,299+ 单元测试 + Python 交叉验证
- 哨兵契约 L1-L5 系统化
- IntelliSense 自动补全（net48）
- MIT License

[2.1.0]: https://github.com/zgrwo/ExcelFormulaLabs/compare/v2.0.1...v2.1.0
[2.0.1]: https://github.com/zgrwo/ExcelFormulaLabs/compare/v2.0.0...v2.0.1
[2.0.0]: https://github.com/zgrwo/ExcelFormulaLabs/compare/v1.0.8...v2.0.0
[1.0.8]: https://github.com/zgrwo/ExcelFormulaLabs/compare/v1.0.7...v1.0.8
[1.0.7]: https://github.com/zgrwo/ExcelFormulaLabs/compare/v1.0.6...v1.0.7
[1.0.6]: https://github.com/zgrwo/ExcelFormulaLabs/compare/v1.0.5...v1.0.6
[1.0.5]: https://github.com/zgrwo/ExcelFormulaLabs/compare/v1.0.4...v1.0.5
[1.0.4]: https://github.com/zgrwo/ExcelFormulaLabs/compare/v1.0.3...v1.0.4
[1.0.3]: https://github.com/zgrwo/ExcelFormulaLabs/compare/v1.0.2...v1.0.3
[1.0.2]: https://github.com/zgrwo/ExcelFormulaLabs/compare/v1.0.1...v1.0.2
[1.0.1]: https://github.com/zgrwo/ExcelFormulaLabs/compare/v1.0.0...v1.0.1
[1.0.0]: https://github.com/zgrwo/ExcelFormulaLabs/releases/tag/v1.0.0
