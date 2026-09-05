# ADR-0004: 哨兵契约 L1-L5 优于抛异常（静默传播阻断）

**日期**: 2026-07（来源：costsuite 性能回归教训，模板收录为通用经验）
**状态**: 已确认

## 上下文

数值计算热路径中 try-catch 开销显著；且 Excel UDF 抛异常会导致整格错误中断，用户体验差。但"静默返回默认值"若设计不当会掩盖数据错误（NaN/Inf 传播）。

## 决策

采用哨兵契约 L1-L5：

- 不可转换值返回类型零值哨兵，**不抛异常**
- 未知类型 Convert 失败：`double → NaN`，其余类型必须 `throw`
- 显式守卫 `NaN/Inf/null/default!`，`WrapError` 不兜底（只转换已抛出的异常为 Excel 错误值）
- `catch when` 排除不可恢复异常（OOM / StackOverflow / AccessViolation，集中式 `ExceptionFilters.IsCatchable`）

## 原因

1. 热路径避免 try-catch 开销（costsuite 性能回归实证）
2. NaN 作为"无效"标记可参与 Excel 原生计算语义（#NUM! 规则）
3. 哨兵可被 CrossVal 与 Python 参考实现精确比对（闭环验证）

## 约束

- `grep -rn "catch\s*{" src/ --include="*.cs"` 必须返回空（pre-commit 检查 1）
- 含除法的 Core 文件必须有 NaN/Inf 守卫（pre-commit 检查 5）
- 不可转换类型必须走 InputNormalizer（哨兵规则单点实现）

## 影响

- 正面：数值函数行为确定、可交叉验证、性能无异常开销
- 代价：调用方需理解哨兵语义（user-manual「错误处理」章节有完整清单）
- 同步位置：AGENTS.md 红线 2/5、skills/excel-dna-project.md、docs/user-manual/user-manual.md

## 演进

- **2026-07**: 初始确认（v1.0.0）
