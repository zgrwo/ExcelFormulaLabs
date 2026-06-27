---
name: excel-dna-addins
description: Excel-DNA 加载项开发全流程 — 创建、打包、测试、分发 .xll 加载项。Use when the user needs to create, review, extend, test, or troubleshoot Excel-DNA add-ins (.xll), design UDFs with [ExcelFunction], or package/sign add-ins for distribution.
---

# Excel-DNA 加载项开发技能

> 术语定义见 [context.md](../../docs/context.md)。

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
- 目标框架：多目标 `net8.0-windows;net48`（Analytics/DataToolkit）/ `net8.0;net48`（Foundation）
- 语言：C#，SDK 风格 .csproj，`LangVersion=latest`
- 输出：每个模块产出两套 .xll（net8.0-windows + net48）

## Golden Rules（Excel-DNA 黄金法则）

1. **UDF 是主要产品。** 把 .NET 计算暴露为参与 Excel 重算、依赖、审计和建模工作流的工作表函数。
2. **优先简单、确定性、无副作用的函数。** 除非需求明确要求异步、流式、对象句柄、宏或 UI 交互。
3. **UI 操作和工作簿修改远离普通工作表函数。** 将状态更改放在 Ribbon 回调、命令、宏或
   通过 ExcelAsyncUtil.QueueAsMacro 排队的代码中。
4. **提前决定运行时和部署。** 本项目双目标：net8.0（需 .NET 8 Runtime）和 net48（Win10/11 自带，零安装）。分发时根据用户环境选择对应的 .xll。
5. **显式注册。** 优先使用 [ExcelFunction] 特性和 ExcelAddInExplicitExports=true。
6. **尊重 Excel 线程模型。** 切勿从任意后台线程调用 Excel COM 对象模型。
7. **使用 Excel-DNA 构建任务和打包。** 签名打包的 .xll 文件用于分发。
8. **IntelliSense 仅限 net48。** .NET 8 下 `ExcelSynchronizationContext.Post()` 内部 NRE（Excel-DNA Issue #343），`RegisterUnhandledExceptionHandler` 无法拦截异步回调。禁止在 net8.0 构建中启用 IntelliSense。代码隔离：`#if NET48` 条件编译 + `.dna.tpl` net8 模板不引用 `ExcelDna.IntelliSense.dll`。

## 本项目架构

三层双文件结构（Foundation → Analytics → DataToolkit），调用链和模块职责详见 [项目 skill](../excel-dna-project/skill.md#架构)。


## 新 UDF 实现清单

1. 在对应的 Core 类中实现 `internal static` 方法（纯逻辑，无 Excel 依赖）
2. 在对应的 Udf 类中添加 `[ExcelFunction]` 包装方法，选择正确的 MapOver 变体
3. 编写单元测试：标量、null、error 透传、数组、多参数尺寸不匹配
4. 如果涉及统计函数，与 Python numpy/scipy 交叉验证

```csharp
// Core (internal static, no Excel dependency)
internal static string ReverseString(string t) { t ??= ""; var a = t.ToCharArray(); Array.Reverse(a); return new string(a); }

// UDF ([ExcelFunction], public static object)
[ExcelFunction(Name = "STR.REVERSE", Description = "Reverse a string character-wise.")]
public static object UDF_STR_REV(
    [ExcelArgument(Name = "text", Description = "The text string to process.")]
    object t
) => OutputWrapper.WrapError(() => ElementWiseMapper.MapOver<string, string>(t, StringCore.ReverseString));
```

## 本项目常用命令

> 详见 [CLAUDE.md § 构建与测试](../../CLAUDE.md#构建与测试)。

构建输出（每个模块双 TFM）：
- `src/Analytics/bin/Release/net8.0-windows/publish/Analytics-AddIn-packed.xll`  (.NET 8)
- `src/Analytics/bin/Release/net48/publish/Analytics-AddIn-packed.xll`       (.NET Framework 4.8)
- `src/DataToolkit/bin/Release/net8.0-windows/publish/DataToolkit-AddIn-packed.xll`  (.NET 8)
- `src/DataToolkit/bin/Release/net48/publish/DataToolkit-AddIn-packed.xll`           (.NET Framework 4.8)


## Excel 加载 .xll

1. **选择版本**：net48 版（Win10/11 直接可用）或 net8.0 版（需安装 [.NET 8 Runtime](https://dotnet.microsoft.com/download/dotnet/8.0)）
2. Excel → 文件 → 选项 → 加载项 → Excel 加载项 → 转到 → 浏览
3. 选择对应的 `-packed.xll` 文件
4. 函数即可在工作表中使用，如 `=STATS.MEAN(A1:A100)` 或 `=STR.REVERSE("hello")`

## 相关文档

- [CLAUDE.md](../../CLAUDE.md) — 项目宪法（规则和技能路由）
- [skill.md](../excel-dna-project/skill.md) — 编码规范和详细参考
- [README.md](../../README.md) — 用户向项目说明
- [docs/api-reference.md](../../docs/api-reference.md) — UDF 签名唯一信源
- [docs/user-manual.md](../../docs/user-manual.md) — 每函数详细示例（Python 交叉验证）
- [context.md](../../docs/context.md) — 领域术语表
- 测试数据：tests/TestData/Cross_Validation_vs_Python.xlsx
- Excel-DNA 官方文档：https://github.com/Excel-DNA/ExcelDna

