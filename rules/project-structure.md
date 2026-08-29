# ExcelFormulaLabs — 项目结构

> 本文件是项目结构的**唯一定义**。新增/删除/移动文件时必须同步更新。
> `verify-docs.ps1` 强制检查：目录树声明的每个条目必须真实存在，且与 AGENTS.md 目录树顶层一致。
> 变更目录树后运行：`powershell -File scripts/verify-docs.ps1`。

## 目录树

```
ExcelFormulaLabs/
│
├── .github/                        # GitHub 生态
│   ├── workflows/
│   │   ├── ci.yml                  #   CI（7 jobs：红线/测试net8.0/测试net48/CrossVal/Release/文档/覆盖率）
│   │   ├── release.yml             #   Release（tag → build → pack → publish）
│   │   ├── security.yml            #   CodeQL 安全扫描（定时 + push main）
│   │   └── stale.yml               #   僵尸 Issue/PR 自动关闭
│   ├── ISSUE_TEMPLATE/
│   │   ├── bug_report.yml          #   Bug 报告（结构化表单）
│   │   ├── feature_request.yml     #   功能建议
│   │   ├── docs_request.yml        #   文档改进
│   │   ├── refactor_request.yml    #   重构/质量改进
│   │   └── config.yml              #   表单配置（禁用空白 issue）
│   ├── PULL_REQUEST_TEMPLATE.md    # PR 模板
│   ├── CODEOWNERS                  # 代码所有者（PR 审查路由）
│   └── dependabot.yml              # 依赖自动更新（nuget + github-actions）
│
├── benchmarks/                     # 性能基准（BenchmarkDotNet）
│   └── ExcelFormulaLabs.Benchmarks/
│       ├── ExcelFormulaLabs.Benchmarks.csproj
│       ├── Program.cs
│       ├── LinalgBenchmarks.cs
│       ├── MapOverBenchmarks.cs
│       └── StatsBenchmarks.cs
│
├── build/                          # 构建配置说明
│   └── README.md
│
├── docs/                           # 设计文档
│   ├── README.md
│   ├── cross-validation.md         #   交叉验证方法论
│   └── guard-checklist.md          #   NaN/Inf 守卫逐项确认清单
│
├── examples/                       # 示例
│   └── README.md                   #   交叉验证示例（TestData）使用说明
│
├── logs/                           # 日志（.gitignore 排除）
│
├── rules/                          # 治理规则
│   ├── api-reference.md            #   UDF 签名唯一信源（数字基准）
│   ├── context.md                  #   领域术语表
│   ├── documentation.md            #   文档职责与维护规则
│   ├── project-structure.md        #   本文件（结构 SSOT）
│   ├── specification.md            #   技术规格
│   ├── user-manual.md              #   每函数详细示例 + 结果解读
│   └── adr/                        #   架构决策记录（ADR）
│       ├── adr-template.md
│       ├── 0001-dual-tfm.md
│       ├── 0002-core-zero-excel-dependency.md
│       ├── 0003-mapover-abstraction.md
│       ├── 0004-sentinel-contract-over-exceptions.md
│       ├── 0005-sandboxconfig-immutable.md
│       └── 0006-doe-cross-validation-source.md
│
├── scripts/                        # 构建/验证脚本
│   ├── verify-docs.ps1             #   文档一致性验证（18 项检查，唯一实现）
│   ├── verify-docs.sh              #   verify-docs.ps1 的 POSIX 包装器
│   ├── verify-manual.py            #   全 UDF 手册示例验证（Python↔C#）
│   ├── verify-all.ps1              #   一键 5 步验证门
│   ├── verify-pack.ps1             #   打包验证
│   ├── pre-commit-check.ps1        #   6 项红线检查（裸catch/自校验/IntelliSense/Core隔离/NaN守卫/hasHeaders）
│   ├── run-affected-tests.ps1      #   受影响测试定向运行
│   ├── scaffold-udf.ps1            #   UDF 代码生成器（4 文件模板展开）
│   ├── sync-qoder-skills.ps1       #   skills → .qoder 本地镜像同步/校验（不入库）
│   ├── patch-xll-version.ps1       #   XLL 版本号注入
│   ├── test-load-unload.py         #   XLL 加载/卸载自动化测试
│   ├── test-xll.ps1                #   XLL 冒烟测试
│   ├── update_excel_arguments.py   #   同步 Excel 参数描述
│   ├── validate-commit-msg.sh      #   Conventional Commits 提交信息校验
│   └── git-hooks/
│       └── commit-msg              #   git hook（调用 validate-commit-msg.sh）
│
├── skills/                         # AI Skill 定义（单一信源）
│   ├── README.md
│   ├── excel-dna-project.md        #   编码规范、架构、MapOver、测试
│   ├── excel-dna-addins.md         #   Excel-DNA UDF/打包/分发
│   ├── architecture-reviewer.md    #   架构审查（YAGNI 四问）
│   ├── refactoring-guardian.md     #   重构守卫（Phase 安全网）
│   ├── project-plan-review.md      #   项目规划审查
│   └── project-experience.md       #   经验库（高频陷阱/铁律/证据链）
│
├── src/                            # 源码
│   ├── Directory.Build.props       #   全局 MSBuild 属性（版本 + 包元数据）
│   ├── Foundation/                 #   共享工具层（零 Excel 依赖）
│   │   ├── Foundation.csproj
│   │   ├── ElementWiseMapper.cs    #     MapOver/MapOverFlat/MapOverMulti 调度
│   │   ├── InputNormalizer.cs      #     类型转换 + 哨兵契约 (L1-L5)
│   │   ├── OutputWrapper.cs        #     WrapError 异常→#VALUE!
│   │   ├── ExceptionFilters.cs     #     统一异常过滤器（IsCatchable）
│   │   ├── NumericGuard.cs         #     NaN/Inf 矩阵守卫
│   │   ├── ArrayOperations.cs      #     数组基础操作
│   │   ├── FilterUtils.cs          #     过滤工具
│   │   ├── ComparisonUtils.cs      #     比较工具（NaN/Inf 不对称设计）
│   │   ├── DictOperations.cs       #     字典操作
│   │   ├── ExcelEmpty.cs           #     Excel 空值标记
│   │   ├── ExcelError.cs           #     Excel 错误值标记
│   │   ├── ErrorMsg.cs             #     错误消息访问（资源驱动）
│   │   ├── ErrorMessages.resx      #     错误消息资源（国际化）
│   │   └── IsExternalInit.cs       #     net48 record polyfill
│   │
│   ├── Analytics/                  # 统计分析模块 → Analytics-AddIn.xll
│   │   ├── Analytics.csproj
│   │   ├── AddIn.cs                #     AutoOpen/AutoClose + IntelliSense
│   │   ├── AnalyticsHelpers.cs     #     M()/V()/D() 辅助方法
│   │   ├── StatsCore.cs / StatsUdf.cs          # STATS.*
│   │   ├── LinalgCore.cs / LinalgUdf.cs        # LINALG.*（含 DecompCache）
│   │   ├── RegressionCore.cs / RegressionUdf.cs # REGRESS.*
│   │   ├── PhyChemCore.cs / PhyChemUdf.cs      # PHYCHEM.*
│   │   ├── DoeCore.cs / DoeUdf.cs              # DOE.*（设计生成）
│   │   ├── DoeAnalysisCore.cs / DoeAnalysisUdf.cs # DOE.*（效应/ANOVA/Pareto 分析）
│   │   ├── LinalgAsyncUdf.cs       #     异步线性代数入口
│   │   ├── RegressionAsyncUdf.cs   #     异步回归入口
│   │   ├── Analytics-AddIn-net48.dna.tpl
│   │   └── Analytics-AddIn-net8.dna.tpl
│   │
│   ├── DataToolkit/                # 数据处理模块 → DataToolkit-AddIn.xll
│   │   ├── DataToolkit.csproj
│   │   ├── AddIn.cs                #     AutoOpen/AutoClose
│   │   ├── NativeDllStore.cs        #     原生 DLL 内容寻址提取（SHA-256 + 原子替换）
│   │   ├── StringCore.cs / StringUdf.cs       # STR.*
│   │   ├── DateTimeCore.cs / DateTimeUdf.cs   # DT.*
│   │   ├── RegexCore.cs / RegexUdf.cs         # REGEX.*
│   │   ├── ArrayCore.cs / ArrayUdf.cs         # ARR.*
│   │   ├── DictSetCore.cs / DictSetUdf.cs     # DICT.*
│   │   ├── JsonXmlCore.cs / JsonXmlUdf.cs     # JSON.* XML.*
│   │   ├── PivotCore.cs / PivotUdf.cs         # PIVOT.*
│   │   ├── SqlCore.cs / SqlUdf.cs             # SQL.*
│   │   ├── FileSystemCore.cs / FileSystemUdf.cs # FS.*（含 SandboxConfig）
│   │   ├── RangeExportCore.cs / RangeExportUdf.cs # RANGE.*
│   │   ├── IsExternalInit.cs       #     net48 record polyfill
│   │   ├── DataToolkit-AddIn-net48.dna.tpl
│   │   └── DataToolkit-AddIn-net8.dna.tpl
│   │
│   └── (bin/ obj/ 不入库)
│
├── templates/                      # 模块脚手架
│   ├── README.md
│   └── NewModule/
│       ├── {Name}Core.cs.template  #   含哨兵契约 + 异常过滤器
│       ├── {Name}Udf.cs.template   #   含 MapOver 分发 + [ExcelFunction]
│       ├── {Name}Core.Tests.cs.template  # 含边界/NaN/空值测试
│       └── {Name}CrossVal.py.template    # 含 cross_check() 调用
│
├── tests/                          # 测试
│   ├── Foundation.Tests/           #   Foundation 层单元测试（含 csproj 与 13 个测试文件）
│   ├── Analytics.Tests/            #   Analytics 层单元测试（含 csproj 与 17 个测试文件）
│   ├── DataToolkit.Tests/          #   DataToolkit 层单元测试（含 csproj 与 20 个测试文件）
│   ├── CrossValRunner/             #   C# 交叉验证调度器
│   │   ├── CrossValRunner.csproj
│   │   ├── Program.cs
│   │   ├── Dispatcher.cs
│   │   ├── ResultSerializer.cs
│   │   ├── TestManifest.cs
│   │   └── test_manifest.json
│   ├── TestData/                   #   Python 交叉验证参考数据
│   │   ├── Cross_Validation_vs_Python.xlsx
│   │   └── generate_python_refs.py
│   └── scripts/                    #   治理脚本自测（P0-5 回归守卫）
│       ├── run-tests.ps1
│       ├── test_precommit_check.ps1
│       └── test_verify_docs.ps1
│
├── tools/                          # 辅助工具
│   ├── .gitkeep
│   └── README.md
│
├── ExcelFormulaLabs.sln            # 解决方案
├── nuget.config                    # NuGet 源（nuget.org + GitHub Packages）
├── requirements.txt                # Python 交叉验证依赖固定（CI + 贡献者）
├── AGENTS.md                       # 项目宪法 / AI 行为准则
├── README.md                       # 用户向功能指南
├── README.en.md                    # 英文入口
├── CHANGELOG.md                    # Keep a Changelog（版本条目与 tag 强制一致）
├── CONTRIBUTING.md                 # 贡献指南（含提交规范与发版流程）
├── CODE_OF_CONDUCT.md              # 行为准则
├── SECURITY.md                     # 安全政策
├── LICENSE                         # MIT
├── FUNDING.yml                     # 资助信息
├── .editorconfig                   # 编辑器统一风格
├── .gitattributes                  # 换行符/二进制标记
├── .gitignore                      # 排除规则
└── .pre-commit-config.yaml         # 提交前 lint（可选启用）
```

## 架构分层

```
UDF 层 (public static, [ExcelFunction])  ← 入口：仅分发与适配
  ↓ MapOver / MapOverMulti / V() 分发
Core 层 (internal static, 纯逻辑)       ← 零 Excel 依赖
  ↓ 依赖
Foundation (共享工具)                    ← InputNormalizer, ElementWiseMapper, OutputWrapper
```

## 命名约定

| 模式 | 说明 | 示例 |
|------|------|------|
| `{Name}Core.cs` | 纯逻辑，internal static | StatsCore.cs |
| `{Name}Udf.cs` | UDF 入口，public static | StatsUdf.cs |
| `{Name}Helpers.cs` | 辅助方法 | AnalyticsHelpers.cs |

## 不入库

```
bin/  obj/  *.xll  *.deps.json  *.runtimeconfig.json  *.cache  *.user  *.suo
TestResults/  __pycache__/  BenchmarkDotNet.Artifacts/  coverage*/
logs/（目录保留，内容不入库）  .qoder/（Qoder 本地工具配置与技能镜像，本地保留）
```