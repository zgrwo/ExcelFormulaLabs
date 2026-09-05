# AGENTS.md — ExcelFormulaLabs 项目宪法

> Excel 函数增强库：236 UDF，基于 C# / Excel-DNA，双 TFM (net48 + net8.0)。
> 本文件面向 AI 编程助手，编码细节按需加载 Skill。术语见 [context.md](docs/governance/context.md)。

## 元数据

- **项目名**：ExcelFormulaLabs
- **GitHub**：https://github.com/zgrwo/ExcelFormulaLabs
- **语言**：C#（文档与注释默认中文）
- **术语**：[context.md](docs/governance/context.md)
- **数字唯一基准**：[api-reference.md](docs/specification/api-reference.md) — 236 UDF 签名以此为准
- **SSOT**：每个事实只在一处定义，其余仅链接引用

## 四条核心准则

### 1. 先想后写 (Think Before Coding)

- **不确定就提问**。不要猜测业务规则——去查 specification。
- **说出来你做假设了**。"假设 X 不超过 5 级 → 代码按此编写。"
- **主动呈现权衡**。"两种方案：A 简单 O(N)，B 复杂 O(1)。当前规模下 A 足够。"
- **发现架构偏离时停下来**。例如：发现自己在 Core 层引用了 ExcelDna → 停下，走 UDF 层。

### 2. 简洁至上 (Simplicity First)

- **最少代码解决问题**。
- **不为一成不变的场景建抽象层**。
- **自检**：一个资深开发者看这段代码会觉得过度设计吗？如果是，简化。

### 3. 精准修改 (Surgical Changes)

- **只改该改的**。不要顺带重构无关代码。
- **匹配现有风格**。
- **发现无关问题时提出来，不擅自改**。

### 4. 目标驱动 (Goal-Driven Execution)

- **先定义验证方式，再开始写代码**。
- 将指令转化为可验证目标：

| 而不是 | 而是 |
|--------|------|
| "添加 UDF" | "新 UDF 通过 CrossVal 与 Python 独立实现一致。去验证。" |
| "修复 Bug" | "复现测试 FAILS → 修复后 PASSES + 无回归。去验证。" |

## 技能加载

修改代码前**必须**加载对应 Skill：

| 范围 | Skill 文件 | 内容 |
| :--- | :--- | :--- |
| Foundation / Analytics / DataToolkit / 编码规范 | `skills/excel-dna-project.md` | 编码规范、架构、MapOver 变体、表头/哨兵契约、测试模式 |
| UDF / .xll 打包 / 分发 | `skills/excel-dna-addins.md` | UDF 声明规范、Excel-DNA 黄金法则、打包流程 |
| 修改依赖/门禁/数值/发版（速查） | `skills/project-experience.md` | 高频陷阱与铁律（版本臆测、pwsh7 差异、8.3 短路径、数值溢出） |

### 专家 Skill（重构生命周期）

| 阶段 | Skill | 触发时机 |
|------|-------|----------|
| 决策前 | `skills/architecture-reviewer.md` | 新增组件/层级/依赖前 |
| 执行中 | `skills/refactoring-guardian.md` | 每个 Phase 开始/结束时 |
| 执行后 | `skills/project-plan-review.md` | 里程碑复盘/规划评审时 |

## 架构分层

```
UDF 层 (public static, [ExcelFunction])  ← 入口：仅分发与适配
  ↓ MapOver / MapOverMulti / V() 分发
Core 层 (internal static, 纯逻辑)       ← 零 Excel 依赖
  ↓ 依赖
Foundation (共享工具)                    ← InputNormalizer, ElementWiseMapper, OutputWrapper
```

- ✅ UDF 不包含业务逻辑；Core 不引用 `ExcelDna.Integration`
- ❌ 禁止跨层直接调用或反向依赖

## 仓库目录树

> 路由地图：所有文件路径均以此为基准。详细结构见 [project-structure.md](docs/governance/project-structure.md)。

```
ExcelFormulaLabs/
├── src/                          # 源码（Foundation / Analytics / DataToolkit）
├── tests/                        # 测试 + CrossVal + 脚本自测（tests/scripts）
├── docs/                         # 项目文档（governance / specification / user-manual / adr 四分类）
├── skills/                       # Skill 定义（单一信源；.qoder 本地镜像不入库）
├── scripts/                      # 构建/验证/治理脚本
├── templates/                    # 模块脚手架（NewModule）
├── benchmarks/                   # 性能基准（BenchmarkDotNet）
├── build/                        # 构建配置说明
├── .github/                      # CI 工作流 + Issue/PR 模板 + CODEOWNERS + dependabot
├── logs/                         # 日志；审查报告（reports/release/probes 三分类，不入库）
├── AGENTS.md                     # 本文件
├── README.md                     # 用户向功能指南
├── README.en.md                  # 英文入口
├── CHANGELOG.md                  # 版本变更记录（每个 v* tag 必须有条目，verify-docs 强制）
├── CONTRIBUTING.md               # 贡献指南（提交规范 + 发版流程）
├── CODE_OF_CONDUCT.md            # 行为准则
├── SECURITY.md                   # 安全政策
├── LICENSE                       # MIT
├── FUNDING.yml                   # 资助信息
├── nuget.config                  # NuGet 源
├── ExcelFormulaLabs.sln          # 解决方案
├── .editorconfig                 # 编辑器统一风格
├── .gitattributes                # 换行符/二进制标记
├── .gitignore                    # 排除规则
└── .pre-commit-config.yaml       # 提交前 lint（可选启用）
```

> **目录树变更管控**：顶层目录与 AGENTS.md / project-structure.md 双树必须同步（verify-docs 检查 15 强制）。

## 红线规则

### 1. 接口与兼容性

| ✅ DO | ❌ DON'T |
| :--- | :--- |
| 保持 Public 签名、UDF 参数/返回值不变 | 修改公开接口或破坏双 TFM 兼容 |
| `#if NET48` 仅限内部实现 | 在签名/参数上使用条件编译 |
| 新增 NuGet 前确认双 TFM 可用 | 引入单框架依赖 |

### 2. 防错三原则

| 原则 | 核心 |
| :--- | :--- |
| **静默传播阻断** | 显式守卫 NaN/Inf/null/default!，WrapError 不兜底 |
| **防御完整性** | ValidatePath / Regex Timeout / SQL 参数化 覆盖所有方法 |
| **异常过滤器** | `catch when` 排除 OOM/StackOverflow/AccessViolation |

> `grep -rn "catch\s*{" src/ --include="*.cs"` 必须返回空。

### 3. IntelliSense 框架隔离

- ✅ net48：启用 ExcelDna.IntelliSense
- ❌ net8.0：**禁止添加 IntelliSense 代码**（Excel-DNA Issue #343）

### 4. 表头行契约

所有接受 `object[,]` 的 Core 方法必须含 `bool hasHeaders = true`。
豁免：纯结构变换（Transpose / SelectColumns / SelectRows / CrossJoin / Flatten2D / Count / Keys / Values）不解释表头语义，无需该参数。

### 5. 哨兵契约（L1-L5）

不可转换值返回类型零值哨兵，不抛异常。未知类型 Convert 失败：double→NaN，其余必须 throw。

### 6. 闭环验证（Python ↔ C# ↔ 手册）

- 禁止自校验 `check(name, X, X)`
- 数值类 UDF 必须 `cross_check()`
- 修改后运行全量验证 5 步

## 构建与测试

| 场景 | 命令 |
| :--- | :--- |
| 日常构建 | `dotnet restore && dotnet build && dotnet test` |
| 分发构建 | `dotnet build -c Release` |
| 全量测试 | ① verify-docs ② dotnet test ③ CrossVal ④ verify-manual.py ⑤ Release build |
| 文档一致性（18 项） | `powershell -File scripts/verify-docs.ps1` |
| 提交前红线（6 项） | `powershell -File scripts/pre-commit-check.ps1` |
| 治理脚本自测 | `powershell -File tests/scripts/run-tests.ps1` |
| 本地 Qoder 技能镜像 | `powershell -File scripts/sync-qoder-skills.ps1`（可选，本地工具用，不入库） |

## 提交规范（Conventional Commits）

- 所有提交信息必须符合 Conventional Commits：`type(scope): 描述`。
- 允许类型：`feat fix docs style refactor test chore build ci perf revert release`。
- 校验脚本：`scripts/validate-commit-msg.sh`（本地 hook：`scripts/git-hooks/commit-msg`；CI 对 PR 内每个提交强制执行）。
- 发版流程：bump `src/Directory.Build.props` 版本 + 更新 `CHANGELOG.md` + 打 `vX.Y.Z` tag → release.yml 自动构建发布。
- **版本一致性**：最新 `v*` tag 必须等于 `Directory.Build.props` 的 `<Version>`，且 CHANGELOG 必须有对应条目（verify-docs 检查 10 强制）。

## AGENTS.md 生态兼容

- 本文件即 `AGENTS.md`（大写）——2026 年跨工具事实标准（Codex / Copilot / Windsurf / JetBrains / Gemini / QoderCN 均可直接读取）。
- Claude Code 需要 `CLAUDE.md` 副本：`Copy-Item AGENTS.md CLAUDE.md`（每次修改 AGENTS.md 后需重新创建；CLAUDE.md 不入库登记）。
- 子目录级 AGENTS.md（多模块仓库时）：写清「你只管 X，不要碰 Y」，越靠近当前目录优先级越高。
- **Agent 看不见的事实**（写在文档而不依赖代码推断）：包管理/构建工具选择（dotnet SDK 8.0）、生成文件目录（bin/ obj/ BenchmarkDotNet.Artifacts/ 禁止手改）、聚焦测试命令（改单模块先跑 `tests/<Module>.Tests`）、安全边界（FS 沙箱默认关闭、NuGet push 与 Release 需人工确认）、修改模块边界前必读 `docs/specification/api-reference.md`。

## 历史经验（从 diff 提炼）

### 高频修复模式

| 模式 | 出现次数 | 根因 |
|------|----------|------|
| NaN/Inf 守卫缺失 | 10+ | 初始实现未考虑退化输入；**NaN/Inf/溢出三路径都要守卫**（P1-4 教训：只修 NaN 分支不修 Inf 路径 = 同类缺陷复活） |
| 绝对阈值误判小量纲 | 6 处 | `va < 1e-15` 等常数判据与数据量纲无关——ppm/ppb 数据方差天然 < 1e-15 → 误判常量（P1-5）；判据必须与数据同尺度（精确零/相对阈） |
| 正规方程条件数平方 | 1 | `X'X.Solve` 把 cond(X) 平方——cond>1e8 时 r²≈1 但系数静默全错（P0-1）；回归求解必须 QR/SVD，标准误由 R⁻¹ 求 |
| IntelliSense 反复尝试 | 8 次 | Excel-DNA net8.0 已知 bug |
| 文档数字不一致 | 5+ | 多处硬编码计数（verify-manual.py 的 224 与 section 声明 221 矛盾） |
| 交叉验证自校验 | 3 处 | check(X,X) 假阴性；77% 的 check() 是纯 Python 自校验（P0-3b），C# 未参与 |
| long[] 封送失败 | 2 处 | Excel-DNA 不支持 long[] |

### 审查逃逸预防（2026-08-31 深度审查复盘，详见 project-experience §八）

- **门禁补洞**：verify-docs 检查 16 曾只匹配英文 `N UDF`，中文「N 个 UDF」漂移 236→999 全绿通过（P0-4）；版本头/CHANGELOG 链接行/覆盖数每新增一个"声称"就加一条检查
- **数值三律**：判据必须相对；回归禁止正规方程；NaN/Inf/溢出三路径全守卫——写代码/审查时 `grep -rn "< 1e-" src/` 逐条核对
- **测试铁律**：期望必须硬编码（禁自校验）；断言必须有效（禁 NotBeNull/零断言）；复现测试必须进正式测试文件（审计临时件 _AUDIT_Verify 自身曾有 bug：A1 用例不可达、A6 漏 ±1 负号）
- **交叉验证**：特殊值带标签（`{"__nan__":true}`）；manual/cross 双通道分别汇报；manifest tolerance 必须被消费
- **环境归因**："间歇性失败"先查 bin/obj 陈旧产物（.dna 残留曾冒充构建竞态）再归因代码

### 关键设计决策

- 双 TFM 而非单框架：覆盖 .NET Framework 4.8（免安装）和 .NET 8（性能）
- MapOver 三变体：标量/数组/多参数广播统一处理
- Python 交叉验证：独立实现比对，消除"自己验证自己"的假阴性

## 开发流程

### 修改前（强制）

1. **Read** 对应 Skill 文件（Skill-first，不凭记忆编造实现方式）
2. 检查调用者与影响范围
3. 确认不违反红线规则

### 修改后

- 验证与调用方一致（签名/返回值/异常传播链路）
- 运行构建 + 测试确认无回归
- 缺陷处理：追溯根因 → 写入 memory（禁止仅修表面）

### 遇到 Bug 时

1. 写最小复现测试 → confirm: 测试 FAILS（Bug 存在）
2. 修复 → confirm: 复现测试 PASSES + 已有测试无回归
3. **保留复现测试**（它现在是回归守卫）
4. 检查是否需要更新 spec / skill / review

### 提交前必检

- [ ] 所有新代码有对应的测试
- [ ] 无跨层/跨线程违规
- [ ] 命名空间与文件夹一致
- [ ] 没动无关文件
- [ ] `dotnet build` 双 TFM 通过 + `dotnet test` 全绿

## 防幻觉铁律

| 铁律 | 说明 |
|------|------|
| **不靠记忆引用文档** | 每次引用 docs/ 或 skills/ 中的内容时，先 Read/Grep 确认 |
| **不确定 = 承认不确定** | 不要编造业务规则；说“我需要在 spec 中确认”然后去查 |
| **写过的代码 = 读过的代码** | 不要假设自己知道某个文件内容——Read 它再改 |
| **版本号是事实锚点** | 每个结论标注来源文档版本 |

## 会话管理

### 何时自查

- **每完成一个独立功能点** — 对照四条核心准则自检
- **上下文超过 5 个文件 / 20 轮对话** — 提醒用户开新会话
- **反复纠正同一个错误时** — 停下来写进文档或更新规则

### 跨会话接力

```
上一个会话结束时 → 在回复末尾简述：
  ✅ 已完成: [具体交付物]
  🔜 下一步: [下一动作 + 涉及文件]
  ⚠️ 待决策: [阻塞项]
  📄 关键上下文: [后续会话必须知道的约束/假设]
```

### 基本原则

- 新会话先读本文件 + 对应 Skill
- 跨会话通过 git commit 衔接，不依赖对话历史
- 每个 commit 应自包含、可追溯

## 参考

| 文档 | 角色 | 内容 |
| :--- | :--- | :--- |
| [README.md](README.md) | 用户入口 | 安装、模块速览、使用模式 |
| [README.en.md](README.en.md) | 英文入口 | 国际用户入口 |
| [context.md](docs/governance/context.md) | 术语表 | 所有术语唯一定义 |
| [specification.md](docs/specification/specification.md) | 技术规格 | 项目概述、模块清单、功能规格 |
| [api-reference.md](docs/specification/api-reference.md) | 数字唯一信源 | 236 UDF 签名、参数、错误行为 |
| [user-manual.md](docs/user-manual/user-manual.md) | 学习教程 | 每函数详细示例 + 结果解读 |
| [project-structure.md](docs/governance/project-structure.md) | 结构地图 | 文件职责与层级关系 |
| [documentation.md](docs/governance/documentation.md) | 文档职责 | 各文档分工与维护规则 |
| [adr/](docs/adr/adr-template.md) | 决策记录 | 架构决策 ADR 0001-0006 |
| [CHANGELOG.md](CHANGELOG.md) | 变更记录 | 版本变更历史（与 tag 强制一致） |
| [CONTRIBUTING.md](CONTRIBUTING.md) | 贡献指南 | 开发/PR/发版流程 |
