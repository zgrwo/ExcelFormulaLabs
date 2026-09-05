# build/ — 构建配置说明

> 本目录存放构建/打包配置的说明与文档；CI 工作流在 `.github/workflows/`。
> 结构唯一定义见 [project-structure.md](../docs/governance/project-structure.md)。

## 当前状态

- 构建流程（restore / build / test / pack / xll 收集）全部在 `.github/workflows/ci.yml` 与 `release.yml` 中定义
- 本地一键验证：`scripts/verify-all.ps1`（5 步门：构建 → 测试 → CrossVal → 红线 → Release 构建）

## 约定

- 构建逻辑优先放 CI 工作流与 `scripts/`（可被本地执行与测试），本目录只放说明
- 若引入 MSBuild targets / props 文件（非 src/Directory.Build.props 全局属性），登记到 `docs/governance/project-structure.md` 目录树
