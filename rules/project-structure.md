# ExcelFormulaLabs — 项目结构

> 本文件是项目结构的**唯一定义**。新增/删除/移动文件时必须同步更新。

## 目录树

```
ExcelAddin函数库/
│
├── rules/                            # 治理规则（本目录）
│   ├── refactoring-plan.md           #   重构路线图
│   ├── project-structure.md          #   本文件（结构 SSOT）
│   ├── specification.md              #   技术规格
│   └── ...
│
├── src/                              # 源码
│   ├── Foundation/                   # 共享工具层（零 Excel 依赖）
│   │   ├── ElementWiseMapper.cs      #   MapOver/MapOverFlat/MapOverMulti 调度
│   │   ├── InputNormalizer.cs        #   类型转换 + 哨兵契约 (L1-L5)
│   │   ├── OutputWrapper.cs          #   WrapError 异常→#VALUE!
│   │   ├── ExceptionFilters.cs       #   统一异常过滤器（IsCatchable）
│   │   ├── NumericGuard.cs           #   NaN/Inf 矩阵守卫
│   │   ├── ArrayOperations.cs        #   数组基础操作
│   │   ├── FilterUtils.cs            #   过滤工具
│   │   ├── ComparisonUtils.cs        #   比较工具（NaN/Inf 不对称设计）
│   │   ├── DictOperations.cs         #   字典操作
│   │   ├── ExcelEmpty.cs             #   Excel 空值标记
│   │   ├── ExcelError.cs             #   Excel 错误值标记
│   │   └── IsExternalInit.cs         #   net48 record polyfill
│   │
│   ├── Analytics/                    # 统计分析模块 → Analytics-AddIn.xll
│   │   ├── StatsCore.cs / StatsUdf.cs         # STATS.*
│   │   ├── LinalgCore.cs / LinalgUdf.cs       # LINALG.*（含 DecompCache）
│   │   ├── RegressionCore.cs / RegressionUdf.cs # REGRESS.*
│   │   ├── PhyChemCore.cs / PhyChemUdf.cs     # PHYCHEM.*
│   │   ├── AnalyticsHelpers.cs       #   M()/V()/D() 辅助方法
│   │   └── AddIn.cs                  #   AutoOpen/AutoClose + IntelliSense
│   │
│   ├── DataToolkit/                  # 数据处理模块 → DataToolkit-AddIn.xll
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
│   │   ├── AddIn.cs                  #   AutoOpen/AutoClose
│   │   └── IsExternalInit.cs         #   net48 record polyfill
│   │
│   └── Directory.Build.props         # 全局 MSBuild 属性（双 TFM + 包元数据）
│
├── tests/                            # 测试
│   ├── Foundation.Tests/             # Foundation 层单元测试
│   ├── Analytics.Tests/              # Analytics 层单元测试
│   ├── DataToolkit.Tests/            # DataToolkit 层单元测试
│   ├── CrossValRunner/               # C# 交叉验证调度器（输出 JSON）
│   └── TestData/                     # Python 交叉验证参考数据
│
├── docs/                             # 文档
│   ├── api-reference.md              #   UDF 签名唯一信源（数字基准）
│   ├── user-manual.md                #   每函数详细示例 + 结果解读
│   ├── context.md                    #   领域术语表
│   └── guard-checklist.md            #   NaN/Inf 守卫逐项确认清单
│
├── scripts/                          # 构建/验证脚本
│   ├── verify-manual.py              #   全 UDF 手册示例验证（Python↔C#）
│   ├── pre-commit-check.ps1          #   4 项自动检查（裸catch/自校验/IntelliSense/Core隔离）
│   ├── test-load-unload.py           #   XLL 加载/卸载自动化测试
│   ├── update_excel_arguments.py     #   同步 Excel 参数描述
│   └── patch-xll-version.ps1         #   XLL 版本号注入
│
├── templates/NewModule/              # UDF 脚手架模板
│   ├── {Name}Core.cs.template        #   含哨兵契约 + 异常过滤器
│   ├── {Name}Udf.cs.template         #   含 MapOver 分发 + [ExcelFunction]
│   ├── {Name}Core.Tests.cs.template  #   含边界/NaN/空值测试
│   └── {Name}CrossVal.py.template    #   含 cross_check() 调用
│
├── skills/                           # AI Skill 定义（扁平结构）
│   ├── excel-dna-project.md          #   编码规范、架构、MapOver、测试
│   ├── excel-dna-addins.md           #   Excel-DNA UDF/打包/分发
│   ├── architecture-reviewer.md      #   架构审查（YAGNI 四问）
│   ├── refactoring-guardian.md       #   重构守卫（Phase 安全网）
│   └── project-plan-review.md        #   项目规划审查
│
├── .github/
│   ├── workflows/ci.yml              #   CI（5 jobs）
│   ├── workflows/release.yml         #   Release（tag→build→pack→publish）
│   ├── ISSUE_TEMPLATE/               #   Bug/Feature 模板
│   └── PULL_REQUEST_TEMPLATE.md      #   PR 模板
│
├── ExcelFormulaLabs.sln              # 解决方案
├── nuget.config                      # NuGet 源（nuget.org + GitHub Packages）
├── CHANGELOG.md                      # Keep a Changelog
├── CONTRIBUTING.md                   # 贡献指南
├── agents.md                         # 项目宪法 / AI 行为准则
├── README.md                         # 用户向功能指南
├── LICENSE                           # MIT
└── .gitignore                        # 排除规则
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
bin/  obj/  *.xll  *.deps.json  *.runtimeconfig.json
.claude/  .codegraph/  TestResults/  __pycache__/
```
