# 贡献指南

感谢你对 ExcelFormulaLabs 的关注！本指南说明如何参与贡献。

## 快速开始

### 环境要求

- Windows 10/11
- [.NET SDK 8.0](https://dotnet.microsoft.com/download/dotnet/8.0)（含 net48 目标支持）
- Python 3.11+（交叉验证用）：`pip install -r requirements.txt`（numpy / scipy / scikit-learn / pyDOE2，版本由 dependabot 维护）
- Git

### 构建与测试

```powershell
# 克隆
git clone https://github.com/zgrwo/ExcelFormulaLabs.git
cd ExcelFormulaLabs

# 构建（双 TFM）
dotnet restore
dotnet build

# 运行全量测试
dotnet test --verbosity normal

# 交叉验证（需先构建 CrossValRunner）
dotnet build tests/CrossValRunner
python scripts/verify-manual.py
```

## 贡献流程

1. **Fork** 本仓库到你的 GitHub 账户
2. **创建分支**：`git checkout -b fix/描述` 或 `feat/描述`
3. **编写代码**（遵循下方规范）
4. **本地验证**：确保 6 步验证全部通过
5. **提交 PR**：填写 PR 模板，描述变更内容和测试结果

### 6 步验证（提交前必须通过，与 `scripts/verify-all.ps1` 同序）

```powershell
# ① 文档一致性（20 个编号项；运行时断言数见脚本输出）
powershell -File scripts/verify-docs.ps1

# ② 构建（双 TFM）
dotnet build

# ③ 全量单元测试（双 TFM）
dotnet test --verbosity normal

# ④ 交叉验证（verify-manual.py 内部运行 CrossValRunner.exe 做 C# 对照）
python scripts/verify-manual.py

# ⑤ 提交前红线（6 项）
powershell -File scripts/pre-commit-check.ps1

# ⑥ Release 构建
dotnet build -c Release
```

> 另有治理脚本自测：`powershell -File tests/scripts/run-tests.ps1`。使用 Qoder 本地工具时，修改
> `skills/` 后运行 `powershell -File scripts/sync-qoder-skills.ps1` 同步本地 .qoder 镜像（不入库）。

## 编码规范

### 架构分层（严格单向依赖）

```
UDF 层 (public static, [ExcelFunction])  ← 仅分发与适配
  ↓
Core 层 (internal static, 纯逻辑)       ← 零 Excel 依赖
  ↓
Foundation (共享工具)                    ← InputNormalizer, ElementWiseMapper, OutputWrapper
```

### 红线规则

- **不修改**现有 240 UDF 的公开签名、参数、返回值（数量以 [api-reference.md](docs/specification/api-reference.md) 为准）
- **不允许** src/ 下出现裸 `catch {}`
- **不允许**自校验模式 `check(name, X, X)`
- **net8.0 禁止**添加任何 IntelliSense 相关代码
- 异常处理必须使用 `catch when` 排除 OOM/StackOverflow/AccessViolation
- 数值类 UDF 必须有 Python 交叉验证
- 接受 `object[,]` 的 Core 方法必须含 `bool hasHeaders = true`（纯结构变换豁免）

## 提交规范（Conventional Commits）

所有提交信息必须符合 Conventional Commits 格式：

```
type(scope): 描述        # 如 fix(engine): 修复 anova 效应量计算
```

- 允许类型：`feat fix docs style refactor test chore build ci perf revert release`
- 标题 ≤ 72 字符；`Merge` / `fixup!` / `Revert` 前缀提交跳过校验
- 本地安装提交校验 hook（可选但推荐）：

```powershell
git config core.hooksPath scripts/git-hooks
```

- CI 会对 PR 内每个提交强制校验（不通过 = PR 无法合并）

### 命名约定

| 模式 | 说明 | 示例 |
|------|------|------|
| `{Name}Core.cs` | 纯逻辑，internal static | StatsCore.cs |
| `{Name}Udf.cs` | UDF 入口，public static | StatsUdf.cs |
| `{Name}Helpers.cs` | 辅助方法 | AnalyticsHelpers.cs |

### 哨兵契约

所有 Core 方法对不可转换值返回类型零值哨兵，不抛异常：
- `double` → `NaN`
- `string` → `""`
- 未知类型 Convert 失败 → 必须 throw

## Issue 规范

- **Bug 报告**：使用 Bug Report 模板，包含复现步骤和期望行为
- **功能请求**：使用 Feature Request 模板，说明使用场景
- 提交前请搜索已有 Issue，避免重复

## 版本发布

发版由 [release-please](https://github.com/googleapis/release-please) 自动化（2026-09-23 起）：

1. 版本号遵循 [Semantic Versioning](https://semver.org/lang/zh-CN/)
2. 日常只需合并符合 Conventional Commits 的 PR；push main 后 release-please 自动维护 Release PR
   （bump `version.txt` + `src/Directory.Build.props` 的 `<Version>` + `CHANGELOG.md`
   + 3 份文档版本头：`docs/specification/specification.md`、`docs/user-manual/user-manual.md`、
   `docs/specification/api-reference.md`，经 `x-release-please-version` 注释锚点自动替换）
3. 合并 Release PR → 自动打 `vX.Y.Z` tag、创建 GitHub Release，并触发 release.yml 构建/测试/打包/推送 NuGet
4. 版本一致性由 `verify-docs.ps1` 强制：最新 tag == `<Version>` == `version.txt`，CHANGELOG 必有对应条目，
   文档版本头 == `<Version>`（新增/移动文档版本头时须保留 `x-release-please-version` 锚点，否则下次发版漂移）

> **Release PR 无 CI 检查（已知边界，review-2026-09-24 R1-14）**：GITHUB_TOKEN 创建的
> Release PR 触发的 workflow runs 停留在 `action_required`（GitHub 对 bot 事件的默认拦截），
> 因此 Release PR 上**没有** CI/安全/文档门禁信号。合并前请在本地运行 `scripts/verify-all.ps1`
> （6 步门）+ `scripts/verify-docs.ps1` 确认全绿；真正的全量验证在 tag 后的 release.yml 中强制执行。
> 如需在 Release PR 上获得检查信号，需仓库管理员在 Settings → Actions 允许 bot PR 自动运行 workflow。
>
> **紧急手工发版**：bump `version.txt` 与 `src/Directory.Build.props` 的 `<Version>` + 更新 CHANGELOG + 3 份文档版本头 → 提交 →
> `git tag vX.Y.Z && git push origin vX.Y.Z`（tag 触发 release.yml 同流程）。
> 发布前本地运行 `scripts/verify-all.ps1`（6 步门）+ `scripts/verify-docs.ps1` 确认全绿。
> **前置设置**：仓库 Settings → Actions → General → "Allow GitHub Actions to create and approve pull requests" 必须启用
> （GITHUB_TOKEN 创建 Release PR 所需，2026-09-23 已启用）。

## 许可证

提交代码即表示你同意以 [MIT License](LICENSE) 授权。
