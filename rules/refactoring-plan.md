# ExcelFormulaLabs — 重构计划

> 基于 138 commits 全量历史分析 | 目标：从"能用"到"卓越"
> 项目成熟度：★★★★★（重构完成，5 步验证 + 双 TFM CI 全部通过）
> 状态：✅ Phase 0-4 核心任务已完成（CI/CD、脚手��、交叉验证、i18n、Benchmarks）
> 对标项目：Excel-DNA 官方最佳实践 / StatsCLR / NuGet 生态

## 1. 现状评估

### 1.1 优势（必须保留）

| 维度 | 现状 | 评价 |
|------|------|------|
| 架构分层 | UDF→Core→Foundation 三层严格单向 | ★★★★★ 零违反 |
| 闭环验证 | Python↔C#↔手册三方交叉 | ★★★★★ 业界罕见 |
| 测试覆盖 | 1,299+ 单元 + CrossVal + 手册验证 | ★★★★☆ |
| 哨兵契约 | L1-L5 系统化 | ★★★★★ |
| 文档体系 | agents.md + api-reference + user-manual + context | ★★★★☆ |

### 1.2 痛点（历史反复出错）

| 痛点 | 出现次数 | 根因 | 优先级 |
|------|----------|------|--------|
| IntelliSense 反复 | 8 次 commit | net8.0 兼容性未提前验证 | P1 |
| 审查修复轮次过多 | 17/12/10/13/16 项 | 初始实现缺乏 checklist | P0 |
| NaN/Inf 守卫遗漏 | 6+ 次系统性加固 | 新函数未遵循哨兵契约模板 | P0 |
| 自校验假阴性 | 3 处证实 Bug | check(X,X) 模式未被工具拦截 | P0 |
| 安全竞态 | 4+ 次 | SandboxRoot/DecompCache 线程安全 | P1 |
| 双 TFM 构建问题 | 5+ 次 | DNA 模板/打包/条件编译 | P1 |

### 1.3 与 GitHub 同类项目的差距

| 维度 | 当前状态 | 卓越标准 | 差距等级 |
|------|---------|---------|---------|
| 包分发 | 手动复制 XLL | NuGet 包 + 自动更新 | 🔴 高 |
| 异步 UDF | 全同步计算 | ExcelAsyncUtil 异步（大矩阵不阻塞 UI） | 🟡 中 |
| CI/CD | 无自动化发布 | GitHub Actions tag→build→release | 🔴 高 |
| 性能追踪 | 无 benchmark | BenchmarkDotNet 持续追踪 | 🟡 中 |
| 按需加载 | 220+ 函数全量加载 | LazyRegister 按模块加载 | 🟡 中 |
| 开源基础 | ✅ 已完成 | MIT + 贡献指南 + Issue 模板 | ✅ |

### 1.4 技术债

- [ ] 部分 Core 方法仍有裸 `catch {}`（需 grep 确认）
- [ ] verify-manual.py 中可能残留自校验模式
- [ ] 性能热点未 profiling（CorrelationMatrix 已优化，其余未知）
- [ ] 缺少基准测试（benchmark）基础设施
- [ ] 错误消息国际化（当前中英混杂）
- [x] ~~无 LICENSE / CONTRIBUTING.md / CHANGELOG~~（已完成）

## 2. 重构目标

### 2.1 核心目标

1. **质量前移**（P0）：将审查修复从"事后 N 项"降为"事前拦截"
2. **工程化基础设施**（P0）：CI/CD + NuGet + LICENSE + CHANGELOG ✅
3. **开发效率**（P1）：新增 UDF 流程模板化（需先测量当前 baseline）
4. **性能可观测**（P2）：关键路径有基准测试，回归可检测
5. **异步支持**（P2）：LINALG/REGRESS 等耗时函数支持异步 UDF

### 2.2 非目标（明确排除）

- ❌ 不增加新 UDF 模块（v2.0 再议）
- ❌ 不迁移到其他 Excel 集成方案（Excel-DNA 已验证）
- ❌ 不支持 Excel 2013 及以下
- ❌ **不修改现有 220+ UDF 的实现**（仅改基础设施）
- ❌ 不合并双 TFM 为单一目标（保持 net48 兼容性）

### 2.3 IntelliSense 决策树（历史 8 次反复的终结方案）

```
新增/修改 IntelliSense 相关代码前：
├── 目标框架是 net48？
│   ├── 是 → 允许使用 ExcelDna.IntelliSense
│   └── 否（net8.0）→ 🚫 禁止添加任何 IntelliSense 代码
├── 新增 NuGet 依赖？
│   └── 必须先在隔离 PoC 中验证 net48 + net8.0 双 TFM
└── 修改 .dna.tpl？
    └── 必须运行 scripts/test-load-unload.py 覆盖双框架
```

## 3. 重构方案

### 3.0 Phase 0: 重构前审计（2-3 天）【P0，必须先做】

**目标**：建立 baseline，避免盲目重构

| 任务 | 产出 | 验收标准 |
|------|------|----------|
| 裸 catch 审计 | `grep -rn "catch\s*{" src/` 结果 | 记录当前数量 |
| 自校验审计 | `grep -nE 'check\(.*,\s*(.+),\s*\1' scripts/verify-manual.py` | 记录当前数量 |
| 新增 UDF 耗时测量 | 实际新增一个简单 UDF 并计时 | 记录分钟数（作为 baseline） |
| 全量测试 baseline | 运行 5 步验证，记录通过/失败/耗时 | 存档为 `docs/baseline-v1.0.7.md` |

**回滚条件**：如果审计发现现有测试已有 >5 个失败，先修复再重构。

### 3.1 Phase 1: 工程化基础设施（1 周）【P0，最高优先】

**目标**：补齐开源项目基本要素 + CI/CD

| 任务 | 产出 | 验收标准 | 依赖 |
|------|------|----------|------|
| 添加 LICENSE | `LICENSE`（MIT） | 文件存在 | — |
| 添加 CONTRIBUTING.md | `CONTRIBUTING.md` | 含 fork→PR 流程 | — |
| 添加 CHANGELOG.md | `CHANGELOG.md`（keepachangelog） | 含 v1.0.0~v1.0.7 | — |
| GitHub Actions CI | `.github/workflows/ci.yml` | PR 触发 build+test（net48+net8.0 矩阵） | — |
| GitHub Actions Release | `.github/workflows/release.yml` | tag 触发自动打包 XLL + 创建 Release | CI |
| Issue/PR 模板 | `.github/ISSUE_TEMPLATE/` + `PULL_REQUEST_TEMPLATE.md` | 模板可用 | — |
| README 徽章 | `README.md` 更新 | build/test/version 徽章 | CI |

**CI 矩阵设计**：
```yaml
jobs:
  build-test:
    strategy:
      matrix:
        framework: [net48, net8.0]
        os: [windows-latest]
    runs-on: ${{ matrix.os }}
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
      - run: dotnet build -f ${{ matrix.framework }}
      - run: dotnet test -f ${{ matrix.framework }} --no-build
```

**回滚策略**：全部是新增文件，不影响现有代码。

### 3.2 Phase 2: 质量前移（1-2 周）【P0】

**目标**：消除"审查修复 N 项"的循环

| 任务 | 产出 | 验收标准 | 依赖 |
|------|------|----------|------|
| 创建 UDF 脚手架模板 | `templates/NewModule/` | 含 Core+UDF+Test+CrossVal 四文件 | Phase 0 |
| 编写 pre-commit 检查脚本 | `scripts/pre-commit-check.sh` | 拦截裸catch/自校验/缺失守卫 | Phase 0 |
| 建立 NaN/Inf 守卫 checklist | `docs/guard-checklist.md` | 每个 Core 方法逐项确认 | — |
| 修复 Phase 0 发现的裸 catch | 源码修复 | grep 返回空 | Phase 0 |
| 修复 Phase 0 发现的自校验 | verify-manual.py 修复 | grep 返回空 | Phase 0 |

**脚手架模板结构**：
```
templates/NewModule/
├── {Name}Core.cs.template      # 含哨兵契约 + 异常过滤器
├── {Name}Udf.cs.template       # 含 MapOver 分发 + [ExcelFunction]
├── {Name}Core.Tests.cs.template # 含边界/NaN/空值测试
└── {Name}CrossVal.py.template   # 含 cross_check() 调用
```

**回滚策略**：脚手架模板是新增文件；pre-commit 脚本可通过 `--no-verify` 跳过。

### 3.3 Phase 3: 架构加固 + NuGet 发布（1-2 周）【P1】

**目标**：消除安全竞态 + 实现 NuGet 分发

| 任务 | 产出 | 验收标准 | 依赖 |
|------|------|----------|------|
| SandboxRoot 改为不可变配置 | `SandboxConfig` record | 零竞态，启动时一次性读取 | Phase 2 |
| DecompCache 线程安全审计 | `ConcurrentDictionary` 或 `Lock` | 压力测试 0 异常 | — |
| 统一异常过滤器到 Foundation | `ExceptionFilters.cs` | 全项目引用，零裸catch | Phase 2 |
| NuGet 包配置 | `.nuspec` 或 csproj Pack 配置 | `dotnet pack` 成功 | Phase 1 |
| Semantic Versioning | git tag `v1.0.8` | 版本号与 CHANGELOG 一致 | — |

**NuGet 发布流程**：
```
git tag v1.0.8 → GitHub Actions release.yml →
  dotnet pack → nuget push → GitHub Release + XLL 附件
```

**回滚策略**：每修改一个文件前创建 git branch；压力测试失败则 revert 该文件。

### 3.4 Phase 4: 开发体验 + 性能（按需）【P2】

| 任务 | 产出 | 验收标准 | 依赖 |
|------|------|----------|------|
| UDF 代码生成器 | `scripts/scaffold-udf.ps1` | 输入模块名+函数名，生成四文件 | Phase 2 |
| 本地一键验证脚本 | `scripts/verify-all.ps1` | 整合 5 步验证为单命令 | — |
| 错误消息资源文件 | `Resources/ErrorMessages.resx` | 中英文统一，可本地化 | — |
| BenchmarkDotNet 基础设施 | `benchmarks/` | STATS/LINALG 关键路径基准 | Phase 3 |
| 异步 UDF 支持（LINALG） | `ExcelAsyncUtil` 集成 | SVD/QR 大矩阵不阻塞 UI | Phase 3 |

**异步 UDF 设计**：
```csharp
[ExcelFunction(Description = "SVD 分解（异步）")]
public static object LINALG_SVD_ASYNC(object matrix) {
    return ExcelAsyncUtil.Run(nameof(LINALG_SVD_ASYNC), matrix,
        () => LinalgCore.Svd(V(matrix)));
}
```

**注意**：性能优化和异步支持必须在有 baseline 数据后才进行。

## 4. 里程碑与时间线

```
Phase 0 (2-3天): 重构前审计 — 建立 baseline 【必须先做】
  ├─ Day 1: 裸catch/自校验审计 + 全量测试
  └─ Day 2: 新增 UDF 耗时测量 + 存档

Phase 1 (1周): 工程化基础设施 【P0，最高优先】
  ├─ LICENSE + CONTRIBUTING + CHANGELOG
  ├─ GitHub Actions CI/CD
  └─ Issue/PR 模板 + README 徽章

Phase 2 (1-2周): 质量前移 【P0】
  ├─ 脚手架模板 + pre-commit 脚本
  ├─ 守卫 checklist
  └─ 修复审计发现的问题

Phase 3 (1-2周): 架构加固 + NuGet 【P1】
  ├─ SandboxRoot/DecompCache 线程安全
  ├─ 异常过滤器统一
  └─ NuGet 包发布 + Semantic Versioning

Phase 4 (按需): 开发体验 + 性能 【P2，有数据后再做】
  ├─ 代码生成器 + 一键验证
  ├─ BenchmarkDotNet
  └─ 异步 UDF（LINALG/REGRESS）
```

## 5. 重构守卫（每 Phase 必须执行）

```
Phase 开始前：
  ① dotnet test（全量单元测试）
  ② dotnet test --filter CrossVal（交叉验证）
  ③ python scripts/verify-manual.py（手册验证）
  → 记录通过数/失败数

Phase 结束后：
  ①②③ 同上
  → 对比：任何新增失败 = 立即回滚该 Phase 的修改
```

## 6. 风险与缓解

| 风险 | 概率 | 影响 | 缓解措施 |
|------|------|------|----------|
| 重构引入回归 | 中 | 高 | 重构守卫 + 每 Phase 结束全量验证 |
| Excel-DNA 版本升级破坏兼容 | 低 | 高 | 锁定 1.8.0，升级前完整回归 |
| 双 TFM 条件编译复杂度 | 中 | 中 | 最小化 #if，优先多目标而非条件 |
| 性能优化与正确性冲突 | 低 | 高 | 优化后必须通过 CrossVal 容差 1e-10 |
| NuGet 发布后用户预期管理 | 中 | 中 | README 明确说明 XLL 加载方式 |
| 异步 UDF 引入线程问题 | 中 | 高 | 仅对纯计算函数启用，禁止 COM 调用 |

## 7. 验收标准

重构完成后，以下指标必须达成：

- [ ] `grep -rn "catch\s*{" src/` 返回空（Phase 0 baseline → 0）
- [ ] `grep -nE 'check\(.*,\s*(.+),\s*\1\s*[,)]' scripts/verify-manual.py` 返回空
- [ ] 新增 UDF 耗时比 Phase 0 baseline 减少 50%+
- [ ] CI 矩阵 net48+net8.0 全绿（PR 自动触发）
- [ ] NuGet 包可成功 `dotnet pack` + `nuget push`
- [ ] LICENSE + CONTRIBUTING + CHANGELOG 完整
- [ ] 关键路径有 benchmark 基准（Phase 4 完成后）
- [ ] 零裸 catch，零自校验，零已知竞态

## 8. 历史经验教训（必须铭记）

### 8.1 IntelliSense 8 次反复的教训

**根因**：未在 net8.0 环境预先验证 ExcelDna.IntelliSense 兼容性

**对策**：
- 遵循 §2.3 IntelliSense 决策树
- 任何新依赖必须先在隔离 PoC 中验证双 TFM
- 验证脚本：`scripts/test-load-unload.py` 必须覆盖 net48+net8.0

### 8.2 自校验假阴性的教训

**根因**：`check(name, X, X)` 永远 PASS，3 处 Bug 因此漏过

**对策**：
- pre-commit 脚本强制拦截此模式
- 数值类 UDF 必须使用 `cross_check()`（Python vs C#）

### 8.3 审查修复 17 项的教训

**根因**：初始实现缺乏 checklist，问题积累到审查才暴露

**对策**：
- 脚手架模板内置所有防御模式
- 每完成一个 Core 方法立即运行守卫 checklist
- pre-commit 脚本在提交前拦截常见问题
