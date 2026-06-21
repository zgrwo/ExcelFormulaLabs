# Excel-VBA-Libraries .NET Port

Excel-DNA C# implementation with clean layered architecture.

## Structure

```
dotnet/
├── ExcelVbaLibraries.sln
├── src/
│   ├── Foundation/         ← Zero-dependency class library (8 source files)
│   ├── Analytics/          ← Statistics, regression, linear algebra (Phase 2)
│   └── DataToolkit/        ← JSON, XML, regex, text, dates, files (Phase 2)
└── tests/
    └── Foundation.Tests/   ← xUnit unit tests
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

**Zero NuGet dependencies — pure .NET 8.**

| Class | Role | Ported From |
|-------|------|-------------|
| `ExcelError` | Immutable struct with 7 standard error codes | VBA CVErr |
| `ExcelEmpty` | Empty cell sentinel (singleton) | VBA Empty |
| `InputNormalizer` | COM Range detection, type probing, safe coercion, array normalisation | VariantKit.cls |
| `ElementWiseMapper` | Core abstraction — eliminates ~3000 lines of duplicated boilerplate | N/A (new) |
| `OutputWrapper` | WrapError, ReshapeOutput — error-safe execution | VBA On Error pattern |
| `ArrayOperations` | Hybrid quicksort, slice, index-of, flatten, argsort, column detection | ArrayOps.cls |
| `DictOperations` | Dictionary factory, FromKeys, ToArray, Merge | DictProxy.cls |
| `ComparisonUtils` | ValuesEqual, Compare, SafeKey — type-aware comparison | VariantKit.cls |
| `FilterUtils` | FilterPasses — 12 operators (regex, contains, comparisons) | VariantKit.cls |

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
cd dotnet
dotnet restore
dotnet build
dotnet test
```

## Phase Status

| Phase | Status |
|-------|--------|
| Solution scaffold + all 8 source files | ✅ Complete |
| Unit tests | 📝 Pending |
| Analytics.dll + DataToolkit.dll | 📅 Future |
