# ExcelFormulaLabs 卓越路线图（2026-09-23）

> 基线：v2.3.1。对照姊妹项目 EngSmartSuite（Python）的工程实践差距分析产出。
> 原则：每条行动必须有可执行验证方式；门禁类变更必须正向全绿 + 负向注入自测。
> 状态标记：`[ ]` 未开始 / `[~]` 进行中 / `[x]` 已完成（验证通过）。

---

## 一、实测基线（2026-09-23，本机 CI 同口径）

| 指标 | 初始实测 | 当前实测（2026-09-23 Phase 4 后） | 目标 |
| :--- | :--- | :--- | :--- |
| 行覆盖率 Foundation（net8.0） | 84.25%（门禁 75） | **96.02%（门禁 92）** | ≥88% 达成 |
| 行覆盖率 Analytics（net8.0） | 89.05%（门禁 50） | **90.2%（门禁 86）** | ≥90% 达成 |
| 行覆盖率 DataToolkit（net8.0） | 89.22%（门禁 42） | **90.18%（门禁 86）** | ≥90% 达成 |
| 真 C# 交叉对照 UDF 数 | 124/240（51.7%） | **216/240（90.0%）** | ≥80% 达成 |
| 手册检查总数 | 432（manual 235 / cross 197） | **520（manual 235 / cross 285），0 FAIL / 0 SKIP** | — |
| 测试弱断言审计 | 无 | 零断言/恒真 FAIL + 存在性断言预算 **0** | 预算归零 达成 |
| 依赖锁定 | 无 lock 文件 | 8 工程 packages.lock.json + CI locked mode | 达成 |
| NuGet 漏洞审计 | 仅 dependabot 告警 | CI `dependency-audit` job（JSON 解析 + 失败门禁） | 达成 |
| 性能基准 | 不进 CI | 周更 workflow + 构件（阈值告警留后续） | 回归阈值告警（部分） |
| 发版 | 手工 bump + CHANGELOG | release-please 自动化（首次 Release PR 待远端观察） | 达成 |
| 文档站 | 无 | mkdocs-material + Pages + 英文 API 摘要页 | 达成 |
| 用户上手 | 手工加载 .xll | 安装脚本 + 示例工作簿 + SHA256 | 达成 |
| 静态分析 | 无 | `AnalysisMode=Recommended` + 0 警告门禁（32 处修复） | 达成 |

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
- [x] 验证（Phase 1.4 时点）：498 项检查 0 FAIL / 0 SKIP；真 C# 对照 **194/240（80.8%）**
      （后续 Phase 4.2/4.5 提升至 520→523 项 / 216/240，见 4.2/4.5）

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

> 注（review-2026-09-24 R1-13）：原此处残留一份 Phase 2 未勾选的重复清单
>（与上方 `[x]` 版本并存，易被误读为未完成）——已删除，Phase 2 状态以本节 `[x]` 为准。

---

## Phase 3 — 工程质量与用户价值（2026-09-23 实施）

### 3.1 公共 ROADMAP.md `[x]`

- [x] 根 `ROADMAP.md`：项目定位、决策门（新 UDF/依赖/性能/行为变更/文档）、里程碑、
      good first issue 候选
- [x] AGENTS/project-structure 双树登记 + README 文档索引
- [x] 验证：verify-docs 26 PASS

### 3.2 Excel COM E2E 定期化评估 `[x]`

- [x] 评估结论：**暂不纳入 CI**（[ADR-0010](../adr/0010-excel-com-e2e-scheduling.md)）——
      托管 runner 无 Office；self-hosted 成本/并发独占/供应链风险；发版检查单手工 E2E 已覆盖
- [x] 复评触发条件写入 ADR 演进节；ROADMAP 中期里程碑标注结论

### 3.3 静态分析补盲 `[x]`

- [x] `src/Directory.Build.props` 启用 `<AnalysisMode>Recommended</AnalysisMode>`，配合
      `TreatWarningsAsErrors=true` 新违规直接阻断构建（net48 TFM 不产生该分析器集诊断）
- [x] 修复 32 处命中（8 条规则）：CA1304/CA1305 文化显式化、CA1309/CA1310 序数/显式比较、
      CA1859 具体返回类型、CA1861 静态复用常量数组、CA1864 去重查找、CA1869 缓存
      `JsonSerializerOptions`
- [x] 不可跨 TFM 的 7 条规则（CA2249/CA1512/CA1845/CA1846/CA1847/CA1850/CA1837，建议 API
      仅 .NET Core+）与 CA1707（UDF 命名契约）在 `.editorconfig` 显式抑制并注明理由
- [x] 验证：负向注入 `Convert.ToDouble("1.5")` → `error CA1305` 构建失败；双 TFM 构建 0 警告

### 3.4 弱断言预算归零 `[x]`

- [x] 替换 3 个存在性断言测试（`Uuid_not_null`/`RndA_not_null`/`RndN_not_null` →
      格式+长度强断言）
- [x] `check-test-quality.ps1` 默认 `-MaxWarn 0`；自测 7/7 通过
- [x] AGENTS 命令表同步（预算 3 → 0）

### 3.5 覆盖率爬坡至 90+ `[x]`

- [x] 新增 61 个测试（Foundation 43 / Analytics 14 / DataToolkit 4），针对真实未覆盖分支：
      RegexBudget 作用域/耗尽、SafeKey 深度与高秩数组、字典类型键与比较模式、Regex 缓存驱逐、
      输入规范化退化路径、QuickSort/argsort 大数组、MapOver 逐格异常隔离与类型转换、
      DecompCache/Identity/Diagonal 守卫、PHYCHEM 下标溢出、DictToReport 数组解包、
      DOE 项数/单元数守卫与三阶交互、字符串 2D 空白归一化等
- [x] 实测（CI 口径 net8.0）：Foundation 84.26% → **96.02%**、Analytics 89.06% → **90.2%**、
      DataToolkit 89.45% → **90.18%**
- [x] 门禁阈值 80/85/85 → **92/86/86**（≥4 点余量），ci.yml / coverage.ps1 / AGENTS /
      project-structure 同步
- [x] spec `[Fact]` 计数 2,805 → 2,866（Phase 3.5 时点；当前 2,871，见 specification.md）

### 3.6 英文 API 摘要页 `[x]`

- [x] `docs/api-summary.en.md`：16 个模块全部函数名索引 + 用法模式/错误语义/安全/验证摘要
- [x] mkdocs nav 增条目；README.en 文档索引登记；project-structure 树登记
- [x] 验证：`mkdocs build --strict` 全绿；verify-docs 26 PASS

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

## Phase 4 — 用户价值与验证深化（2026-09-23 实施）

### 4.1 基准回归阈值告警 `[x]`

- [x] benchmarks.yml 接入 `benchmark-action/github-action-benchmark@v1`：对比上一轮结果，
      劣化 >150% 触发 commit comment 告警（`fail-on-alert=false`，共享 runner 抖动不阻断）
- [x] 历史数据经 actions/cache 持久化（key 含 run_id + restore-keys 前缀），不推 gh-pages
- [x] `--exporters json`；三类报告分步上报（action 仅接受单文件，已本地 dry 验证文件名）
- [x] 验证：YAML 结构校验 + dry 运行产物文件名与配置逐一比对

### 4.2 交叉对照 65.4% → 80.8% `[x]`

- [x] 新增 37 个函数的真 C# 对照（UDF 级覆盖 157 → 194）：DT 9 / REGEX 4 / DICT 8 /
      RANGE 7 / JSON+XML 5 / PIVOT 2 / STR.FORMAT / LINALG.LU_P
- [x] Dispatcher 新增 27 个注册 + ToDayOfWeek/NullableString 助手；manifest 190 → 227 条
- [x] **交叉对照发现并修复真实缺陷**：`DT.WOM` 旧实现返回 0/2，与文档示例（6/15→3）
      矛盾 → 修复为日历周序（独立 commit a295569），新增回归守卫。
      注（review-2026-09-24 R1-05）：上限契约修正为 1–6——含 1 号的那一周为第 1 周时，
      1 号为周日且月长 31 天（如 2026-03）最多跨 6 个日历周，"1–5" 数学上不可达
- [x] verify-docs 检查 16 修正：manual/cross 为检查项数，允许 > 240（实测 cross=263 触发旧断言误报）
- [x] 实测（4.2 + 4.5 后）：520 项检查 0 FAIL / 0 SKIP；真 C# 对照 **216/240（90.0%）**；
      剩余 24 缺口为 `*_ASYNC`（12，Excel 异步）/ 随机类（5：ARR.SHUFFLE、STR.RND*/UUID）/
      复杂表输出（7：JSON/XML/SQL 表与 ARR.TOSET）
      （review-2026-09-24 后：523 项检查 0 FAIL / 0 SKIP，真 C# 对照 216/240 不变）

### 4.3 示例工作簿扩展 `[x]`

- [x] SOLVE sheet：新增 `SOLVE.QUALITY`（交叉验证 R²/MAE）与 `SOLVE.EQUATION`（方程+闭式反解）
- [x] DOE sheet：新增 coded 设计 + 响应数据块与 `DOE.ANALYZE` / `DOE.ANOVA` / `DOE.PARETO` 示例
- [x] 示例分块放置（避开动态数组溢出区，防 #SPILL 冲突）；samples/README + docs/samples 同步
- [x] 验证：openpyxl 重新生成 18 sheet 工作簿（19,337 bytes），结构与公式逐项抽查

### 4.4 手册按模块拆页 `[x]`

- [x] 3,983 行手册拆为：总览页（版本头/通用约定/错误参考/附录，114 行）+
      16 个模块分页（`docs/user-manual/modules/01-stats.md` … `16-range.md`）
- [x] TOC 链接重写为模块页链接；模块内外链相对路径自动加一层（`../specification` → `../../specification`）
- [x] mkdocs nav 嵌套 16 模块；project-structure 树同步
- [x] 验证：文件链接检查 0 断链；`mkdocs build --strict` 全绿；verify-docs 26 PASS

### 4.5 FS 批次交叉对照（22/22） `[x]`

- [x] 探针目录方案：Dispatcher 内 `FsProbe` 运行时唯一根（`%TEMP%/efl-crossval-<guid>`）+
      相对路径包装，manifest 参数保持静态；Program 测试后清理（尽力而为）
- [x] 22 个 FS UDF：纯路径 9（NORM/COMBINE/FNAME/BNAME/EXT/FOLDER/PWD/TEMP/DRIVES）+
      I/O 13（MKDIR/WRITE/APPEND/READ/FEXISTS/FSIZE/FDEXISTS/COPY/MOVE/LS/LSDIR/DELETE/DELDIR）
      按 manifest 顺序执行；LS/LSDIR 返回**相对文件名**并排序（目录枚举顺序无契约，
      两侧比较集合而非绝对路径）
- [x] **交叉对照发现 BOM 语义**：`FS.WRITE` 经 .NET `Encoding.UTF8` 写入 UTF-8 BOM（3 字节），
      Append 不重复写；契约此前未声明 → 手册 FS.READ/WRITE/APPEND 明确 BOM 语义；
      Python 侧以 `utf-8-sig` 镜像验证（14 字节与读回内容逐项一致）
- [x] 实测：520 项检查 0 FAIL / 0 SKIP；真 C# 对照 **216/240（90.0%）**；
      剩余 24 缺口为 `*_ASYNC`（12）/ 随机类（5）/ 复杂表输出（7）
      （review-2026-09-24 后：523 项检查，见 4.2）

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
| verify-manual | 498 项 0 FAIL / 0 SKIP；真 C# 对照 194/240 |
| mkdocs build --strict | 全绿 |
| Release 构建（-m:1） | 8 个 XLL；FileVersion/ProductVersion = 2.3.1 |
| 待合并后确认 | release-please 首次 Release PR；docs.yml 首次 Pages 部署；benchmarks 首次 dispatch |
