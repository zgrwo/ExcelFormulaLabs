# 贡献指南

感谢你对 ExcelFormulaLabs 的关注！本指南说明如何参与贡献。

## 快速开始

### 环境要求

- Windows 10/11
- [.NET SDK 8.0](https://dotnet.microsoft.com/download/dotnet/8.0)（含 net48 目标支持）
- Python 3.11+（交叉验证用）：`pip install numpy scipy scikit-learn`
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
4. **本地验证**：确保 5 步验证全部通过
5. **提交 PR**：填写 PR 模板，描述变更内容和测试结果

### 5 步验证（提交前必须通过）

```powershell
# ① 文档一致性（16 项检查）
powershell -File scripts/verify-docs.ps1

# ② 全量单元测试（双 TFM）
dotnet test --verbosity normal

# ③ 交叉验证
dotnet build tests/CrossValRunner
dotnet run --project tests/CrossValRunner -- tests/CrossValRunner/test_manifest.json

# ④ 手册验证
python scripts/verify-manual.py

# ⑤ Release 构建
dotnet build -c Release
```

> 另有两条轻量门禁：`powershell -File scripts/pre-commit-check.ps1`（6 项红线）与
> `powershell -File tests/scripts/run-tests.ps1`（治理脚本自测）。使用 Qoder 本地工具时，修改
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

- **不修改**现有 236 UDF 的公开签名、参数、返回值（数量以 [api-reference.md](rules/api-reference.md) 为准）
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

1. 版本号遵循 [Semantic Versioning](https://semver.org/lang/zh-CN/)
2. bump 版本：`src/Directory.Build.props` 的 `<Version>`（最新 tag 必须等于此值，verify-docs 检查 10 强制）
3. 更新 [CHANGELOG.md](CHANGELOG.md)：新增 `## [x.y.z] - 日期` 条目 + 底部 compare 链接（每个 v* tag 必须有条目，同门禁强制）
4. 提交并推送（提交信息建议 `release: vX.Y.Z`）
5. 打 tag 并推送：`git tag vX.Y.Z && git push origin vX.Y.Z` → release.yml 自动构建、打包 xll、推送 NuGet、创建 GitHub Release

> 发布前本地运行 `scripts/verify-all.ps1`（5 步门）+ `scripts/verify-docs.ps1` 确认全绿。

## 许可证

提交代码即表示你同意以 [MIT License](LICENSE) 授权。
