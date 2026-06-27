using System;
using System.Collections.Generic;
using System.Globalization;

namespace ExcelVbaLibraries.Foundation
{
    /// <summary>Comparison mode for sorting and searching.</summary>
    public enum ComparerMode
    {
        /// <summary>Auto-detect: uses <see cref="ComparisonUtils.Compare"/> for type-aware ordering.</summary>
        Auto,
        /// <summary>Numeric comparison — non-numeric values sort after numeric ones.</summary>
        Numeric,
        /// <summary>Case-insensitive text comparison.</summary>
        Text
    }

    /// <summary>
    /// General-purpose array operations: sort, slice, search, flatten, argsort,
    /// and numeric column detection. Ported from ArrayOps.cls.
    /// </summary>
    public static class ArrayOperations
    {
        private const int INSERTION_SORT_CUTOFF = 16;

        // ─────────────────────────────────────────────────────────────────
        // Sort
        // ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// Sort an array in place using hybrid quicksort + insertion sort (cutoff=16).
        /// </summary>
        public static void Sort<T>(T[] array, bool ascending = true,
            ComparerMode mode = ComparerMode.Auto)
        {
            if (array == null || array.Length <= 1) return;
            QuickSort(array, 0, array.Length - 1, ascending, mode);
        }

        /// <summary>
        /// Return a new sorted array — pure function, does not mutate the original.
        /// </summary>
        public static T[] Sorted<T>(T[] array, bool ascending = true,
            ComparerMode mode = ComparerMode.Auto)
        {
            if (array == null) return Array.Empty<T>();
            var copy = new T[array.Length];
            Array.Copy(array, copy, array.Length);
            Sort(copy, ascending, mode);
            return copy;
        }

        private static void QuickSort<T>(T[] arr, int lo, int hi,
            bool ascending, ComparerMode mode)
        {
            while (lo < hi)
            {
                if (hi - lo + 1 <= INSERTION_SORT_CUTOFF)
                {
                    InsertionSort(arr, lo, hi, ascending, mode);
                    return;
                }
                int pivot = Partition(arr, lo, hi, ascending, mode);
                // Tail-recursion: recurse into smaller partition first
                if (pivot - lo < hi - pivot)
                {
                    QuickSort(arr, lo, pivot - 1, ascending, mode);
                    lo = pivot + 1;
                }
                else
                {
                    QuickSort(arr, pivot + 1, hi, ascending, mode);
                    hi = pivot - 1;
                }
            }
        }

        private static int Partition<T>(T[] arr, int lo, int hi,
            bool ascending, ComparerMode mode)
        {
            int mid = lo + (hi - lo) / 2;
            Swap(arr, mid, hi);
            int i = lo;
            for (int j = lo; j < hi; j++)
            {
                int cmp = CompareElements(arr[j], arr[hi], mode);
                bool shouldSwap = ascending ? cmp < 0 : cmp > 0;
                if (shouldSwap) { Swap(arr, i, j); i++; }
            }
            Swap(arr, i, hi);
            return i;
        }

        private static void InsertionSort<T>(T[] arr, int lo, int hi,
            bool ascending, ComparerMode mode)
        {
            for (int i = lo + 1; i <= hi; i++)
            {
                T key = arr[i];
                int j = i - 1;
                while (j >= lo)
                {
                    int cmp = CompareElements(arr[j], key, mode);
                    bool shouldMove = ascending ? cmp > 0 : cmp < 0;
                    if (shouldMove) { arr[j + 1] = arr[j]; j--; }
                    else break;
                }
                arr[j + 1] = key;
            }
        }

        private static int CompareElements<T>(T a, T b, ComparerMode mode)
        {
            if (a == null && b == null) return 0;
            if (a == null) return 1;
            if (b == null) return -1;
            return mode switch
            {
                ComparerMode.Numeric => CompareNumeric(a, b),
                ComparerMode.Text => CompareText(a, b),
                _ => CompareAuto(a, b),
            };
        }

        /// <summary>
        /// Numeric comparison for sorting. NaN values sort after all finite numbers
        /// (IEEE 754 <c>CompareTo</c> behaviour made explicit for auditability).
        /// </summary>
        private static int CompareNumeric<T>(T a, T b)
        {
            bool aNum = ComparisonUtils.IsNumeric(a);
            bool bNum = ComparisonUtils.IsNumeric(b);
            if (!aNum && !bNum) return 0;
            if (!aNum) return 1;
            if (!bNum) return -1;
            double dA = Convert.ToDouble(a, System.Globalization.CultureInfo.InvariantCulture);
            double dB = Convert.ToDouble(b, System.Globalization.CultureInfo.InvariantCulture);
            // Explicit NaN guard (防错原则1): NaN sorts last, consistent with IEEE 754 CompareTo.
            // IsNumeric above returns true for double.NaN (it IS a double), so NaN reaches here.
            if (double.IsNaN(dA) && double.IsNaN(dB)) return 0;
            if (double.IsNaN(dA)) return 1;
            if (double.IsNaN(dB)) return -1;
            return dA.CompareTo(dB);
        }

        private static int CompareText<T>(T a, T b)
        {
            string sA = a?.ToString() ?? "";
            string sB = b?.ToString() ?? "";
            return string.Compare(sA, sB, StringComparison.CurrentCultureIgnoreCase);
        }

        private static int CompareAuto<T>(T a, T b) => ComparisonUtils.Compare(a!, b!);

        // ─────────────────────────────────────────────────────────────────
        // Slice
        // ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// Extract a slice. Python-style: negative start counts from end.
        /// length=-1 means "to end". Returns empty array for invalid ranges.
        /// </summary>
        public static T[] Slice<T>(T[] array, int start, int length = -1)
        {
            if (array == null || array.Length == 0) return Array.Empty<T>();
            int n = array.Length;
            if (start < 0) start = n + start;
            if (start < 0) start = 0;
            if (start >= n) return Array.Empty<T>();
            if (length == -1) length = n - start;
            if (length <= 0) return Array.Empty<T>();
            if (start + length > n) length = n - start;
            var result = new T[length];
            Array.Copy(array, start, result, 0, length);
            return result;
        }

        // ─────────────────────────────────────────────────────────────────
        // IndexOf
        // ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// Linear search. Returns 0-based index or -1.
        /// For floating-point types, uses tolerance (default 1e-12).
        /// </summary>
        public static int IndexOf<T>(T[] array, T value, double tolerance = 1e-12)
        {
            if (array == null) return -1;
            bool isFloat = typeof(T) == typeof(double) || typeof(T) == typeof(float);
            for (int i = 0; i < array.Length; i++)
            {
                if (array[i] == null && value == null) return i;
                if (array[i] == null || value == null) continue;
                if (isFloat)
                {
                    double dA = Convert.ToDouble(array[i], System.Globalization.CultureInfo.InvariantCulture);
                    double dB = Convert.ToDouble(value, System.Globalization.CultureInfo.InvariantCulture);
                    // Explicit NaN guard (防错原则1): NaN == NaN for search purposes,
                    // otherwise Math.Abs(NaN - NaN) = NaN < tolerance = false → never found.
                    if (double.IsNaN(dA) && double.IsNaN(dB)) return i;
                    if (double.IsNaN(dA) || double.IsNaN(dB)) continue;
                    if (Math.Abs(dA - dB) < tolerance) return i;
                }
                else if (array[i]!.Equals(value)) return i;
            }
            return -1;
        }

        // ─────────────────────────────────────────────────────────────────
        // Flatten
        // ─────────────────────────────────────────────────────────────────

        /// <summary>Flatten 2D array to 1D in row-major or column-major order.</summary>
        public static T[] Flatten<T>(T[,] array,
            NormalizeOrder order = NormalizeOrder.RowMajor)
        {
            if (array == null) return Array.Empty<T>();
            int rows = array.GetLength(0), cols = array.GetLength(1);
            var result = new T[rows * cols];
            if (order == NormalizeOrder.RowMajor)
                for (int r = 0; r < rows; r++)
                    for (int c = 0; c < cols; c++)
                        result[r * cols + c] = array[r, c];
            else
                for (int c = 0; c < cols; c++)
                    for (int r = 0; r < rows; r++)
                        result[c * rows + r] = array[r, c];
            return result;
        }

        // ─────────────────────────────────────────────────────────────────
        // SortIndices (argsort)
        // ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// Sort indices in place so that values[indices[i]] is in sorted order.
        /// Original values array is unchanged.
        /// </summary>
        public static void SortIndices<T>(T[] values, int[] indices,
            bool ascending = true, ComparerMode mode = ComparerMode.Auto)
        {
            if (values == null || indices == null || indices.Length <= 1) return;
            QuickSortIndices(values, indices, 0, indices.Length - 1, ascending, mode);
        }

        private static void QuickSortIndices<T>(T[] values, int[] idx,
            int lo, int hi, bool ascending, ComparerMode mode)
        {
            while (lo < hi)
            {
                if (hi - lo + 1 <= INSERTION_SORT_CUTOFF)
                {
                    InsertionSortIndices(values, idx, lo, hi, ascending, mode);
                    return;
                }
                int pivot = PartitionIndices(values, idx, lo, hi, ascending, mode);
                if (pivot - lo < hi - pivot)
                {
                    QuickSortIndices(values, idx, lo, pivot - 1, ascending, mode);
                    lo = pivot + 1;
                }
                else
                {
                    QuickSortIndices(values, idx, pivot + 1, hi, ascending, mode);
                    hi = pivot - 1;
                }
            }
        }

        private static int PartitionIndices<T>(T[] values, int[] idx,
            int lo, int hi, bool ascending, ComparerMode mode)
        {
            int mid = lo + (hi - lo) / 2;
            Swap(idx, mid, hi);
            int i = lo;
            for (int j = lo; j < hi; j++)
            {
                int cmp = CompareElements(values[idx[j]], values[idx[hi]], mode);
                bool shouldSwap = ascending ? cmp < 0 : cmp > 0;
                if (shouldSwap) { Swap(idx, i, j); i++; }
            }
            Swap(idx, i, hi);
            return i;
        }

        private static void InsertionSortIndices<T>(T[] values, int[] idx,
            int lo, int hi, bool ascending, ComparerMode mode)
        {
            for (int i = lo + 1; i <= hi; i++)
            {
                int key = idx[i];
                int j = i - 1;
                while (j >= lo)
                {
                    int cmp = CompareElements(values[idx[j]], values[key], mode);
                    bool shouldMove = ascending ? cmp > 0 : cmp < 0;
                    if (shouldMove) { idx[j + 1] = idx[j]; j--; }
                    else break;
                }
                idx[j + 1] = key;
            }
        }

        private static void Swap<T>(T[] arr, int i, int j)
        {
            T tmp = arr[i]; arr[i] = arr[j]; arr[j] = tmp;
        }

        // ─────────────────────────────────────────────────────────────────
        // CollectNumericColumns
        // ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// Identify columns in 2D data where all non-empty rows are numeric.
        /// </summary>
        /// <param name="data">2D data (rows × cols).</param>
        /// <param name="numRows">Total rows.</param>
        /// <param name="numCols">Total columns.</param>
        /// <param name="colNames">Output: column names (header row or "Col1","Col2",...).</param>
        /// <param name="hasHeader">If true, row 0 = header, data starts at row 1.</param>
        /// <returns>0-based indices of all-numeric columns.</returns>
        public static int[] CollectNumericColumns(
            object[,] data, int numRows, int numCols,
            out string[] colNames, bool hasHeader = true)
        {
            colNames = new string[numCols];
            var numericCols = new List<int>();
            int dataStartRow = hasHeader ? 1 : 0;

            for (int c = 0; c < numCols; c++)
            {
                colNames[c] = (hasHeader && numRows > 0)
                    ? InputNormalizer.ToString(data[0, c])
                    : $"Col{c + 1}";

                bool allNumeric = true;
                for (int r = dataStartRow; r < numRows; r++)
                {
                    object cell = data[r, c];
                    if (cell == null || cell is DBNull ||
                        InputNormalizer.IsExcelEmptyValue(cell))
                        continue;
                    if (!InputNormalizer.IsNumericCell(cell))
                    { allNumeric = false; break; }
                }
                if (allNumeric) numericCols.Add(c);
            }
            return numericCols.ToArray();
        }
    }
}
