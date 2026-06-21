# Excel-VBA-Libraries .NET Port

Excel-DNA C# implementation porting VBA libraries to .NET 8.

## Build
```bash
dotnet build && dotnet test
```
Target: net8.0-windows / net8.0. Tests: xUnit + FluentAssertions 6.12.0.

## Architecture
```
UDF ([ExcelFunction]) -> InputNormalizer -> ElementWiseMapper -> Core -> OutputWrapper -> Excel
```

### ElementWiseMapper Rules
- **MapOver<TIn,TOut>**: preserves shape (scalar->scalar, array->same shape)
- **MapOverFlat<TIn,TOut>**: ALWAYS returns object[] (even for scalar input)
- **MapOverMulti**: broadcasting; null first arg -> ExcelEmpty.Value (mapper skipped); mismatched sizes -> ExcelError.Value

### InputNormalizer Rules
- NormalizeTo1D(null) -> empty array (no exception)
- NormalizeTo2D(null) -> null (caller uses `!` or handles)
- ToDoubles() skips non-numeric
- ToDateTime() handles OLE date serials (epoch 1899-12-30)

## Test Casting Rules (UDF returns object)
- Numeric: `((double)Udf.Method()).Should().BeApproximately(val, tol);`
- Long: `((long)Udf.Method()).Should().Be(3);`
- Bool: `((bool)Udf.Method()).Should().BeTrue();`
- Array: `var r = (object[])Udf.Method();` or `(object[,])`
- MapOverFlat: `var r = (object[])Udf.Method(scalar); r[0].Should()...`

## StatsCore Quantile
DefaultQuantileDefinition = R7 (matches Python numpy 'linear'). Configurable via optional parameter.

## Bugs Fixed (6)
StdevP, CovarianceP, Covariance, IsoYear, IsoWeekNum, Coalesce

## InternalsVisibleTo
Analytics -> Analytics.Tests, DataToolkit.Tests
DataToolkit -> DataToolkit.Tests
Foundation methods are public.

## Excel Loading
Build produces .xll files. Load: Excel -> Options -> Add-ins -> Browse -> select .xll
