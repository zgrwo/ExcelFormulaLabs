# SKILL.md

## Build & Test

```bash
dotnet build && dotnet test
dotnet test --filter ClassName
dotnet test --no-build -v q
```

xUnit [Fact] + FluentAssertions 6.12.0。CodeGraph 已索引。

## 架构

UDF -> InputNormalizer -> ElementWiseMapper/MapOver -> Core -> OutputWrapper -> Excel

### MapOver 系列
- MapOver<TIn,TOut>: 保持形状，null/error/empty 透传
- MapOverFlat<TIn,TOut>: 始终返回 object[]，即使标量输入
- MapOverMulti: 广播，null 首参→ExcelEmpty，尺寸不匹配→ExcelError

### V()/M() vs MapOverMulti（关键差异）
StatsUdf 的 CVP/CV/PEAR/SPR/T1/T2 用 V() 直接调 Core，不经过 MapOverMulti：
- 尺寸不匹配 → NaN（不是 ExcelError.Value）
- V(null) → 空数组 → NaN（不是 ExcelEmpty.Value）
- MathNet 异常 → WrapError → ExcelError.Value

## InputNormalizer
- NormalizeTo1D(null) → new object[0]
- NormalizeTo2D(null) → null
- ToDoubles() 静默过滤非数值
- ToDateTime() OLE 纪元 1899-12-30

## UDF 测试规范
UDF 返回 object，断言前需转型。每方法覆盖：null、空、error 透传、数组、多参数尺寸不匹配。

## Python 交叉验证
tests/TestData/Cross_Validation_vs_Python.xlsx — 通过 ClosedXML 读取，与 numpy/scipy 比较。

## InternalsVisibleTo
Analytics → Analytics.Tests, DataToolkit.Tests
DataToolkit → DataToolkit.Tests
Foundation 为 public

## 已知限制
- TryExtractComRangeValue：需 COM Excel Range 对象
- Solve 奇异矩阵：MathNet 5.0 可能不抛异常
- GasToSTP 无效单位：走 default 分支
- FileSystem UDF：仅测试 null/empty

## Bug 修复（6 个）
StdevP, CovarianceP, Covariance, IsoYear, IsoWeekNum, Coalesce