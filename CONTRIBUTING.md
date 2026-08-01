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
# ① 文档一致性
bash scripts/verify-docs.sh

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

- **不修改**现有 220+ UDF 的公开签名、参数、返回值
- **不允许** src/ 下出现裸 `catch {}`
- **不允许**自校验模式 `check(name, X, X)`
- **net8.0 禁止**添加任何 IntelliSense 相关代码
- 异常处理必须使用 `catch when` 排除 OOM/StackOverflow/AccessViolation
- 数值类 UDF 必须有 Python 交叉验证

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

- 版本号遵循 [Semantic Versioning](https://semver.org/lang/zh-CN/)
- 每个版本更新 [CHANGELOG.md](CHANGELOG.md)
- 发布通过 git tag 触发 CI 自动打包

## 许可证

提交代码即表示你同意以 [MIT License](LICENSE) 授权。
