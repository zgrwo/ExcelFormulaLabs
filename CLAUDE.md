# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Test Commands

```bash
dotnet build                              # Full solution build
dotnet test                               # Run all tests (1,352 tests, 33 files)
dotnet test --filter "ClassName"          # Run a single test class
dotnet test --no-build -v q               # Quick re-run without rebuild
```

Target: `net8.0-windows` (Analytics/DataToolkit, for Excel-DNA) / `net8.0` (Foundation).
Tests: xUnit `[Fact]` + FluentAssertions 6.12.0. No `[Theory]` used.
CodeGraph indexed: `codegraph explore "<symbol>"` for fast code lookup.

## Architecture

```
UDF ([ExcelFunction], public static object)     ← Excel-facing, ~214 methods
  ↓  calls one of three patterns:
  ├── MapOver<TIn,TOut>        → preserves shape, null/error/empty pass through
  ├── MapOverFlat<TIn,TOut>    → ALWAYS returns object[], even for scalar
  ├── MapOverMulti<T1,T2,TOut> → broadcasting, null first→ExcelEmpty, mismatch→ExcelError
  └── V()/M() helpers          → bypass MapOver entirely! Normalize→Core directly
       ↓
Core functions (internal static, type-safe)    ← pure logic, 100% test covered
       ↓
OutputWrapper.WrapError                         ← exception→#VALUE!
```

### Critical: V()/M() vs MapOverMulti

`StatsUdf` covariance/correlation methods (CVP, CV, PEAR, SPR, T1, T2) use `V()` helpers
that call Core **directly**, bypassing `MapOverMulti`. This means:
- Size mismatch → Core returns `NaN` (not `ExcelError.Value`)
- `V(null)` → empty array → Core returns `NaN` (not `ExcelEmpty.Value`)
- Exception from MathNet (e.g. Pearson on empty) → WrapError → `ExcelError.Value`

`MapOverMulti` is used by StringUdf (STARTSWITH, COMMONPFX, LEVENSHTEIN, etc.) and has
different null/mismatch behavior. Always check the UDF source to see which pattern it uses.

### ElementWiseMapper behavior summary

| Pattern | Scalar input | Null input | Error input | Mismatched arrays |
|---------|-------------|------------|-------------|-------------------|
| MapOver | scalar | null passthrough | error passthrough | N/A |
| MapOverFlat | object[1] | object[0] | error passthrough | N/A |
| MapOverMulti | scalar | ExcelEmpty.Value | depends | ExcelError.Value |

### InputNormalizer key behaviors

- `NormalizeTo1D(null)` → `new object[0]` (no exception)
- `NormalizeTo2D(null)` → `null` (caller must handle or `!`)
- `ToDoubles()` filters non-numeric elements silently
- `ToDateTime()` uses OLE Automation epoch (1899-12-30)

## Test Conventions

UDF methods return `object`. Cast before using FluentAssertions numeric/string matchers:
```csharp
((double)Udf.Method(args)).Should().BeApproximately(3.0, 1e-10);  // numeric
((long)Udf.Method(args)).Should().Be(3);                            // long
((bool)Udf.Method(args)).Should().BeTrue();                         // bool
((string)Udf.Method(args)).Should().Be("expected");                 // string
var r = (object[])Udf.Method(args);                                 // 1D array
var r = (object[,])Udf.Method(args);                                // 2D array
// MapOverFlat: ALWAYS object[], even for scalar
var r = (object[])Udf.Method(scalar); r[0].Should()...
```

**Edge cases every UDF test should cover**: null input, empty input, error input passthrough,
array input (element-wise mapping), and for multi-arg: mismatched sizes.

**Python cross-validation**: `tests/TestData/Cross_Validation_vs_Python.xlsx` —
StatsCore cross-val tests read NumericX1 column via ClosedXML and compare against
numpy/scipy reference values. Quantile methods use R7 definition (Python-compatible).

## InternalsVisibleTo

- `Analytics.csproj`: Analytics.Tests, DataToolkit.Tests
- `DataToolkit.csproj`: DataToolkit.Tests
- Foundation methods are `public` — no InternalsVisibleTo needed

## Known Limitations

- `TryExtractComRangeValue`: untestable without COM Excel Range object
- `Solve` on singular matrices: MathNet 5.0 may not throw (uses internal fallback)
- `GasToSTP` invalid unit strings: fall through to default case, behavior version-dependent
- FileSystem UDF methods: only null/empty tested; real filesystem ops depend on environment
