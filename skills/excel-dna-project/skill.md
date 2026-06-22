# SKILL.md

## Build & Test

```bash
dotnet build && dotnet test
dotnet test --filter ClassName
dotnet test --no-build -v q
```

xUnit [Fact] + FluentAssertions 6.12.0。

## 架构

UDF -> InputNormalizer -> ElementWiseMapper/MapOver -> Core -> OutputWrapper -> Excel

### MapOver 系列
- MapOver<TIn,TOut>: 保持形状，null/error/empty 透传
- MapOverFlat<TIn,TOut>: 始终返回 object[]，即使标量输入
- MapOverMulti: 广播，null 首参→ExcelEmpty，尺寸不匹配→ExcelError

### V()/M() vs MapOverMulti
CVP/CV/PEAR/SPR/T1/T2 用 V() 直接调 Core：尺寸不匹配→NaN，V(null)→空数组→NaN，MathNet 异常→WrapError→ExcelError.Value

### UDF 包装示例
```csharp
[ExcelFunction(Name = "STR.REVERSE")]
public static object UDF_STR_REV(object t)
    => OutputWrapper.WrapError(() =>
        ElementWiseMapper.MapOver<string, string>(t, StringCore.ReverseString));
```

## InputNormalizer
- NormalizeTo1D(null) → new object[0]
- NormalizeTo2D(null) → null
- ToDoubles() 静默过滤非数值
- ToDateTime() OLE 纪元 1899-12-30

## UDF 测试
UDF 返回 object，断言前需转型。每方法覆盖：null、空、error 透传、数组、多参数尺寸不匹配。
```csharp
[Fact]
public void UDF_STAT_MEAN_Basic()
{
    var result = (double)StatsUdf.UDF_STAT_MEAN(new double[] { 1.0, 2.0, 3.0 });
    result.Should().BeApproximately(2.0, 1e-9);
}
```

## 已知限制 & InternalsVisibleTo
Analytics → Analytics.Tests, DataToolkit.Tests / DataToolkit → DataToolkit.Tests / Foundation 为 public
- TryExtractComRangeValue：需 COM Excel Range 对象
- Solve 奇异矩阵：MathNet 5.0 可能不抛异常
- GasToSTP 无效单位：走 default 分支
- FileSystem UDF：仅测试 null/empty

## Bug 修复
StdevP, CovarianceP, Covariance, IsoYear, IsoWeekNum, Coalesce
