---
name: project-experience
description: 项目经验库 — 从 v2.0.0 至今全部 commit/审查/CI 事故提炼的高频陷阱与铁律。修改代码、写测试、改门禁、发版前先查本表（版本臆测、pwsh7 差异、8.3 短路径、原生 DLL 提取、数值溢出等）。Use when fixing bugs, adding gates, bumping dependencies, or preparing releases.
---

# SKILL.md — 项目经验库

> 从 v2.0.0 至今（66+ commits、4 轮 max level 深度审查、多起 CI 事故）凝练。
> **单一信源**：AGENTS.md 的「历史经验」表只留摘要，详细根因与证据链在本文件。
> 每条格式：**现象 → 根因 → 正确做法 → 证据**。

---

## 一、版本与依赖（最高频踩坑域）

### D1 版本下限臆测（CI 事故 #1，2026-08-29）
- **现象**：`requirements.txt` 写 `pyDOE2>=1.9`，CrossVal CI 依赖解析失败。
- **根因**：凭记忆写版本；PyPI 最新仅 **1.3.0**（可用 1.0.2→1.3.0）。
- **铁律**：任何依赖版本下限必须 `pip index versions <pkg>` 实测；新增/修改依赖后必须 `pip install --dry-run -r requirements.txt`。
- **证据**：`fix(ci): pyDOE2 版本下限修正`。

### D2 CI Python 版本兼容边界
- **现象**：dependabot 自动开 numpy 2.5.2 / scipy 1.18.1 PR，CI 失败。
- **根因**：numpy≥2.5 / scipy≥1.18 要求 Python≥3.12，CI 用 3.11。
- **铁律**：CI Python 版本是硬约束，必须在 requirements.txt 写**上限**（`numpy>=1.26,<2.5` / `scipy>=1.11,<1.18`）；dependabot 识别上限后不再开超限 PR（PR 标题自动变为 `>=1.9.0,<2` 形式）。
- **证据**：`fix(deps): Python 依赖上限约束`；PR #20-22。

### D3 ExcelDna 升级铁律
- **1.8→1.9**：含 CVE-2024-43485 修复，升级必须做 net48 兼容回归。
- **IntelliSense**：net8.0 禁止安装（Excel-DNA Issue #343，8 次反复尝试教训）；仅 net48 `#if NET48` 启用。
- **证据**：`chore(deps): bump ExcelDna packages from 1.8.0 to 1.9.0`。

### D4 NuGet 版本耦合
- **Microsoft.Data.Sqlite ↔ SQLitePCLRaw**：e_sqlite3.dll 嵌入路径与 bundle 版本耦合，大版本升级需人工适配 → dependabot ignore semver-major/minor。
- **System.Text.Json**：10.x 不支持 netstandard2.0（net48 不可用）→ 停在 8.0.5（CVE-2024-43485 修复版）。
- **证据**：`fix(security): System.Text.Json 8.0.4→8.0.5`；dependabot.yml ignore 规则。

### D5 依赖升级破坏 API
- **FluentAssertions 8.10**：移除 `MatchRegex`/`OrEqualTo` 旧 API，升级后测试编译失败 → 升级依赖必须全量 `dotnet test`。
- **证据**：`chore(deps): FluentAssertions 升级 8.10.0`。

---

## 二、CI 与门禁（"本地通过 ≠ CI 通过"）

### C1 8.3 短路径破坏字符串前缀（CI 事故 #4）
- **现象**：test_verify_docs 场景 A 在 CI 全 FAIL，undeclared 路径是 `ure/src/...`。
- **根因**：GitHub Actions `$env:TEMP = C:\Users\RUNNER~1\...`（8.3 短名），`Get-ChildItem` 返回长名 `runneradmin`——长度差 3，`Substring($RepoRoot.Length)` 错位；本地路径无短名所以从未暴露。
- **铁律**：任何 `Substring(路径前缀长度)` 切片前，必须 `[IO.Path]::GetFullPath($RepoRoot)` 规范化（GetFullPath 会展开 8.3 短名）。
- **证据**：`fix(ci): verify-docs 路径规范化（GetFullPath 展开 8.3 短名）`。

### C2 pwsh7 vs PS5.1 行为差异（CI 事故 #3）
- **现象**：H1 产物断言在 CI 报「缺少 8 个产物」，但文件明明存在。
- **根因**：`$_.Name -eq $_`（字符串 vs FileInfo 对象比较）——.NET Framework 的 `FileInfo.ToString()` 返回**文件名**（PS 5.1 碰巧成立），.NET Core 返回**全路径**（pwsh 7 恒 false）。
- **铁律**：集合成员判断用纯字符串比较（`$_ -notin @($xlls | ForEach-Object { $_.Name })`），**绝不依赖对象隐式字符串化**；本地验证必须考虑 pwsh 7 语义。
- **证据**：`fix(ci): H1 产物断言 pwsh7 兼容`。

### C3 跨平台路径分隔符（CI 事故 #2）
- **现象**：verify-docs 检查 16/18 在 Linux（pwsh）全失配。
- **根因**：`TrimStart('\')` 只去反斜杠；Linux 路径前导 `/` 未被处理，豁免比较 `"docs\cross-validation.md"` 与 `"docs/cross-validation.md"` 不相等。
- **铁律**：路径相对化先 `-replace '\\','/'` 再 `TrimStart('/')`；文档/豁免比较一律用正斜杠形式；排除正则用 `'/(\.git|bin|obj|logs)/'`（归一化后匹配）。
- **证据**：`fix(ci): verify-docs 跨平台路径归一化`。

### C4 门禁代码必须执行测试（含负向）
- **现象**：H1 资产断言、检查 17/18 首次在 CI 执行即暴露 bug——本地"看起来对"。
- **铁律**：新门禁/检查必须：① 正向全绿；② **负向注入漂移 → FAIL 指名**（如改 STATS.MEAN 参数名、新增 FakeProbe.cs、README 注入错误计数如 999）；③ 加入 `tests/scripts/test_verify_docs.ps1` 场景防回归。
- **证据**：release-audit-2026-08-29/30；场景 B-G。

### C5 既有门禁教训
- **NuGet push**：`artifacts/nupkg/*.nupkg` 通配符在 pwsh 下不展开 → 用 `Get-ChildItem` 管道；加 `--no-symbols`（snupkg 存在）。
- **覆盖率门禁**：coverlet 默认 minimum 阈值误判 → 显式 `ThresholdStat=total`。
- **版本一致性校验**：必须在 pack-release job（build-test 里 `GITHUB_OUTPUT` 为空）；tag==`<Version>`==AV/FV 三值一致（verify-docs G1 检查）。
- **release tag 触发**：GitHub Actions 过滤是 **glob 语义非正则**——`+` 不可作量词，用 `v[0-9]*.[0-9]*.[0-9]*`。
- **证据**：`ci: 修复 Release 流水线 NuGet push 通配符`；`fix(ci): 版本一致性校验移至 pack-release job`。

---

## 三、安全与架构红线

### S1 原生 DLL 提取（B1：v2.2.1 失效 → v2.2.2 修复）
- **现象**：v2.2.1 的 SHA-256 完整性加固是空转——`Sha256Equals` 把资源流读到末尾**不复位** → 写 0 字节；`File.Move` 无法覆写已存在文件 → 同尺寸篡改 DLL 仍被加载、升级换版本永不替换。
- **正确做法**（NativeDllStore）：内容寻址（目标路径由嵌入字节 SHA-256 派生）+ **每次调用重验**盘上哈希 + `File.Replace`/`File.Move` 原子替换（双 TFM）+ 失败 fail-safe 跳过加载（外层 catch-all + ExceptionFilters）。
- **残余**：校验与 LoadLibrary 间 TOCTOU 窗口（本地攻击者威胁模型内接受，已文档化）。
- **证据**：`fix(review): 2.2.2 发行前深度审查修复实施`。

### S2 防错三原则（红线，零容忍）
- 静默传播阻断：显式守卫 NaN/Inf/null/default!，WrapError 不兜底。
- 防御完整性：ValidatePath / Regex 超时 / SQL 参数化覆盖所有方法。
- 异常过滤器：`catch when` 排除 OOM/StackOverflow/AccessViolation（`grep -rn "catch\s*{" src/` 必须为空）。
- Core 层零 Excel 依赖（`grep -rn "ExcelDna.Integration" src/*/*Core.cs` 必须为空）。

### S3 CodeQL 抑制注释
- codeql-action v4 下抑制注释**不生效**（实测）→ quality 警报走 GitHub dismiss 流程，不写无效注释。
- **证据**：`revert: 移除无效的 CodeQL 抑制注释`。

---

## 四、数值正确性（高频修复模式）

| 模式 | 出现次数 | 根因与正确做法 |
|---|---|---|
| NaN/Inf 守卫缺失 | 10+ | 除法/累加/平方/序列未考虑退化输入；除法路径必须守卫 |
| 累加溢出泄漏 | 4 | Pivot/GroupBy 的 SUM/AVG 累加溢出 ±Inf 直接进输出单元格 → AggResult 对非有限值返回 NaN |
| 平方溢出绕过守卫 | 1 | AnovaOneWay `Abs(Inf)<1e-15` 恒 false → 补非有限平方和显式抛错 |
| int 回绕 | 3 | `qty1+qty2` 求和、水合物系数逐位累积 → 改 long / 显式溢出抛错 |
| 灾难性抵消 | 2 | TSS 两遍减法 → 单遍中心化形式 |
| OOM 守卫 | 3 | DOE 因子数（MaxFactors=1000）、LINALG.IDENTITY（2000）、cells 上限（MaxCells=1e6）——**在分配数组前**检查，除法形式防乘法溢出 |
| 序列退化 | 1 | ArrayCore.Sequence 补 ±Inf 守卫；`(int)d` 对 d≥2³¹ 回绕 → 显式检查 |
| **绝对阈值误判小量纲**（2026-08-31 深度审查 P1-5，6 处） | 6 | `va < 1e-15`/`sd < 1e-12`/`Abs(ssW) < 1e-15`/`1e-8` 绝对对称阈——ppm/ppb/nm 量纲数据方差天然 < 1e-15 → 常量分支/误报。**判据必须与数据同尺度**：常量判据用精确零（`va == 0`），对称判据用相对（`> 1e-8 * scale`） |
| **正规方程条件数平方**（2026-08-31 深度审查 P0-1） | 1 | `X'X.Solve`/`X'X.Inverse` 把 cond(X) 平方——cond>1e8 时 r²≈1 但系数全错（静默）。**回归求解必须 QR/SVD**，标准误由 R⁻¹ 求 |
| **NaN 比较恒 false 的复活路径**（2026-08-31 深度审查 P1-4） | 1 | `sd < 1e-15` 对 sd=Inf/NaN 恒 false → 假 1.0 对角线。修了一个分支（rows<2）不修 NaN/Inf 路径 = 同类缺陷复活。**守卫必须同时覆盖 NaN/Inf/溢出三路径** |
| **2⁶³ 边界守卫漏洞**（2026-08-31 深度审查 P2-8） | 1 | `rd > long.MaxValue` 比较时 long.MaxValue 转 double = 2⁶³，`2⁶³ > 2⁶³` 恒 false → 守卫绕过 → (long) 得 long.MinValue。**边界守卫用 2⁶³ 字面量严格比较** |
| **排序全等值 O(n²)**（2026-08-31 深度审查 P1-2） | 1 | Lomuto 分区全等值输入每次只推进 1 个元素（20 万 = 152 秒）。**3-way（Dutch flag）分区跳过等值段** |
| **顺序依赖溢出**（2026-08-31 深度审查 P2-11） | 1 | `Product(1e300,1e300,1e-300)` 朴素左折叠 → Inf→NaN（真值 1e300）。**按 |x| 升序相乘** |

- **哨兵契约（L1-L5）**：不可转换值返回类型零值哨兵不抛异常；double→NaN，其余未知类型 throw。
- **测试**：每个数值修复必须保留复现测试（回归守卫）。

---

## 五、文档 SSOT 与计数

- **数字唯一信源**：`rules/api-reference.md`（236 UDF），一切计数从此推导。
- **散文计数**：verify-docs 检查 16 全仓扫描 `*.md` 的 `N UDF`（豁免 cross-validation 模块级与 CHANGELOG 历史）——曾 232 陈旧漂移全绿通过。
- **CHANGELOG 声称必须反映现实**（防幻觉铁律）：3 次发现「声称未兑现」（.editorconfig 对齐、脚本参数化、门禁 17/18）——写完条目对照 diff 逐条核实。
- **版本头三处同步**：specification / user-manual / cross-validation（含日期行）。
- **参数一致性**：api-reference 参数列 ↔ `[ExcelArgument(Name=...)]` 自动比对（检查 17，剥离 `[可选]` 标记）。

---

## 六、发版流程检查清单（按序执行）

1. `src/Directory.Build.props`：`<Version>` + AV/FV **三值同步** bump（G1 强制）。
2. CHANGELOG：`## [X.Y.Z] - 日期`（无占位符）+ 版本链接行。
3. verify-docs 全绿**除**「Version == latest tag」（预期，tag 未建）。
4. 提交 → 打 tag → push main + tag（release.yml 的 tag==Version 校验拦截不一致）。
5. Release 工作流：Build/Test → Verify Docs → CrossVal → Pack & Release（8 资产 H1 断言、--no-symbols、fail_on_unmatched_files）。
6. 发版后确认：GitHub Release 资产 8 个、NuGet Foundation 包推送、CHANGELOG 与 tag 一致。
7. **CI 事故复盘**：若 release 失败，查工作流 job 日志定位（CI 是唯一真实环境）。

---

## 七、治理流程

- **max level 深度审查**：P0（发行阻塞）/P1（高危）/P2（应修复）/P3（门禁增强）分级；实证复现 + 负向测试 + 逐条声称对账；报告归档 `logs/review/release-audit-*.md`。
- **修复验证闭环**：复现测试 FAILS → 修复 → PASSES + 无回归 → **保留复现测试**。
- **跨会话交接**：`✅ 已完成 / 🔜 下一步 / ⚠️ 待决策 / 📄 关键上下文` 四段式。
- **提交规范**：Conventional Commits；`fix(review)` 是审查修复专用 scope。

## 八、审查逃逸模式（2026-08-31 深度审查复盘）

> 为什么 61 项问题（4 P0 / 20 P1 / 37 P2）能逃过此前 4 轮审查。每条都是"下一轮审查必须主动扫描"的模式。

### E1 门禁正则盲区（漏检 P0-4 / P1-14 / P1-15 / P1-18）
- **现象**：verify-docs 检查 16 只匹配 `N UDF`（英文），中文「N 个 UDF」漏网；检查 10 只查 CHANGELOG 章节头不查链接行；无文档版本头检查；pre-commit 只扫 `*Core.cs`。
- **铁律**：门禁正则必须覆盖中英双语 + 全部变体（`(\d+)\s*(?:个)?\s*UDF` + 分数形式）；每新增一个"声称"（版本头/链接行/覆盖数）就加一条检查；扫描范围用"排除 bin/obj 的全部 .cs"而非文件名通配。
- **证据**：P0-4 负向注入（中文 999 全绿通过，英文被抓）。

### E2 绝对阈值被"数学直觉"接受（漏检 P1-5 六处 / P1-6）
- **现象**：`va < 1e-15`、`sd < 1e-12`、`Abs(ssW) < 1e-15`、`1e-8` 对称阈——"1e-15 ≈ 0"在纯数学成立，ppm/ppb 量纲数据（Excel 常态）下是错误判据。
- **铁律**：审查时 `grep -rn "< 1e-\|< 1e\|1e-1[0-9]" src/` 逐条核对是否相对判据；任何"与数据量级无关的常数"都是红旗。
- **证据**：TTEST1({1e-9,2e-9,3e-9}) 返回 NaN，真值 p=0.478。

### E3 审查用例设计盲区（漏检 P0-1 / P1-4）
- **现象**：P0-1 的 Hilbert 8×8（n=p）被 df=0 守卫拦截，审查者没试 n>p 用例；P1-4 修了 rows<2 分支，NaN/Inf 溢出路径（sd=Inf）漏检。
- **铁律**：数值缺陷的复现用例必须覆盖"守卫不触发的相邻区间"（n=p 旁边试 n=p+2；NaN 旁边试 Inf；有限旁边试溢出）；临时审计测试本身必须进正式测试文件并反向验证（_AUDIT_Verify 的 A1/A6 曾有 bug）。
- **证据**：Hilbert 12×8 实测系数误差 10.86 而 r²=1.000000000000。

### E4 交叉验证假阳性（漏检 P0-3 三类）
- **现象**：NaN/Inf 序列化压成 null（C#=+Inf vs Python=NaN 判 PASS）；77% 的 check() 是纯 Python 自校验；manifest tolerance/summary 死数据。
- **铁律**：交叉验证的"验证通道"必须与"自校验通道"分离汇报；特殊值必须带标签（`{"__nan__":true}`）；manifest 元数据必须被消费（tolerance 读入比较、summary.error==0 断言）。
- **证据**：把 C# 侧 STATS.LN 改坏，一条都不会 FAIL。

### E5 审查测试自身质量（漏检 P1-7 / P2-19 / 报告 5 处错误）
- **现象**：审计测试 A6 漏了 ±1 编码负号（假通过）；A1 用例不可达；P2-19 断言 expected 由被测实现生成；报告自身 P1-2 计时低估 30 倍、P2-32 数字错。
- **铁律**：审计测试也是代码——负向测试必须显式标注"应失败"且修复后删除/转正；测试期望必须硬编码（禁 `Be(Soundex(x))` 自校验）；报告的计时/计数数字必须复测而不是引用。
- **证据**：Taguchi 别名真实存在（col2 == -A×B）但 A6 假通过（缺负号）。

### E6 临时件 vs 回归守卫（漏检 P1-2 / P1-9）
- **现象**：审计的计时基准（5.3s）未含 ComparisonUtils 分发开销（实测 152s，差 30 倍）；构建竞态"50% 失败率"实为 bin 残留 .dna 污染（BuildInParallel=false 早已修复）。
- **铁律**：性能/概率声称必须用**与生产代码相同路径**的复刻测量（含全部分发层）；"间歇性失败"先查环境残留（bin/obj 陈旧产物）再归因代码。
- **证据**：清理 bin 残留 .dna 后 10 次并行构建 0 失败。

---

## 经验索引（按触发场景速查）

| 场景 | 必读条目 |
|---|---|
| 加/改依赖版本 | D1, D2, D4 |
| 写/改门禁或 CI 脚本 | C1, C2, C3, C4 |
| 处理数值/聚合 | 四、数值正确性 |
| 处理文件/IO/原生资源 | S1, S2 |
| 改文档/计数 | 五、文档 SSOT |
| 发版 | 六、发版流程 |
| 修 bug | 七、治理流程 + 对应域条目 |
