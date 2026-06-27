using System;
using FormulaLabs.Foundation;

namespace FormulaLabs.Analytics
{
    internal static class AnalyticsHelpers
    {
        internal static double[,] ToDoubleMatrix(object[,] data)
        {
            int r = data.GetLength(0), c = data.GetLength(1);
            var m = new double[r, c];
            for (int i = 0; i < r; i++)
                for (int j = 0; j < c; j++)
                {
                    double v = InputNormalizer.ToDouble(data[i, j]);
                    if (double.IsNaN(v) || double.IsInfinity(v))
                        throw new ArgumentException(
                            $"Matrix contains non-numeric value at [{i},{j}]: '{data[i, j]}'. " +
                            "All cells must be numeric for linear algebra / regression operations.");
                    m[i, j] = v;
                }
            return m;
        }
        internal static double[,] PrepM(object data)
        {
            var normal = InputNormalizer.NormalizeTo2D(data);
            if (normal == null) throw new ArgumentException("Cannot convert input to 2D array.");
            return ToDoubleMatrix(normal);
        }
        /// <summary>
        /// Converts input to a double[] vector for statistical UDFs.
        /// Throws on NaN/Inf — consistent with PrepM which also throws.
        /// (Previously used ToDoubles which silently filtered non-numeric values,
        /// causing length mismatches between X and y when y had NaN/Inf but X did not.)
        /// </summary>
        internal static double[] PrepV(object data)
        {
            var raw = InputNormalizer.NormalizeTo1D(data);
            var result = new System.Collections.Generic.List<double>(raw.Length);
            for (int i = 0; i < raw.Length; i++)
            {
                if (!InputNormalizer.IsNumericCell(raw[i])) continue;
                double v = InputNormalizer.ToDouble(raw[i]);
                if (double.IsNaN(v) || double.IsInfinity(v))
                    throw new ArgumentException(
                        $"Vector contains {(double.IsNaN(v) ? "NaN" : "Infinity")} at index {i}. " +
                        "All values must be finite for statistical operations.");
                result.Add(v);
            }
            return result.ToArray();
        }
    }
}
