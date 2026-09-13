# build/ — 构建配置说明

> 本目录存放构建/打包配置的说明与文档；CI 工作流在 `.github/workflows/`。
> 结构唯一定义见 [project-structure.md](../docs/governance/project-structure.md)。

## 当前状态

- 构建流程（restore / build / test / pack / xll 收集）全部在 `.github/workflows/ci.yml` 与 `release.yml` 中定义
- 本地一键验证：`scripts/verify-all.ps1`（6 步门：verify-docs → 构建 → 测试 → CrossVal → 红线 → Release 构建；R5-P3-33，2026-09-06 同步）
- 已知环境噪声（R7-3/R7-4，2026-09-13 发行前审查）：ExcelDnaPack 在 Windows 上偶发
  `Win32Exception (110) EndUpdateResource`（Defender 实时扫描刚写出的 `.xll` 造成瞬时锁），
  重试即可通过——`verify-all.ps1` 的构建步骤（2/6、6/6）已内置 1 次自动重试，CI 的 Release
  构建步骤同样重试 1 次。若中断发生在打包阶段，publish 目录可能残留跨 TFM 过期 XLL 或
  base 尺寸坏产物；`verify-all.ps1` 构建后会对 4 个模块/TFM 逐一运行 `verify-pack.ps1`
  拦截（Debug/Release 均覆盖），必要时清理 `src/<Module>/bin/<Config>` 后重建。

## 约定

- 构建逻辑优先放 CI 工作流与 `scripts/`（可被本地执行与测试），本目录只放说明
- 若引入 MSBuild targets / props 文件（非 src/Directory.Build.props 全局属性），登记到 `docs/governance/project-structure.md` 目录树
