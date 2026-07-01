# CLAUDE.md — 项目宪法

> 全局架构、绝对红线与核心流程。编码细节按需加载 Skill。术语见 [context.md](docs/context.md)。

## 元数据

- **语言**：文档与注释默认中文
- **术语**：[context.md](docs/context.md) — 所有领域词汇唯一定义
- **数字唯一基准**：[api-reference.md](docs/api-reference.md) — UDF 签名与错误行为以此为准
- **信息单一事实来源**：每个事实只在一处定义，其余仅链接引用

## 技能加载

修改代码前**必须**加载对应 Skill：

| 范围 | Skill 文件 | 内容 |
| :--- | :--- | :--- |
| Foundation / Analytics / DataToolkit / 编码规范 | `skills/excel-dna-project/skill.md` | 编码规范、架构、MapOver 变体、表头/哨兵契约、测试模式、全量测试命令 |
| UDF / .xll 打包 / 分发 | `skills/excel-dna-addins/skill.md` | UDF 声明规范、Excel-DNA 黄金法则、打包流程 |

> **执行方式**：直接 `Read` skill 文件（Skill 工具无法加载项目级 skill）。

## 架构分层

```
UDF 层 (public static, [ExcelFunction])  ← 入口：仅分发与适配
  ↓ MapOver / MapOverMulti / V() 分发
Core 层 (internal static, 纯逻辑)       ← 零 Excel 依赖
  ↓ 依赖
Foundation (共享工具)                    ← InputNormalizer, ElementWiseMapper, OutputWrapper, …
```

- ✅ UDF 不包含业务逻辑；Core 不引用 `ExcelDna.Integration`
- ❌ 禁止跨层直接调用或反向依赖

## 仓库目录树

> 路由地图：所有文件路径均以此为基准。推送规则见 [Git](#git)。

```
ExcelFormulaLabs/
│
├── src/                          # ✅ 源码
│   ├── Foundation/               # 共享工具层
│   │   ├── ElementWiseMapper.cs   # MapOver/MapOverFlat/MapOverMulti 调度
│   │   ├── InputNormalizer.cs     # 类型转换 + 哨兵 (L1-L5)
│   │   ├── OutputWrapper.cs       # WrapError 异常→#VALUE!
│   │   ├── NumericGuard.cs        # NaN/Inf 矩阵守卫
│   │   ├── ArrayOperations.cs     # 数组基础操作
│   │   ├── FilterUtils.cs         # 过滤工具
│   │   ├── ComparisonUtils.cs     # 比较工具
│   │   ├── DictOperations.cs      # 字典操作
│   │   ├── ExcelEmpty.cs          # Excel 空值标记
│   │   └── ExcelError.cs          # Excel 错误值标记
│   ├── Analytics/                 # 统计分析模块 → Analytics-AddIn.xll
│   │   ├── StatsCore.cs / StatsUdf.cs         # STATS.*
│   │   ├── LinalgCore.cs / LinalgUdf.cs       # LINALG.*
│   │   ├── RegressionCore.cs / RegressionUdf.cs # REGRESS.*
│   │   ├── PhyChemCore.cs / PhyChemUdf.cs     # PHYCHEM.*
│   │   ├── AnalyticsHelpers.cs                # M()/V()/D() 辅助
│   │   └── AddIn.cs                            # AutoOpen/AutoClose
│   ├── DataToolkit/               # 数据处理模块 → DataToolkit-AddIn.xll
│   │   ├── StringCore.cs / StringUdf.cs       # STR.*
│   │   ├── DateTimeCore.cs / DateTimeUdf.cs   # DT.*
│   │   ├── RegexCore.cs / RegexUdf.cs         # REGEX.*
│   │   ├── ArrayCore.cs / ArrayUdf.cs         # ARR.*
│   │   ├── DictSetCore.cs / DictSetUdf.cs     # DICT.*
│   │   ├── JsonXmlCore.cs / JsonXmlUdf.cs     # JSON.* XML.*
│   │   ├── PivotCore.cs / PivotUdf.cs         # PIVOT.*
│   │   ├── SqlCore.cs / SqlUdf.cs             # SQL.*
│   │   ├── FileSystemCore.cs / FileSystemUdf.cs # FS.*
│   │   ├── RangeExportCore.cs / RangeExportUdf.cs # RANGE.*
│   │   └── AddIn.cs                            # AutoOpen/AutoClose
│   └── Directory.Build.props      # 全局 MSBuild 属性
│
├── tests/                         # ✅ 测试
│   ├── Foundation.Tests/
│   ├── Analytics.Tests/           # + CrossVal
│   ├── DataToolkit.Tests/         # + IntegrationPipeline
│   └── TestData/                  # Python 交叉验证数据
│
├── docs/                          # ✅ 文档
│   ├── api-reference.md           # UDF 签名唯一信源（数字基准）
│   ├── user-manual.md             # 每函数详细示例
│   └── context.md                 # 领域术语表
│
├── scripts/                       # ✅ 构建/验证脚本
│   ├── verify-docs.sh             # 文档一致性
│   ├── verify-manual.py           # 全 UDF 示例验证
│   ├── test-load-unload.py        # XLL 加载/卸载测试
│   ├── update_excel_arguments.py  # 同步 Excel 参数描述
│   └── patch-xll-version.ps1      # XLL 版本号注入
│
├── skills/                        # ✅ Skill 定义
│   ├── excel-dna-project/skill.md  # 编码规范、架构、MapOver、测试
│   └── excel-dna-addins/skill.md   # Excel-DNA UDF/打包/分发
│
├── .github/workflows/ci.yml       # ✅ CI/CD
├── ExcelFormulaLabs.sln           # ✅ 解决方案
├── README.md                       # ✅ 用户向功能指南
├── CLAUDE.md                       # ✅ 项目宪法（本文件）
└── .gitignore                      # ✅ 排除规则
```

```
❌ 不入库: bin/  obj/  *.xll  *.deps.json  *.runtimeconfig.json
           .claude/reviews/  TestResults/  __pycache__/
```

## 红线规则

### 1. 接口与兼容性

| ✅ DO | ❌ DON'T |
| :--- | :--- |
| 保持 Public 签名、UDF 参数/返回值不变 | 修改公开接口或破坏双 TFM 兼容 |
| `#if NET48` 仅限内部实现，方法签名双目标一致 | 在签名/参数上使用条件编译 |
| 新增 NuGet 前确认双 TFM 可用 | 引入单框架依赖 |

### 2. 防错三原则（违反 = bug）

| 原则 | 核心 | 详见 |
| :--- | :--- | :--- |
| **静默传播阻断** | 显式守卫 `NaN`/`Inf`/`null`/`default!`，WrapError 不兜底 | [skill.md §1](skills/excel-dna-project/skill.md#1-静默传播阻断) |
| **防御完整性** | 安全机制覆盖模块所有方法（ValidatePath / Regex Timeout / SQL 参数化） | [skill.md §2](skills/excel-dna-project/skill.md#2-防御完整性) |
| **异常过滤器** | `catch (Exception ex) when` 统一排除 `OutOfMemoryException`/`StackOverflowException`/`AccessViolationException` | [skill.md §3](skills/excel-dna-project/skill.md#3-异常过滤器统一) |

> **提交前自检**：`grep -rn "catch\s*{" src/ --include="*.cs"` 必须返回空。

### 3. IntelliSense 框架隔离（Excel-DNA Issue #343）

- ✅ net48：启用 `ExcelDna.IntelliSense`（AppDomain 模型稳定）
- ❌ net8.0：**禁止添加 IntelliSense 代码**——`ExcelSynchronizationContext.Post()` 在 .NET 8 下内部空引用，`RegisterUnhandledExceptionHandler` 无法拦截异步回调。违反 → Excel JIT 调试对话框 + 公式单元格交互崩溃。
- 代码隔离方式：`#if NET48` 条件编译，`.dna.tpl` net8 模板不引用 `ExcelDna.IntelliSense.dll`

### 4. 表头行契约（`object[,]` 表格方法）

- ✅ 所有接受 `object[,]` 的 Core 方法**必须**含 `bool hasHeaders = true`（例外：无表头语义的方法豁免 — `CrossJoin`、`SelectColumns`、`SelectRows`）
- ✅ `hasHeaders=true`（默认）：跳过第一行（表头），数据从 `r=1` 开始；需要表头文本时 `r=0` 读取（不参与计算）

### 5. 哨兵契约（InputNormalizer L1-L5）

> 完整 L1-L5 层级表见 [skill.md §哨兵契约](skills/excel-dna-project/skill.md#哨兵契约-l1-l5)。

核心原则：不可转换值返回类型零值哨兵（`double`→`NaN`、`long`→0、`int`→0、`bool`→`false`、`DateTime`→`MinValue`、`string`→`""`），不抛异常。Excel 信号（`null`/`DBNull`/`ExcelEmpty`/`ExcelError`/`ExcelMissing`）返回哨兵。未知类型 `Convert.ChangeType` 失败：`double`→`NaN`，其余**必须 `throw`**（禁止 `return default(T)` 静默替代）。

### 6. 验证脚本行为一致性（`verify-manual.py` ↔ 源码）

> 完整对照表见 [skill.md §验证一致性](skills/excel-dna-project/skill.md#验证脚本行为一致性)。

验证脚本必须模拟与 C# 源码**完全一致的行为**——相同模型参数、相同算法变体、相同转换常量。脚本用不同参数通过 = 假阴性。

> **自检**：新增/修改 UDF 后，确认 `verify-manual.py` 中的 Python 调用与 C# 源码参数、模型结构、转换常量逐项一致。

## 开发流程

### 修改前（强制）

1. **Read** 对应 Skill 文件（`skills/excel-dna-project/skill.md` 或 `skills/excel-dna-addins/skill.md`）
2. **CodeGraph** 检查调用者与影响范围（优先级高于 Skill）
3. 确认不违反红线规则（接口兼容、表头契约、哨兵契约、异常过滤器）

### 修改后

- 验证与调用方一致（签名/返回值/异常传播链路）
- 运行 `dotnet build && dotnet test` 确认无回归
- 缺陷处理：追溯根因 → 写入 memory 或记录改进计划（禁止仅修表面）

### 构建与测试

| 场景 | 命令 |
| :--- | :--- |
| 日常构建 | `dotnet restore && dotnet build && dotnet test` |
| 分发构建 | `dotnet build -c Release`（生成 `.xll`） |
| 全量测试 | ① `bash scripts/verify-docs.sh` ② `dotnet test` ③ `dotnet test --filter "CrossVal"` ④ `python scripts/verify-manual.py` ⑤ `dotnet build -c Debug && dotnet build -c Release` |

### Git

| ✅ DO | ❌ DON'T |
| :--- | :--- |
| 仅推送[目录树](#仓库目录树)中出现的文件路径 | 推送目录树之外的文件（构建产物 `bin/` `obj/` `*.xll` `*.deps.json`、会话产物 `.claude/`、临时文件等） |
| Commit 前确认 `dotnet test` 全绿 | 未经用户明确同意执行 `git push` |
| Commit message 描述变更内容与原因 | 空 message 或无意义提交 |

> **目录树变更管控**：对[目录树](#仓库目录树)的任何修改（新增/删除/重命名路径、调整 ✅/❌ 标记）**必须**先获得用户明确批准。目录树是推送范围的唯一信源——目录树中没有的路径不得推送。

## 参考

| 文档 | 角色 | 内容 |
| :--- | :--- | :--- |
| [context.md](docs/context.md) | 术语表 | 所有术语唯一定义 |
| [api-reference.md](docs/api-reference.md) | 数字唯一信源 | UDF 签名、参数、错误行为 |
| [user-manual.md](docs/user-manual.md) | 学习教程 | 每函数详细示例 + 结果解读指南 |
| [README.md](README.md) | 用户入口 | 安装、模块速览、使用模式、安全 |
| [skill: excel-dna-project](skills/excel-dna-project/skill.md) | 编码规范 | MapOver 选型、预防规则、测试模式 |
| [skill: excel-dna-addins](skills/excel-dna-addins/skill.md) | 打包分发 | UDF 声明、黄金法则、.xll 打包 |
