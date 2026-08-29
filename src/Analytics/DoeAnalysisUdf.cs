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
                var (maxOrder, quadratic) = ParseTerms(terms);
                return DoeAnalysisCore.Analyze(M(design), V(response), maxOrder, quadratic);
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
                var (maxOrder, quadratic) = ParseTerms(terms);
                return DoeAnalysisCore.Anova(M(design), V(response), maxOrder, quadratic);
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
                var (maxOrder, quadratic) = ParseTerms(terms);
                return DoeAnalysisCore.Pareto(M(design), V(response), maxOrder, quadratic);
            });

        private static (int maxOrder, bool quadratic) ParseTerms(object terms)
        {
            if (terms == null || InputNormalizer.IsExcelMissing(terms))
                return (2, false); // default: main + 2-way interactions
            string t = InputNormalizer.ToString(terms).Trim().ToUpperInvariant();
            return t switch
            {
                "MAIN" or "1" => (1, false),
                "2WAY" or "2" => (2, false),
                "QUADRATIC" or "Q" or "FULL" => (2, true),
                _ => throw new System.ArgumentException(
                    $"Unknown terms '{t}'. Supported: main, 2way, quadratic.")
            };
        }
    }
}
