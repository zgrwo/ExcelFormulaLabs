using System;
using System.Collections.Generic;
using System.Globalization;

namespace ExcelFormulaLabs.Foundation
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
                // review 2026-08-31（深度审查 P1-2）：Lomuto 分区在全等值/高重复输入下
                // 退化为 O(n²)——全等值 20 万实测 152 秒（复刻计时）。改为 3-way（Dutch
                // national flag）分区，把与 pivot 相等的元素集中到中间段一次跳过。
                var (lt, gt) = Partition3(arr, lo, hi, ascending, mode);
                // Tail-recursion: recurse into smaller side first, skip the equal band [lt, gt]
                if (lt - lo < hi - gt)
                {
                    QuickSort(arr, lo, lt - 1, ascending, mode);
                    lo = gt + 1;
                }
                else
                {
                    QuickSort(arr, gt + 1, hi, ascending, mode);
                    hi = lt - 1;
                }
            }
        }

        /// <summary>3-way partition (Dutch national flag): returns (lt, gt) such that
        /// [lo, lt) &lt; pivot, [lt, gt] == pivot, (gt, hi] &gt; pivot.</summary>
        private static (int lt, int gt) Partition3<T>(T[] arr, int lo, int hi,
            bool ascending, ComparerMode mode)
        {
            int mid = lo + (hi - lo) / 2;
            Swap(arr, mid, hi);
            int lt = lo, i = lo, gt = hi;
            T pivot = arr[hi];
            while (i <= gt)
            {
                int cmp = CompareElements(arr[i], pivot, mode);
                bool less = ascending ? cmp < 0 : cmp > 0;
                bool greater = ascending ? cmp > 0 : cmp < 0;
                if (less) { Swap(arr, lt, i); lt++; i++; }
                else if (greater) { Swap(arr, i, gt); gt--; }
                else i++;
            }
            return (lt, gt);
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
            // P2 (pre-release review): null sorts FIRST — consistent with the documented
            // VBA VariantKit order in ComparisonUtils.Compare (Null → Empty → values → Error).
            if (a == null) return -1;
            if (b == null) return 1;
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
        /// For floating-point values, matches within a relative tolerance (default 1e-12):
        /// |a−b| &lt; tolerance·max(|a|,|b|). Exact floating-point equality always matches.
        /// review 2026-09-04（reaudit D1）：原绝对容差 |a−b| &lt; 1e-12 在数据量纲小于容差时
        /// 任何查值都命中第一个元素（假阳性：{1e-16;2e-16;3e-16} 查 3e-16 返回 0），且
        /// P1-5 已论证绝对阈值对小量纲失效——同一认知未跨函数复用。改纯相对：量纲 &lt; 1 的
        /// 数据（ppm/ppb/µA/nm…）退化为精确比较；O(1) 量级仍能桥接浮点累差（0.1+0.2≈0.3）。
        /// 注意相对窗口随量级增长，超大值（1e12 级）相邻数据不再有 1e-12 的绝对安全区。
        /// </summary>
        public static int IndexOf<T>(T[] array, T value, double tolerance = 1e-12)
        {
            if (array == null) return -1;
            // review 2026-08-31（深度审查 P1-3）：原 `typeof(T) == typeof(double)` 判断在
            // T=object（ARR.INDEXOF / ARR.CONTAINS 的实际调用路径，ArrayCore.IndexOf 传 object[]）
            // 下恒为 false → 1e-12 容差分支是死代码，退化为装箱 Equals 精确比较：
            // {0.1+0.2} 查 0.3 → -1；整型 1 查浮点 1.0 → 找不到。改为运行时逐元素探测——
            // 元素与目标值均为数值类型时走容差路径（文本 "2" 不匹配数值 2，保持原 Equals 语义）。
            bool valueIsNumeric = value is double or float or int or long;
            for (int i = 0; i < array.Length; i++)
            {
                if (array[i] == null && value == null) return i;
                if (array[i] == null || value == null) continue;
                if (valueIsNumeric && array[i] is double or float or int or long)
                {
                    double dA = Convert.ToDouble(array[i], System.Globalization.CultureInfo.InvariantCulture);
                    double dB = Convert.ToDouble(value, System.Globalization.CultureInfo.InvariantCulture);
                    // Explicit NaN guard (防错原则1): NaN == NaN for search purposes,
                    // otherwise Math.Abs(NaN - NaN) = NaN < tolerance = false → never found.
                    if (double.IsNaN(dA) && double.IsNaN(dB)) return i;
                    if (double.IsNaN(dA) || double.IsNaN(dB)) continue;
                    // P2-13 (review-2026-08-31): same-sign Infinity equals itself —
                    // Math.Abs(Inf − Inf) = NaN < tolerance is false → would never match.
                    // Cross-sign Infinity is not equal. Mirrors ComparisonUtils.ValuesEqual.
                    if (double.IsInfinity(dA) || double.IsInfinity(dB))
                    {
                        if (dA == dB) return i;
                        continue;
                    }
                    // 相等快路径（覆盖 0==0、同值）：相对容差窗口在双零时下溢为 0，不能依赖它判等。
                    if (dA == dB) return i;
                    // 纯相对容差：|a−b| < tolerance·max(|a|,|b|)。
                    if (Math.Abs(dA - dB) <
                        tolerance * Math.Max(Math.Abs(dA), Math.Abs(dB))) return i;
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
                // review-2026-08-31（深度审查 P1-2）：Lomuto 分区在全等值输入下 O(n²)——
                // argsort 路径（ARR.ARGSORT）与值排序同模式，一并改为 3-way 分区。
                var (lt, gt) = PartitionIndices3(values, idx, lo, hi, ascending, mode);
                if (lt - lo < hi - gt)
                {
                    QuickSortIndices(values, idx, lo, lt - 1, ascending, mode);
                    lo = gt + 1;
                }
                else
                {
                    QuickSortIndices(values, idx, gt + 1, hi, ascending, mode);
                    hi = lt - 1;
                }
            }
        }

        /// <summary>3-way partition for argsort: [lo, lt) &lt; pivot, [lt, gt] == pivot, (gt, hi] &gt; pivot.</summary>
        private static (int lt, int gt) PartitionIndices3<T>(T[] values, int[] idx,
            int lo, int hi, bool ascending, ComparerMode mode)
        {
            int mid = lo + (hi - lo) / 2;
            Swap(idx, mid, hi);
            int lt = lo, i = lo, gt = hi;
            T pivot = values[idx[hi]];
            while (i <= gt)
            {
                int cmp = CompareElements(values[idx[i]], pivot, mode);
                bool less = ascending ? cmp < 0 : cmp > 0;
                bool greater = ascending ? cmp > 0 : cmp < 0;
                if (less) { Swap(idx, lt, i); lt++; i++; }
                else if (greater) { Swap(idx, i, gt); gt--; }
                else i++;
            }
            return (lt, gt);
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
        /// <param name="hasHeaders">If true, row 0 = header, data starts at row 1.</param>
        /// <returns>0-based indices of all-numeric columns.</returns>
        public static int[] CollectNumericColumns(
            object[,] data, int numRows, int numCols,
            out string[] colNames, bool hasHeaders = true)
        {
            colNames = new string[numCols];
            var numericCols = new List<int>();
            int dataStartRow = hasHeaders ? 1 : 0;

            for (int c = 0; c < numCols; c++)
            {
                colNames[c] = (hasHeaders && numRows > 0)
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