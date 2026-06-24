---
name: excel-dna-project
description: 本项目编码规范与架构参考 — Foundation/Analytics/DataToolkit 模块职责、MapOver 变体行为差异、UDF 调用链、测试模式。Use when modifying code in this project, understanding module responsibilities, or writing new UDFs.
disable-model-invocation: true
---

# SKILL.md

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

## 全量测试

三轮命令，缺一不可：

```bash
# ① 全量单元测试（含内联 Python 对照，除豁免外期望值均源自 scipy/numpy）
dotnet test

# ② Excel 数据源交叉验证 — 从 Cross_Validation_vs_Python.xlsx 加载真实数据，
#    与 Python scipy/numpy 输出逐项对照（方法名含 CrossVal_ + PythonCrossValidationTests）
dotnet test --filter "CrossVal|PythonCrossValidation"

# ③ Debug + Release 双配置 DLL/XLL 打包构建
dotnet build -c Debug && dotnet build -c Release
```

数值精度 1e-10。豁免：FS.*（POSIX 差异）、RANGE.*（无标准输出格式），标记 `// No Python ref:`。
数据源：`tests/TestData/Cross_Validation_vs_Python.xlsx`。

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

## 预防规则（来自 9+ 轮审查的反复模式）

1. **静默传播阻断**：NaN/Inf/null/default! 不得静默返回。WrapError 只捕获异常。
2. **防御完整性**：安全机制（ValidatePath/Regex Timeout/SQL参数化）须覆盖模块所有方法。
3. **异常过滤器**：裸 `catch{}` = bug，须 `when (ex is not OOM and not StackOverflow)`。

## 已知限制 & InternalsVisibleTo
Analytics → Analytics.Tests, DataToolkit.Tests / DataToolkit → DataToolkit.Tests / Foundation 为 public
- TryExtractComRangeValue：需 COM Excel Range 对象
- Solve 奇异矩阵：MathNet 5.0 可能不抛异常
- GasToSTP 无效单位：走 default 分支
- FileSystem UDF：依赖真实文件系统，部分测试需特定环境
- MathNet 5.0 QR 不支持宽矩阵 (m < n)，采用零填充方阵后提取子矩阵
- FilterPasses 比较运算符使用类型感知 Compare（与 VBA 语义一致）

## 历史修复
- StdevP/CovarianceP/Covariance：MathNet 5.0 破性变更（样本→总体协方差）
- IsoYear/IsoWeekNum：net48 无 System.Globalization.ISOWeek，手工 polyfill
- Coalesce：null 与空字符串统一处理
- Lu P 矩阵：排列循环 > 2 时行交换 bug，改为逐元素赋值 P[i,perm[i]]=1.0
- Qr 宽矩阵：零填充法支持 m < n 的 QR 分解
- Stats.Mode：O(n²) GroupBy→O(n) Dictionary，委托 StatsCore.Mode
- PhyChem 便捷 UDF：C_TO_F 等委托 PhyChemCore 消除公式重复
- PhyChem LB 常数：453.592→453.59237（国际标准 lb=0.45359237 kg）
- StringCore.RandomString：static Random→ThreadLocal<Random>/Random.Shared（多线程安全）
- SqlCore.CreateTable：列类型推断单行→扫描前10行；列名去重追加 _2/_3 后缀
- ArrayOperations.IsNumericValue：新增数值字符串识别，与 ComparisonUtils.IsNumeric 统一
- DateDiff 年差：DayOfYear→Month/Day 比较，修复闰年跨年偏差
- ValidatePath：追加分隔符再 StartsWith，修复 "." 路径误判越界
- RegressionCore 除零：FitOLS/FitRidge tss≈0 guard + df≤0 guard + AnovaOneWay k<2 guard + FactorImportance n<2 guard
- FileSystemCore 沙箱：FileExists/GetFileSize/FolderExists 补 ValidatePath；FileSystem测试竞态用 xUnit Collection 序列化
- 异常过滤器统一（P0/P1/P2）：全项目 18 处裸 catch{} → `when` 过滤器；IdealGasLaw 零分母 guard
- DataToolkit XLL 打包：增量构建残留 .dna 污染 → CleanupDnaAfterBuild 目标
