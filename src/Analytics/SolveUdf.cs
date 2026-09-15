using System;
using ExcelDna.Integration;
using ExcelFormulaLabs.Foundation;

namespace ExcelFormulaLabs.Analytics
{
    /// <summary>
    /// SOLVE.* UDF wrappers: process-parameter inversion, forward prediction, model quality
    /// and equation text. Delegates all logic to <see cref="SolveCore"/> (UDF layer only
    /// marshals arguments and wraps errors).
    /// </summary>
    public static class SolveUdf
    {
        [ExcelFunction(Name = "SOLVE.INVERSE",
          Description = "Invert process settings to hit output targets; returns a recommendation table.")]
        public static object UDF_SOLVE_INVERSE(
            [ExcelArgument(Name = "data", Description = "History table with headers; may include request rows with blank adjustable columns. SharedOutput* columns pool one shared rate.")] object data,
            [ExcelArgument(Name = "[request]", Description = "Optional separate request table with the same headers; data is then pure history.")] object request = null,
            [ExcelArgument(Name = "[bounds]", Description = "Optional bounds table (variable name or index, lower, upper); defaults to history min/max.")] object bounds = null,
            [ExcelArgument(Name = "[model]", Description = "Model: auto (default), linear, poly, rate or rate_poly.")] object model = null,
            [ExcelArgument(Name = "[seed]", Description = "Random seed for optimizer and sampling; default 42.")] object seed = null,
            [ExcelArgument(Name = "[max_starts]", Description = "Number of optimization starts (1-50); default 10.")] object maxStarts = null)
            => OutputWrapper.WrapError(() => SolveCore.Inverse(
                RequiredTable(data, "data"),
                OptionalTable(request),
                OptionalTable(bounds),
                InputNormalizer.ToString(model),
                // 空白单元格（ExcelEmpty）与省略同语义回退默认值。
                InputNormalizer.IsOmitted(seed) ? 42L : InputNormalizer.ToLong(seed),
                InputNormalizer.IsOmitted(maxStarts) ? 10 : InputNormalizer.ToInt32(maxStarts)));

        [ExcelFunction(Name = "SOLVE.PREDICT",
          Description = "Forward-predict outputs for one or more complete parameter rows.")]
        public static object UDF_SOLVE_PREDICT(
            [ExcelArgument(Name = "data", Description = "History table with headers.")] object data,
            [ExcelArgument(Name = "values", Description = "One or more rows of non-output values (Incoming + Variable + Fixed, data order).")] object values,
            [ExcelArgument(Name = "[model]", Description = "Model: auto (default), linear, poly, rate or rate_poly.")] object model = null)
            => OutputWrapper.WrapError(() => SolveCore.PredictTable(
                RequiredTable(data, "data"),
                RequiredTable(values, "values"),
                InputNormalizer.ToString(model)));

        [ExcelFunction(Name = "SOLVE.QUALITY",
          Description = "Cross-validated model quality per output (R2 / MAE by candidate model).")]
        public static object UDF_SOLVE_QUALITY(
            [ExcelArgument(Name = "data", Description = "History table with headers.")] object data,
            [ExcelArgument(Name = "[model]", Description = "Model: auto (default), linear, poly, rate or rate_poly.")] object model = null,
            [ExcelArgument(Name = "[seed]", Description = "Random seed for fold shuffling; default 42.")] object seed = null)
            => OutputWrapper.WrapError(() => SolveCore.Quality(
                RequiredTable(data, "data"),
                InputNormalizer.ToString(model),
                InputNormalizer.IsOmitted(seed) ? 42L : InputNormalizer.ToLong(seed)));

        [ExcelFunction(Name = "SOLVE.EQUATION",
          Description = "Forward equation plus closed-form inverse formula for single-variable linear models.")]
        public static object UDF_SOLVE_EQUATION(
            [ExcelArgument(Name = "data", Description = "History table with headers.")] object data,
            [ExcelArgument(Name = "[model]", Description = "Model: auto (default), linear, poly, rate or rate_poly.")] object model = null)
            => OutputWrapper.WrapError(() => SolveCore.Equation(
                RequiredTable(data, "data"),
                InputNormalizer.ToString(model)));

        private static object[,] RequiredTable(object value, string name)
        {
            if (value == null || InputNormalizer.IsExcelMissing(value) || InputNormalizer.IsExcelEmptyValue(value))
                throw new ArgumentException($"'{name}' must be a range or array, not empty.");
            return InputNormalizer.NormalizeTo2D(value)
                ?? throw new ArgumentException($"'{name}' must be a 2D range or array.");
        }

        private static object[,]? OptionalTable(object value)
        {
            if (value == null || InputNormalizer.IsExcelMissing(value) || InputNormalizer.IsExcelEmptyValue(value))
                return null;
            return InputNormalizer.NormalizeTo2D(value);
        }
    }
}
