using ExcelDna.Integration;
using ExcelFormulaLabs.Foundation;

namespace ExcelFormulaLabs.Analytics
{
    public static class DoeAnalysisUdf
    {
        private static double[,] M(object d) => AnalyticsHelpers.PrepM(d);
        private static double[] V(object d) => AnalyticsHelpers.PrepV(d);

        [ExcelFunction(Name = "DOE.ANALYZE",
          Description = "DOE effect table (term, coef, effect, t, p) from a coded design matrix and response.")]
        public static object UDF_DOE_ANALYZE(
            [ExcelArgument(Name = "design", Description = "Coded factor matrix (DOE.PLAN factor columns)")]
            object design,
            [ExcelArgument(Name = "response", Description = "Response column (one value per run)")]
            object response,
            [ExcelArgument(Name = "[terms]", Description = "Terms: \"main\", \"2way\" (default), or \"quadratic\"")]
            object terms = null)
            => OutputWrapper.WrapError(() =>
            {
                double[,] X = M(design); double[] y = V(response);
                var (maxOrder, quadratic) = EffectiveTerms(X, terms);
                return DoeAnalysisCore.Analyze(X, y, maxOrder, quadratic);
            });

        [ExcelFunction(Name = "DOE.ANOVA",
          Description = "Multi-factor ANOVA table (SS, df, MS, F, p per term) from a coded design matrix and response.")]
        public static object UDF_DOE_ANOVA(
            [ExcelArgument(Name = "design", Description = "Coded factor matrix (DOE.PLAN factor columns)")]
            object design,
            [ExcelArgument(Name = "response", Description = "Response column (one value per run)")]
            object response,
            [ExcelArgument(Name = "[terms]", Description = "Terms: \"main\", \"2way\" (default), or \"quadratic\"")]
            object terms = null)
            => OutputWrapper.WrapError(() =>
            {
                double[,] X = M(design); double[] y = V(response);
                var (maxOrder, quadratic) = EffectiveTerms(X, terms);
                return DoeAnalysisCore.Anova(X, y, maxOrder, quadratic);
            });

        [ExcelFunction(Name = "DOE.PARETO",
          Description = "DOE Pareto ranking of effects (term, effect) sorted by descending magnitude.")]
        public static object UDF_DOE_PARETO(
            [ExcelArgument(Name = "design", Description = "Coded factor matrix (DOE.PLAN factor columns)")]
            object design,
            [ExcelArgument(Name = "response", Description = "Response column (one value per run)")]
            object response,
            [ExcelArgument(Name = "[terms]", Description = "Terms: \"main\", \"2way\" (default), or \"quadratic\"")]
            object terms = null)
            => OutputWrapper.WrapError(() =>
            {
                double[,] X = M(design); double[] y = V(response);
                var (maxOrder, quadratic) = EffectiveTerms(X, terms);
                return DoeAnalysisCore.Pareto(X, y, maxOrder, quadratic);
            });

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
