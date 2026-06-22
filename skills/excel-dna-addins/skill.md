# Excel-DNA 加载项开发技能

在 Windows 上使用 .NET 创建、审查、扩展、测试、排查和分发 Excel-DNA (.xll) 加载项时使用此技能。

## 适用场景

- 选择 Excel-DNA 作为加载项技术方案
- 用 C# 创建或维护 .xll Excel 加载项
- 设计 Excel 用户定义函数 (UDF) 作为核心加载项接口
- 实现高级函数：线程安全、异步、流式、对象句柄、数组/范围函数
- 添加 Excel 扩展：Ribbon UI、COM 自动化、自定义任务窗格、函数 IntelliSense
- 测试、打包、签名、安装、运行时选择
- 处理 Excel C API、XlCall、ExcelReference 和加载项注册规则

## 不适用场景

- Office.js/JavaScript 加载项（除非用于比较或迁移建议）
- Mac/Web/iPad 加载项（.xll 无法在这些平台加载）
- 与 Excel 加载项无关的通用 .NET 开发

## 本项目技术栈

- Excel-DNA 版本：1.8.0（通过 NuGet ExcelDna.AddIn）
- 目标框架：net8.0-windows（Analytics/DataToolkit）/ net8.0（Foundation）
- 语言：C#，SDK 风格 .csproj
- 输出：.xll 文件（ExcelDnaPack 打包为 -packed.xll）

## Golden Rules（Excel-DNA 黄金法则）

1. **UDF 是主要产品。** 把 .NET 计算暴露为参与 Excel 重算、依赖、审计和建模工作流的工
   作表函数。
2. **优先简单、确定性、无副作用的函数。** 除非需求明确要求异步、流式、对象句柄、宏或 UI 交互。
3. **UI 操作和工作簿修改远离普通工作表函数。** 将状态更改放在 Ribbon 回调、命令、宏或
   通过 ExcelAsyncUtil.QueueAsMacro 排队的代码中。
4. **提前决定运行时和部署。** 本项目使用 .NET 8。用户机器需安装 .NET 8 Runtime。
5. **显式注册。** 优先使用 [ExcelFunction] 特性和 ExcelAddInExplicitExports=true。
6. **尊重 Excel 线程模型。** 切勿从任意后台线程调用 Excel COM 对象模型。
7. **使用 Excel-DNA 构建任务和打包。** 签名打包的 .xll 文件用于分发。

## 本项目架构

```
src/
├── Foundation/           ← 零依赖类库（8 个源文件）
│   ├── ExcelError.cs     ← 不可变结构体，7 种标准错误码
│   ├── ExcelEmpty.cs     ← 空白单元格哨兵（单例）
│   ├── InputNormalizer.cs← COM Range 检测、类型探测、安全转换、数组归一化
│   ├── ElementWiseMapper.cs← 核心抽象层，消除 ~3000 行重复模板代码
│   ├── OutputWrapper.cs  ← WrapError、ReshapeOutput — 错误安全执行
│   ├── ArrayOperations.cs← 混合快速排序、切片、查找、展平、argsort、列检测
│   ├── DictOperations.cs ← 字典工厂、FromKeys、ToArray、Merge
│   ├── ComparisonUtils.cs← ValuesEqual、Compare、SafeKey — 类型感知比较
│   └── FilterUtils.cs    ← FilterPasses — 12 种运算符（regex、contains、比较）
├── Analytics/            ← 统计、回归、线性代数、物理化学
│   ├── StatsCore.cs / StatsUdf.cs
│   ├── LinalgCore.cs / LinalgUdf.cs
│   ├── RegressionCore.cs / RegressionUdf.cs
│   └── PhyChemCore.cs / PhyChemUdf.cs
└── DataToolkit/          ← 字符串、日期、正则、数组、字典、JSON/XML、透视、文件、SQL、范围
    ├── StringCore.cs / StringUdf.cs
    ├── DateTimeCore.cs / DateTimeUdf.cs
    ├── RegexCore.cs / RegexUdf.cs
    ├── ArrayCore.cs / ArrayUdf.cs
    ├── DictSetCore.cs / DictSetUdf.cs
    ├── JsonXmlCore.cs / JsonXmlUdf.cs
    ├── PivotCore.cs / PivotUdf.cs
    ├── FileSystemCore.cs / FileSystemUdf.cs
    ├── SqlCore.cs / SqlUdf.cs
    └── RangeExportCore.cs / RangeExportUdf.cs

调用链：UDF → InputNormalizer → MapOver/MapOverFlat/MapOverMulti/V() → Core → OutputWrapper.WrapError → Excel
```


## 新 UDF 实现清单

1. 在对应的 Core 类中实现  方法（纯逻辑，无 Excel 依赖）
2. 在对应的 Udf 类中添加  包装方法，选择正确的 MapOver 变体
3. 编写单元测试：标量、null、error 透传、数组、多参数尺寸不匹配
4. 如果涉及统计函数，与 Python numpy/scipy 交叉验证

## 本项目常用命令

```bash
# 还原依赖
dotnet restore

# 构建全部项目
dotnet build

# 运行全部测试
dotnet test

# 运行指定测试类
dotnet test --filter ClassName

# 静默快速测试（跳过构建）
dotnet test --no-build -v q

# 构建 Release 版本（含 ExcelDnaPack 打包）
dotnet build -c Release

# 清理构建产物
dotnet clean
```

构建输出：
- `src/Analytics/bin/Release/net8.0-windows/Analytics-packed.xll`
- `src/DataToolkit/bin/Release/net8.0-windows/DataToolkit-packed.xll`


## Excel 加载 .xll

1. 确保安装 .NET 8 Runtime
2. Excel → 文件 → 选项 → 加载项 → Excel 加载项 → 转到 → 浏览
3. 选择 `src\Analytics\bin\Release\net8.0-windows\Analytics-packed.xll`
4. 选择 `src\DataToolkit\bin\Release\net8.0-windows\DataToolkit-packed.xll`
5. 函数即可在工作表中使用，如 `=STATS.MEAN(A1:A100)` 或 `=STR.REVERSE("hello")`

## 相关文档

- [CLAUDE.md](../../CLAUDE.md) — 项目宪法（规则和架构概览）
- [skill.md](../excel-dna-project/skill.md) — 编码规范和详细参考
- [README.md](../../README.md) — 用户向项目说明
- 测试数据：tests/TestData/Cross_Validation_vs_Python.xlsx
- Excel-DNA 官方文档：https://github.com/Excel-DNA/ExcelDna

## 默认输出风格

生成 Excel-DNA 项目代码时：
- 使用 SDK 风格 .csproj
- PackageReference Include="ExcelDna.AddIn" Version="1.8.0"
- 目标 net8.0-windows
- 使用 ExcelAddInExplicitExports=true
- 包含 [ExcelFunction] 和 [ExcelArgument] 元数据
- 包含至少一个可冒烟测试的 UDF