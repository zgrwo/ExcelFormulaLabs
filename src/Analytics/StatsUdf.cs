using System;
using ExcelDna.Integration;
using ExcelFormulaLabs.Foundation;

namespace ExcelFormulaLabs.Analytics
{
    /// <summary>
    /// STATS.* UDF 分发层。**全部 34 个函数声明由工具生成**，见
    /// <c>StatsUdf.g.cs</c>（真源：<c>udf-metadata/StatsUdf.json</c>，见 ADR-0011）；
    /// 本文件只保留手写的参数预处理助手。
    /// </summary>
    /// <remarks>
    /// 修改函数名/描述/参数/分类 → 改元数据后运行 <c>python tools/udfgen.py generate</c>；
    /// 修改实现 → 改 <see cref="StatsCore"/>。
    /// </remarks>
    public static partial class StatsUdf
    {
        /// <summary>x → double[]（非数值/空/错误单元格显式抛错，见 AnalyticsHelpers.PrepV）。</summary>
        private static double[] V(object d) => AnalyticsHelpers.PrepV(d);

        /// <summary>x → double[,]（同上，见 AnalyticsHelpers.PrepM）。</summary>
        private static double[,] M(object d) => AnalyticsHelpers.PrepM(d);
    }
}
