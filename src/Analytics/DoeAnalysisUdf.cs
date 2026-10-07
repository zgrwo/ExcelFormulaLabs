using ExcelDna.Integration;
using ExcelFormulaLabs.Foundation;

namespace ExcelFormulaLabs.Analytics
{
    public static partial class DoeAnalysisUdf
    {
        private static double[,] M(object d) => AnalyticsHelpers.PrepM(d);
        private static double[] V(object d) => AnalyticsHelpers.PrepV(d);

        /// <summary>饱和设计（扩展项数+截距 ≥ n）时自动降阶（quadratic → 2way →
        /// main），使默认 terms 在 2×2 等最小示例上可用；降阶到 main 仍不足时才
        /// 交由 FitOLS 显式报错。</summary>
        private static (int maxOrder, bool quadratic) EffectiveTerms(double[,] design, object terms)
        {
            var (maxOrder, quadratic) = DoeAnalysisCore.ParseTerms(terms);
            int n = design.GetLength(0), k = design.GetLength(1);
            while (maxOrder >= 2 && DoeAnalysisCore.ExpandedTermCount(k, maxOrder, quadratic) + 1 >= n)
            {
                if (quadratic) quadratic = false;
                else maxOrder = 1;
            }
            return (maxOrder, quadratic);
        }
    }
}
