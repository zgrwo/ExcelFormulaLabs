# CLAUDE.md

项目宪法。编码细节按需加载对应 skill。

## 语言

所有 Markdown 文档默认优先中文。

## 技能

- `/excel-dna-project` — 编码规范、架构、MapOver 变体、测试模式
- `/excel-dna-addins` — Excel-DNA 加载项开发（UDF、.xll 打包、分发）

## 领域术语

见 [CONTEXT.md](CONTEXT.md)。

## 规则

- **接口约束**：禁止修改 Core 类 Public 签名和 UDF 参数/返回值。`#if NET48` 仅限内部实现，方法签名双目标一致。新增 NuGet 须确认双 TFM 可用。
- **缺陷处理**：发现缺陷 → 复核并追踪根因 → 写入 memory 或记录改进计划。
- **Git**：push 前须用户明确同意。禁止推送 src/tests 外的文件/文件夹。
- **文档**：信息只在一处定义，其余链接引用。数字仅存 api-reference.md。
- **上下文**：膨胀时主动建议开新会话。新会话自动继承本文件。
- **验证**：修改函数前用 CodeGraph 检查调用者（blast radius），修改后用 CodeGraph 验证一致性。如 skill 或 CodeGraph 冲突，优先 CodeGraph。
- **全量测试** = ①全量单元测试 ②Excel 数据源交叉验证 ③Debug+Release DLL/XLL 双目标打包。
  三轮命令：
  ```bash
  # ① 全量单元测试（含内联 Python 对照，除豁免外期望值均源自 scipy/numpy）
  dotnet test

  # ② Excel 数据源交叉验证 — 从 Cross_Validation_vs_Python.xlsx 加载真实数据，
  #    与 Python scipy/numpy 输出逐项对照（方法名含 CrossVal_ + PythonCrossValidationTests）
  dotnet test --filter "CrossVal|PythonCrossValidation"

  # ③ Debug + Release 双配置 DLL/XLL 打包构建
  dotnet build -c Debug && dotnet build -c Release
  ```
  数值精度 1e-10。
  豁免：FS.*（POSIX 差异）、RANGE.*（无标准输出格式），标记 `// No Python ref:`。
  数据源：`tests/TestData/Cross_Validation_vs_Python.xlsx`。

## 项目结构

```
src/
├── Foundation/         共享基础设施（InputNormalizer, ElementWiseMapper, OutputWrapper, FilterUtils, ArrayOperations, ComparisonUtils, DictOperations, ExcelEmpty, ExcelError）
├── Analytics/          分析引擎（Stats, Linalg, Regression, PhyChem）+ Excel-DNA 加载项
└── DataToolkit/        数据工具箱（String, DateTime, Regex, JSON/XML, Pivot, SQL, FileSystem, Array, DictSet）+ 加载项

tests/
├── Foundation.Tests/   ArrayOperations, ComparisonUtils, DictOperations, ElementWiseMapper, FilterUtils, InputNormalizer, OutputWrapper
├── Analytics.Tests/    Stats, Linalg, Regression, PhyChem 的 Core + UDF 双重测试 + Python 交叉验证
└── DataToolkit.Tests/  DataToolkit 的 Core + UDF 双重测试 + PythonCrossValidationTests

docs/
├── api-reference.md    214 UDF 完整签名与说明（数字的唯一来源）
└── user-guide.md       安装与使用指南
CONTEXT.md              领域术语表（项目根目录）
```

### 架构分层

```
UDF (public static, [ExcelFunction])  →  Excel-DNA 入口
  ↓ 调用
Core (internal static, 纯逻辑)         →  零 Excel 依赖，100% 单元测试
  ↓ 依赖
Foundation (共享工具)                   →  InputNormalizer, ElementWiseMapper, OutputWrapper
```

## 构建

```bash
dotnet restore && dotnet build && dotnet test
```
