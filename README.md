# Excel-VBA-Libraries .NET 移植

基于 Excel-DNA 的 C# 实现，采用清晰的分层架构。

## 项目结构

```
ExcelVbaLibraries.sln
├── src/
│   ├── Foundation/         ← 零依赖类库（8 个源文件）
│   ├── Analytics/          ← 统计、回归、线性代数
│   └── DataToolkit/        ← JSON、XML、正则、文本、日期、文件
├── tests/
│   ├── Foundation.Tests/   ← xUnit 单元测试
│   ├── Analytics.Tests/
│   └── DataToolkit.Tests/
└── skills/                 ← AI 辅助技能/参考文档
```

## 架构

```
UDF 层 ([ExcelFunction], ~5 行, public static object)
    ↓
ElementWiseMapper + InputNormalizer   （预处理）
    ↓
Core 函数                             （internal static，类型安全）
    ↓
OutputWrapper                         （后处理）
    ↓
返回 Excel
```

## Foundation.dll — API 参考

详见 [skills/excel-dna-addins/skill.md](skills/excel-dna-addins/skill.md) 架构部分，包含全部 Foundation 类说明和调用链。

### UDF 包装模式

```csharp
// VBA: 21 行（18 行模板代码） → C#: 5 行（零模板代码）
[ExcelFunction(Name = "STR.REVERSESTRING", Description = "Reverse a string.",
               Category = "StringUtils")]
public static object UDF_STR_REVERSESTRING(object text)
    => OutputWrapper.WrapError(() =>
        ElementWiseMapper.MapOver(text, StringUtilsCore.ReverseString));
```

### 多参数 UDF

```csharp
[ExcelFunction(Name = "STR.EXTRACTBETWEEN")]
public static object UDF_STR_EXTRACTBETWEEN(
    object text, object left, object right, object nth, object include)
    => OutputWrapper.WrapError(() =>
        ElementWiseMapper.MapOver(text, v =>
            StringUtilsCore.ExtractBetween(
                InputNormalizer.ToString(v),
                InputNormalizer.ToString(left),
                InputNormalizer.ToString(right),
                (int)InputNormalizer.ToLong(nth),
                InputNormalizer.ToBool(include))));
```

## 环境要求

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Excel-DNA](https://github.com/Excel-DNA/ExcelDna)（用于 .xll 打包）

## 构建与测试

```bash
dotnet restore
dotnet build
dotnet test
```

## 当前状态

| 层 | Core 测试 | UDF 测试 | 状态 |
|-------|-----------|-----------|--------|
| Foundation | 198 | N/A (public) | ✅ 完成 |
| Analytics | 104 | 158 | ✅ 完成 |
| DataToolkit | 157 | 735 | ✅ 完成 |
| **总计** | **459** | **893** | **1,352 测试，0 失败** |

完整函数列表见 [docs/api-reference.md](docs/api-reference.md)。
