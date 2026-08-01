using System;
using System.Collections.Generic;
using System.Linq;
using ExcelFormulaLabs.Foundation;

namespace ExcelFormulaLabs.Analytics
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
            if (normal == null) throw new ArgumentException(ErrorMsg.Get("Convert_Not2DArray"));
            return ToDoubleMatrix(normal);
        }
        /// <summary>
        /// Converts input to a double[] vector for statistical UDFs.
        /// Throws on any non-numeric cell (text, errors, empty, NaN, Inf)
        /// — consistent with PrepM / ToDoubleMatrix which also throws.
        /// (Previously used IsNumericCell to silently skip non-numeric values,
        /// which caused length mismatches and silent subset computation.)
        /// </summary>
        internal static double[] PrepV(object data)
        {
            var raw = InputNormalizer.NormalizeTo1D(data);
            var result = new System.Collections.Generic.List<double>(raw.Length);
            for (int i = 0; i < raw.Length; i++)
            {
                double v = InputNormalizer.ToDouble(raw[i]);
                if (double.IsNaN(v) || double.IsInfinity(v))
                    throw new ArgumentException(
                        $"Vector contains {(double.IsNaN(v) ? "NaN" : "Infinity")} at index {i}. " +
                        "All values must be finite for statistical operations.");
                result.Add(v);
            }
            return result.ToArray();
        }

        /// <summary>
        /// Extract columns from a 2D input into a jagged double array (one array per column).
        /// NaN values (from empty/error cells) are skipped per column.
        /// Used by ANOVA1 to convert an Excel range into group arrays.
        /// </summary>
        internal static double[][] ToJaggedColumns(object data)
        {
            var m = InputNormalizer.NormalizeTo2D(data)
                ?? throw new ArgumentException(ErrorMsg.Get("Convert_Not2DArray"));
            int rows = m.GetLength(0), cols = m.GetLength(1);
            var groups = new double[cols][];
            for (int c = 0; c < cols; c++)
            {
                var list = new List<double>(rows);
                for (int r = 0; r < rows; r++)
                {
                    double v = InputNormalizer.ToDouble(m[r, c]);
                    if (!double.IsNaN(v)) list.Add(v);
                }
                groups[c] = list.ToArray();
            }
            return groups;
        }

        /// <summary>
        /// Convert a Dictionary{string,object} to an Excel-compatible report table.
        /// Each field becomes a row: column 0 = field name, columns 1.. = scalar or
        /// unpacked array elements.
        /// </summary>
        internal static object[,] DictToReport(Dictionary<string, object> d)
        {
            var keys = d.Keys.ToArray();
            int n = keys.Length;

            // Determine max width from array-valued fields (minimum 1 for scalars)
            int maxLen = 1;
            foreach (var key in keys)
            {
                var val = d[key];
                if (val is double[] da && da.Length > maxLen) maxLen = da.Length;
                else if (val is long[] la && la.Length > maxLen) maxLen = la.Length;
                else if (val is object[] oa && oa.Length > maxLen) maxLen = oa.Length;
                else if (val is System.Array arr && arr.Length > maxLen) maxLen = arr.Length;
            }

            var result = new object[n, maxLen + 1];

            for (int i = 0; i < n; i++)
            {
                result[i, 0] = keys[i];
                var val = d[keys[i]];
                int len = 1;

                if (val is double[] da)
                {
                    for (int j = 0; j < da.Length; j++)
                        result[i, j + 1] = da[j];
                    len = da.Length;
                }
                else if (val is long[] la)
                {
                    for (int j = 0; j < la.Length; j++)
                        result[i, j + 1] = la[j];
                    len = la.Length;
                }
                else if (val is object[] oa)
                {
                    for (int j = 0; j < oa.Length; j++)
                        result[i, j + 1] = oa[j] ?? ExcelEmpty.Value;
                    len = oa.Length;
                }
                else if (val is System.Array arr)
                {
                    for (int j = 0; j < arr.Length; j++)
                        result[i, j + 1] = arr.GetValue(j) ?? ExcelEmpty.Value;
                    len = arr.Length;
                }
                else
                {
                    result[i, 1] = val ?? ExcelEmpty.Value;
                }

                // Pad remaining cells in this row with ExcelEmpty
                for (int j = len + 1; j <= maxLen; j++)
                    result[i, j] = ExcelEmpty.Value;
            }

            return result;
        }
    }
}
