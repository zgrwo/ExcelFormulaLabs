# ExcelFormulaLabs 卓越路线图（2026-09-23）

> 基线：v2.3.1。对照姊妹项目 EngSmartSuite（Python）的工程实践差距分析产出。
> 原则：每条行动必须有可执行验证方式；门禁类变更必须正向全绿 + 负向注入自测。
> 状态标记：`[ ]` 未开始 / `[~]` 进行中 / `[x]` 已完成（验证通过）。

---

## 一、实测基线（2026-09-23，本机 CI 同口径）

| 指标 | 初始实测 | 当前实测（2026-09-23） | 目标（Phase 2+） |
| :--- | :--- | :--- | :--- |
| 行覆盖率 Foundation（net8.0） | 84.25%（门禁 75） | 门禁已抬至 **80** | ≥88% |
| 行覆盖率 Analytics（net8.0） | 89.05%（门禁 50） | 门禁已抬至 **85** | ≥90% |
| 行覆盖率 DataToolkit（net8.0） | 89.22%（门禁 42） | 门禁已抬至 **85** | ≥90% |
| 真 C# 交叉对照 UDF 数 | 124/240（51.7%） | **157/240（65.4%）** | ≥80% |
| 手册检查总数 | 432（manual 235 / cross 197） | **461（manual 235 / cross 226），0 FAIL / 0 SKIP** | — |
| 测试弱断言审计 | 无 | — | 零断言 FAIL + null-only WARN 预算 | 预算归零 |
| 依赖锁定 | 无 lock 文件 | — | packages.lock.json + CI locked mode | — |
| NuGet 漏洞审计 | 仅 dependabot 告警 | — | CI `dotnet list package --vulnerable` | — |
| 性能基准 | BenchmarkDotNet 存在，不进 CI | — | 周更 workflow + 构件 | 回归阈值告警 |
| 发版 | 手工 bump + CHANGELOG | verify-docs 三向断言 | release-please 自动化 | — |
| 文档站 | 无（单文件 3,982 行手册） | — | — | mkdocs-material Pages |
| 用户上手 | 手工加载 .xll | — | — | 安装脚本 + 示例工作簿 + SHA256 |

> 覆盖率口径：`dotnet test` + coverlet `Include="[模块]*"`（与 ci.yml coverage job 一致）。
> 本地旧 cobertura（`coverage-local/`）未加 Include 过滤，数字不可作为门禁依据。

---

## Phase 1 — 质量底线（本轮实施）

### 1.1 覆盖率门禁爬坡 + 构件上传 `[x]`

- [x] `ci.yml` coverage job：阈值 75/50/42 → **80/85/85**
- [x] 上传 `tests/*/coverage/*.cobertura.xml` 构件（`if: always()`，保留 14 天）
- [x] 新增 `scripts/coverage.ps1`：本地一键跑 CI 同口径三模块覆盖并汇总
- [x] 验证：`powershell -File scripts/coverage.ps1` 三模块全绿（84.25 / 89.05 / 89.22 ≥ 80/85/85）

### 1.2 测试质量守卫（新门禁） `[x]`

- [x] 新增 `scripts/check-test-quality.ps1`：
  - FAIL：`[Fact]`/`[Theory]` 方法体零断言
  - FAIL：恒真断言（`Assert.True(true)` / `Assert.False(false)`）
  - WARN：仅存在性断言（`.Should().NotBeNull()` / `Assert.NotNull`）——预算制 `-MaxWarn`（基线 3）
- [x] 新增 `tests/scripts/test_check_test_quality.ps1`：7 场景（正向全绿 + 负向注入 + 注释规避）
- [x] 接入 `ci.yml` redline-check job 与 `.pre-commit-config.yaml`
- [x] `AGENTS.md` 提交前必检与命令表登记新门禁
- [x] 验证：自测 7/7 通过（PS 5.1）；当前仓库扫描 2813 个测试方法，0 零断言 / 0 恒真 / 3 存在性

### 1.3 NuGet 锁文件 + 漏洞审计 `[x]`

- [x] `src/Directory.Build.props`、新增 `tests/Directory.Build.props`、`benchmarks/Directory.Build.props`：
      `RestorePackagesWithLockFile=true` + `RestoreLockedMode`（`CI=true` 时）
- [x] 生成并提交全部 8 个工程的 `packages.lock.json`
- [x] `ci.yml`：setup-dotnet 缓存键补 `**/packages.lock.json`；新增 `dependency-audit` job
      （restore locked + `dotnet list package --vulnerable --include-transitive`，JSON 输出解析，有漏洞即 FAIL）
- [x] 验证：负向注入（删 lock 中 Microsoft.CSharp）→ NU1004 FAIL；还原后正向通过；
      审计命令输出 8 工程无漏洞

### 1.4 交叉验证覆盖率提升 `[x]`

- [x] `verify-manual.py` 增加缺口清单输出（`VERIFY_MANUAL_SHOW_GAPS=1` 时打印未覆盖 UDF）
- [x] `cross_vs_csharp` 的 UDF 名计入 CROSS_REFERENCED（修复 QR_R/LU_U/SVD_S/REGRESS.COEF 漏计）
- [x] PHYCHEM 单位换算 8 项：复用既有 manifest 条目补 `cross_vs_csharp`，
      缺条目（PSI_TO_ATM / L_TO_GAL / LB_TO_KG）补 manifest + 映射
- [x] STR 12 项（NORMWS/TITLE/REMOVE/KEEP/TRUNCATE/STARTSWITH/ENDSWITH/LEFTOF/RIGHTOF/EXTRACT/NTHWORD/STRIPHTML）
      + ARR 9 项（SORTASC/SORTDESC/SORTTEXT/SLICE/FILTER×5）：Dispatcher 注册 + manifest + Python 独立实现
- [x] 验证：461 项检查 0 FAIL / 0 SKIP；真 C# 对照 **157/240（65.4%）** ≥ 150

### 1.5 修复复现测试审计（P0-5） `[x]`

- [x] 扫描最近 200 提交中 75 个 `fix(...)` commit：42 带测试改动 / 33 未带
- [x] 33 个未带的逐条分类（CI/脚本/依赖类 20 个无缺口；代码类 13 个中 4 项需补）
- [x] 报告归档 `logs/reports/repro-test-audit-2026-09-23.md`（不入库）
- [x] 收尾复核（2026-09-23）：4 项中 3 项经 `git log -S` 交叉核对为相邻 commit 已覆盖
      （`c3e458f`→454760f 预算测试、`b61f468`→c45e5bb 24 路并发测试、`0d93265`→test_governance_tools [4]）；
      1 项真缺口（`93e1d20` 空哨兵透传）已补 2 测试（真实 ExcelDna.Integration.ExcelEmpty + BeSameAs），双 TFM 全绿
- [x] 审计报告结论已更新：`logs/reports/repro-test-audit-2026-09-23.md`（含"单 commit 视角误报 3/4"的方法论教训）

---

## Phase 2 — 工程效率与用户价值（下一轮）

### 2.2 release-please 自动化发版 `[x]`

- [x] `release-please-config.json`（simple 策略，version.txt 版本文件）+ `.release-please-manifest.json`
- [x] extra-files XML xpath 同步 `src/Directory.Build.props` `<Version>`；AV/FV 改为 `$(Version).0` 派生
- [x] `.github/workflows/release-please.yml`：Release PR → tag → `gh workflow run release.yml --ref <tag>`
      （GITHUB_TOKEN 建 tag 不触发 push 事件的反递归规避，含 5 次重试）
- [x] `release.yml` 增加 `workflow_dispatch` 触发器；仓库设置 "Allow GitHub Actions to create and approve pull requests" 已启用
- [x] `verify-docs` 检查 10b（version.txt == Version）+ 负向自测场景 M
- [ ] 验证：首次 Release PR 全流程（需合并本变更后观察一次真实发版）

### 2.3 安全回归测试显式化 `[x]`

- [x] `tests/DataToolkit.Tests/SecurityTests.cs`：XML XXE/DTD/深度炸弹 + SQL 注入（恶意列名/参数化文本/多语句）
- [x] 既有安全测试打 `[Trait("Category","Security")]`：沙箱越界/ReDoS/公式注入/SQL 校验/原生 DLL 完整性
- [x] CI test job 增加 `dotnet test --filter "Category=Security"` 显式门禁
- [x] 验证：双 TFM 76 用例全绿

### 2.4 用户快速上手包 `[x]`

- [x] `samples/ExcelFormulaLabs-Samples.xlsx`（18 sheet：使用说明 + 16 模块 + JSON/XML 拆分）+ `samples/README.md`
- [x] `scripts/generate-samples.py`（openpyxl 生成，可重复运行）
- [x] `scripts/install.ps1`：SHA-256 校验 + Unblock + 复制到 `%LOCALAPPDATA%` + HKCU 注册（含 `-Uninstall`）
- [x] Release 附 `SHA256SUMS.txt`（8 个 XLL 哈希）
- [x] 验证：安装/卸载本地实测通过；工作簿 19KB 入库

### 2.5 文档站 `[x]`

- [x] `mkdocs.yml`（material 主题 + CJK 锚点对齐 GitHub + 验证分级）+ `docs/index.md` + `docs/samples.md`
- [x] `.github/workflows/docs.yml`：PR 只构建、main 部署 Pages
- [x] 修复手册两处真实断链（`#16-错误参考` → `#17-错误参考`；`arr-ne/gt/lt` 锚点）
- [x] GitHub Pages 已启用（build_type=workflow）：https://zgrwo.github.io/ExcelFormulaLabs/
- [x] 验证：`mkdocs build --strict` 全绿；首次部署待合并后确认

### 2.6 CI 路径过滤 + affected 测试接入 `[x]`

- [x] `classify` job（dorny/paths-filter）：docs-only 变更跳过构建/测试/覆盖率重 job；workflow_dispatch 视为全量
- [x] `verify-docs` 改为始终运行（文档变更恰是它最需要的场景）
- [x] redline-check 增加受影响测试映射步骤（PR 信息性输出，不替代全量测试）
- [x] 验证：YAML 校验 + 本地 DryRun 路由实测通过（分支无保护规则，skipped job 不阻塞合并）

### 2.1 性能基准进 CI `[x]`

- [x] 新增 `.github/workflows/benchmarks.yml`：weekly cron + dispatch + main 路径触发，
      `dotnet run -c Release --project benchmarks/ExcelFormulaLabs.Benchmarks -- --filter "*"`
- [x] 上传 BenchmarkDotNet.Artifacts 构件（保留 90 天）
- [x] 本地验证：基准工程 Release 构建通过；YAML 结构校验通过（首次手动 dispatch 待合并后确认）

### 2.2 release-please 自动化发版 `[ ]`

- [ ] `.github/release-please/config.json` + manifest，`release-type: simple`
- [ ] extra-files 同步 `src/Directory.Build.props` 的 Version/AssemblyVersion/FileVersion
- [ ] release.yml 接入 release-please job（tag 与 GitHub Release 自动创建）
- [ ] 验证：一次完整 release PR 演练（版本三向一致由 verify-docs 检查 10 兜底）

### 2.3 安全回归测试显式化 `[ ]`

- [ ] 将 FS 沙箱越界 / NTFS junction、SQL 注入与列名消毒、Regex 超时收敛为
      `tests/Security.Tests`（或标签过滤 `--filter Security`）
- [ ] CI 增加 `dotnet test --filter Security`
- [ ] 验证：三类攻击面用例可独立运行且全绿

### 2.4 用户快速上手包 `[ ]`

- [ ] `samples/ExcelFormulaLabs-Samples.xlsx`：16 模块各一 sheet，公式已填
- [ ] `scripts/install.ps1`：`Unblock-File` + 复制/引导加载 + 校验 SHA256
- [ ] Release 附 `SHA256SUMS` 清单
- [ ] 验证：干净机器按 README 步骤 60 秒出结果

### 2.5 文档站 `[ ]`

- [ ] mkdocs-material（或 DocFX）站点：手册按 16 模块拆页 + 截图
- [ ] GitHub Pages 部署 workflow
- [ ] 验证：`mkdocs build --strict` + Pages 可访问

### 2.6 CI 路径过滤 + affected 测试接入 `[ ]`

- [ ] docs-only PR 跳过重 job（paths 过滤）
- [ ] `run-affected-tests.ps1` 接入 PR quick job（当前脚本存在但未接入 CI）
- [ ] 验证：docs-only PR 全绿且耗时显著下降；src 改动仍触发全量

---

## Phase 3 — 可选优化

- [ ] 公共 `ROADMAP.md`（决策门 + good first issue）
- [ ] Excel COM E2E（`test-load-unload.py`）定期化评估（self-hosted runner）
- [ ] 死代码/静态分析补盲（Roslyn IDE0051/CA1812 或 `dotnet format analyzers`）
- [ ] 覆盖率继续爬坡至 90+，弱断言预算归零
- [ ] 英文 API 摘要页（README.en 已存在）

---

## 验证总入口

```bash
bash scripts/verify-docs.sh                      # ① 文档一致性
dotnet test                                      # ② 全量测试（双 TFM）
dotnet test --filter "CrossVal"                  # ③ 交叉验证测试
python scripts/verify-manual.py                  # ④ 手册 + C# 对照
powershell -File scripts/pre-commit-check.ps1    # ⑤ 红线 6 项
powershell -File scripts/check-test-quality.ps1  # ⑤+ 测试质量守卫（新增）
powershell -File scripts/coverage.ps1            # ⑥ CI 同口径覆盖率
dotnet build -c Release                          # ⑦ 分发构建
```

> 全量 6 步入口 `scripts/verify-all.ps1` 不变；新增门禁以独立脚本接入 CI 与 pre-commit，
> 避免改写既有"6 步 / 6 项"文档口径。

---

## 执行中发现并处置（2026-09-23）

### Phase 1

- **Release 打包并行竞态**：默认 `-m` 的 Release 构建连续 3 次在 ExcelDnaPack 资源更新报
  `Win32Exception 5 拒绝访问`（独占打开测试确认无外部持锁、文件非只读），`-m:1` 一次通过。
  处置：`ci.yml` / `release.yml` / `verify-all.ps1` 的 Release 构建统一 `-m:1`；
  经验写入 `skills/project-experience.md` E6 补充。
- **门禁测试自身腐化**：`test_verify_docs.ps1` 场景 G7 硬编码 `合计 432`，README 计数更新后
  注入静默失效（负向场景假通过）→ 改为通配 `合计\s*\d+`，测试不再随计数漂移腐化。
- **本地旧覆盖率报告误导**：`tests/*/coverage-local/` 未加 Include 过滤（Analytics 显示 53.5%，
  真实 89.05%）→ `scripts/coverage.ps1` 头注说明，统一以 CI 同口径为准。

### Phase 2

- **文档站发现两处真实断链**：user-manual `#16-错误参考` → `#17-错误参考`；
  `arr-ne/gt/lt` 显式锚点与索引 `#arr-filter-ne/gt/lt` 不一致 → 统一为 `arr-filter-*`。
  `mkdocs build --strict` 现全绿。
- **CJK 锚点算法差异**：Python-Markdown 默认给中文标题生成 `_1/_2` 锚点 → 配置
  `pymdownx.slugs.slugify(case=lower)` 对齐 GitHub，手册内部跳转在站点可用。
- **版本三值同步消除**：AV/FV 改 `$(Version).0` 派生（release-please 只需 bump `<Version>`），
  verify-docs G1 适配派生形式；新增检查 10b 强制 `version.txt == <Version>`。
- **release-please 反递归**：GITHUB_TOKEN 建 tag 不触发 push 事件 → release-please.yml
  显式 `gh workflow run release.yml --ref <tag>`（dispatch 为豁免事件）。
- **Pages 仓库设置**：已通过 API 启用 "Allow GitHub Actions to create and approve pull requests"
  与 Pages（build_type=workflow）。

## Phase 2 收尾验证（2026-09-23）

| 验证项 | 结果 |
| :--- | :--- |
| verify-docs | 26 PASS / 0 FAIL / 1 SKIP（含新检查 10b） |
| pre-commit / test-quality | PASS |
| 治理脚本自测 | 4 脚本 7/14/16/23 场景全绿 |
| dotnet build + test（双 TFM） | 0 警告 0 错误；2,873 用例 ×2 全绿 |
| verify-manual | 461 项 0 FAIL / 0 SKIP；真 C# 对照 157/240 |
| mkdocs build --strict | 全绿 |
| Release 构建（-m:1） | 8 个 XLL；FileVersion/ProductVersion = 2.3.1 |
| 待合并后确认 | release-please 首次 Release PR；docs.yml 首次 Pages 部署；benchmarks 首次 dispatch |
