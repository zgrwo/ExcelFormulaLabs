# ADR-0010: Excel COM E2E 测试定期化评估 — 暂不纳入 CI

**日期**: 2026-09-23
**状态**: 已确认
**关系**: 路线图 Phase 3「Excel COM E2E（`test-load-unload.py`）定期化评估（self-hosted runner）」结论

## 上下文

`scripts/test-load-unload.py` 是现有的端到端测试：在真实 Excel 中加载/卸载 8 种 XLL 组合
（net48/net8.0 × 单/双加载，含混合 TFM），校验公式求值、公式单元格交互、JIT 对话框与
IntelliSense 回调错误，每场景 4 轮。它目前只能手工运行，依赖：

- 本机安装的 Excel（COM 自动化）；
- Python `pywin32` + `psutil`；
- 已打包的 XLL 目录（`--built` 指向含 `net48/` 与 `net8.0-windows/` 子目录的产物根）。

路线图要求评估：能否把该 E2E 定期化（例如 self-hosted runner 周更）？

## 决策

**暂不把 Excel COM E2E 纳入 CI（含 self-hosted runner），保持「发版前手工执行」的显式检查项。**

## 原因

1. **托管 runner 无 Office**：GitHub-hosted Windows runner 不包含 Excel；COM 自动化必须安装
   完整 Office，且依赖交互式桌面会话（`Excel.Application` 在无桌面的服务会话中不稳定，
   弹窗/激活失败会直接挂起 job）。
2. **self-hosted 成本与风险**：需常驻 Windows + Office 授权机器，承担补丁/升级与
   并发独占（Excel 单实例 UI）维护；仓库当前无此类基础设施。
3. **现有替代覆盖充分**：打包正确性由 Release 构建 + `verify-pack.ps1` + `test-xll.ps1`
   覆盖；加载/卸载 E2E 已纳入发版检查单，v2.3.x 发版连续手工验证通过。
4. **供应链安全**：self-hosted runner 上执行 PR 触发代码存在风险；若未来引入，必须
   限定 `workflow_dispatch` / tag 触发，不响应 fork PR。

## 约束

- `scripts/test-load-unload.py` 保持可手工运行；`--built` 参数契约（含 `net48/` 与
  `net8.0-windows/` 子目录）不得破坏。
- 发版检查单保留「手工 E2E」步骤；不得因本 ADR 移除该步骤。
- 若未来引入 self-hosted runner：仅允许 `workflow_dispatch` 或 tag 触发，job 标注
  `[self-hosted, windows, excel]`，并先更新本 ADR（状态改为「已废弃」并引用新决策）。

## 影响

- 正面：无新增基础设施与供应链风险；E2E 结果由发版人显式确认（有签名式检查单）。
- 负面：日常 PR 不拦截「真实 Excel 加载」回归；该类缺陷依赖发版前检查发现。
- 同步位置：`ROADMAP.md`（中期里程碑）、`docs/plans/2026-09-23-excellence-roadmap.md`、
  `docs/governance/project-structure.md` 目录树。

## 演进

- **2026-09-23**: 初版（评估结论：暂不纳入；复评触发条件：仓库获得可专用 Windows+Office
  机器，或 GitHub 提供含 Office 的托管 runner）。
