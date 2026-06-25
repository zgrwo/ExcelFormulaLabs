using System;

namespace ExcelVbaLibraries.Foundation
{
    /// <summary>
    /// Shared NaN/Infinity input validation for numeric arrays.
    /// Per 防错原则1: IEEE 754 non-finite values must be explicitly guarded
    /// before computation — CLR and MathNet behaviour is undefined for NaN/Inf.
    /// </summary>
    public static class NumericGuard
    {
        /// <summary>
        /// Validates that all elements of a 2D matrix are finite.
        /// Throws ArgumentException on the first NaN or Infinity found, with position.
        /// </summary>
        public static void AgainstNonFinite(double[,] matrix)
        {
            int rows = matrix.GetLength(0), cols = matrix.GetLength(1);
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                {
                    double v = matrix[r, c];
                    if (double.IsNaN(v) || double.IsInfinity(v))
                        throw new ArgumentException(
                            $"Matrix contains {(double.IsNaN(v) ? "NaN" : "Infinity")} at [{r},{c}]. " +
                            "This operation requires finite values.");
                }
        }

        /// <summary>
        /// Validates that all elements of a matrix and a vector are finite.
        /// </summary>
        public static void AgainstNonFinite(double[,] matrix, double[] vector)
        {
            AgainstNonFinite(matrix);
            for (int i = 0; i < vector.Length; i++)
                if (double.IsNaN(vector[i]) || double.IsInfinity(vector[i]))
                    throw new ArgumentException(
                        $"Vector contains {(double.IsNaN(vector[i]) ? "NaN" : "Infinity")} at index {i}. " +
                        "This operation requires finite values.");
        }

        /// <summary>
        /// Validates a single double value is finite.
        /// </summary>
        public static void AgainstNonFinite(double value, string paramName)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                throw new ArgumentException(
                    $"Parameter '{paramName}' must be a finite value (got {value}).");
        }
    }
}
