# Excel-VBA-Libraries .NET Port

Excel-DNA C# implementation with clean layered architecture.

## Structure

```
ExcelVbaLibraries.sln
├── src/
│   ├── Foundation/         ← Zero-dependency class library (8 source files)
│   ├── Analytics/          ← Statistics, regression, linear algebra
│   └── DataToolkit/        ← JSON, XML, regex, text, dates, files
├── tests/
│   └── Foundation.Tests/   ← xUnit unit tests
└── skills/                 ← AI assistant skills/reference docs
```

## Architecture

```
UDF Layer ([ExcelFunction], ~5 lines, public static object)
    ↓
ElementWiseMapper + InputNormalizer   (Pre-processing)
    ↓
Core functions                        (internal static, type-safe)
    ↓
OutputWrapper                         (Post-processing)
    ↓
Return to Excel
```

## Foundation.dll — API Reference

详见 [skills/excel-dna-addins/skill.md](skills/excel-dna-addins/skill.md) 架构部分，包含全部 Foundation 类说明和调用链。

### Target UDF Pattern

```csharp
// VBA: 21 lines (18 boilerplate) → C#: 5 lines (0 boilerplate)
[ExcelFunction(Name = "STR.REVERSESTRING", Description = "Reverse a string.",
               Category = "StringUtils")]
public static object UDF_STR_REVERSESTRING(object text)
    => OutputWrapper.WrapError(() =>
        ElementWiseMapper.MapOver(text, StringUtilsCore.ReverseString));
```

### Multi-argument UDF

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

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Excel-DNA](https://github.com/Excel-DNA/ExcelDna) (for .xll packaging — Phase 2+)

## Build & Test

```bash
dotnet restore
dotnet build
dotnet test
```

## Status

| Layer | Core Tests | UDF Tests | Status |
|-------|-----------|-----------|--------|
| Foundation | 198 | N/A (public) | ✅ Complete |
| Analytics | 104 | 158 | ✅ Complete |
| DataToolkit | 157 | 735 | ✅ Complete |
| **Total** | **459** | **893** | **1,352 tests, 0 failures** |

See [docs/api-reference.md](docs/api-reference.md) for full function list.
