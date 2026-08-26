using ExcelDna.Integration;
using ExcelFormulaLabs.Foundation;

namespace ExcelFormulaLabs.Analytics
{
    public static class DoeUdf
    {
        [ExcelFunction(Name = "DOE.PLAN",
          Description = "Generate a DOE design matrix (full factorial); StdOrder + RunOrder + coded factors.")]
        public static object UDF_DOE_PLAN(
            [ExcelArgument(Name = "factor_qty1", Description = "Number of factors in group 1")]
            object qty1,
            [ExcelArgument(Name = "factor_level1", Description = "Levels per factor in group 1")]
            object level1,
            [ExcelArgument(Name = "factor_qty2", Description = "Number of factors in group 2")]
            object qty2,
            [ExcelArgument(Name = "factor_level2", Description = "Levels per factor in group 2")]
            object level2,
            [ExcelArgument(Name = "method", Description = "Design method: FULL")]
            object method,
            [ExcelArgument(Name = "[randomize]", Description = "Randomize run order (default TRUE)")]
            object randomize = null,
            [ExcelArgument(Name = "[seed]", Description = "Fixed seed for reproducible run order")]
            object seed = null)
            => OutputWrapper.WrapError(() => DoeCore.Plan(
                (int)InputNormalizer.ToLong(qty1),
                (int)InputNormalizer.ToLong(level1),
                (int)InputNormalizer.ToLong(qty2),
                (int)InputNormalizer.ToLong(level2),
                InputNormalizer.ToString(method),
                randomize == null || randomize is ExcelMissing ? true : InputNormalizer.ToBool(randomize),
                seed == null || seed is ExcelMissing ? (long?)null : InputNormalizer.ToLong(seed)));
    }
}
