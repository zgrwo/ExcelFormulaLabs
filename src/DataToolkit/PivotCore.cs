using System;
using System.Collections.Generic;
using System.Linq;
using ExcelVbaLibraries.Foundation;

namespace ExcelVbaLibraries.DataToolkit
{
    /// <summary>Pivot, unpivot, grouping, cross join. Ported from PivotUtils.bas.</summary>
    internal static class PivotCore
    {
        internal static object[,] Pivot(object[,] data, int keyCol, int pivotCol, int valueCol, string agg = "SUM", bool hasHeaders = true)
        {
            agg = agg.ToUpperInvariant();
            int rows = data.GetLength(0);
            int startRow = hasHeaders ? 1 : 0;
            var map = new Dictionary<(string k, string p), double>();
            var cnt = new Dictionary<(string k, string p), long>(); // for AVG/COUNT
            var keySet = new HashSet<string>(); var keyList = new List<string>();
            var pivotSet = new HashSet<string>(); var pivotList = new List<string>();
            for (int r = startRow; r < rows; r++)
            {
                string k = InputNormalizer.ToString(data[r, keyCol]);
                string p = InputNormalizer.ToString(data[r, pivotCol]);
                double v = InputNormalizer.ToDouble(data[r, valueCol]);
                if (double.IsNaN(v)) continue;
                if (keySet.Add(k)) keyList.Add(k);
                if (pivotSet.Add(p)) pivotList.Add(p);
                var kv = (k, p);
                if (map.TryGetValue(kv, out double ex))
                {
                    cnt[kv] = cnt[kv] + 1;
                    map[kv] = agg switch { "MAX" => Math.Max(ex, v), "MIN" => Math.Min(ex, v), "SUM" or "AVG" or "COUNT" => ex + v, _ => throw new ArgumentException($"Unknown aggregation '{agg}'. Supported: SUM, AVG, COUNT, MAX, MIN.") };
                }
                else { map[kv] = v; cnt[kv] = 1; }
            }
            var keys = keyList; var pivots = pivotList;
            var result = new object[keys.Count + 1, pivots.Count + 1];
            result[0, 0] = "Key \\ Pivot";
            for (int c = 0; c < pivots.Count; c++) result[0, c + 1] = pivots[c];
            for (int r = 0; r < keys.Count; r++) { result[r + 1, 0] = keys[r]; for (int c = 0; c < pivots.Count; c++)
            {
                var kv = (keys[r], pivots[c]);
                result[r + 1, c + 1] = map.TryGetValue(kv, out double v)
                    ? (agg == "AVG" ? v / cnt[kv] : agg == "COUNT" ? cnt[kv] : v)
                    : ExcelVbaLibraries.Foundation.ExcelEmpty.Value;
            } }
            return result;
        }

        internal static object[,] Unpivot(object[,] data, int[] idCols, int[] valueCols, bool hasHeaders = true)
        {
            int cols = data.GetLength(1);
            if (idCols.Any(c => c < 0 || c >= cols) || valueCols.Any(c => c < 0 || c >= cols))
                throw new ArgumentException("Column index out of range.");
            int rows = data.GetLength(0); int nId = idCols.Length;
            int dataStartRow = hasHeaders ? 1 : 0;
            if (hasHeaders && rows < 2) return new object[0, 0];  // header-only or empty table
            if (!hasHeaders && rows < 1) return new object[0, 0];
            var result = new List<object[]>();
            int outWidth = nId + 2;
            for (int r = dataStartRow; r < rows; r++)
            {
                foreach (int vc in valueCols)
                {
                    // Pre-allocate output row and fill directly — avoids per-cell
                    // Select().ToArray() and Concat().ToArray() allocations.
                    var row = new object[outWidth];
                    for (int j = 0; j < nId; j++) row[j] = data[r, idCols[j]];
                    row[nId] = hasHeaders ? data[0, vc] : $"Var{vc + 1}";
                    row[nId + 1] = data[r, vc];
                    result.Add(row);
                }
            }
            var outArr = new object[result.Count, nId + 2];
            for (int i = 0; i < result.Count; i++) for (int j = 0; j < result[i].Length; j++) outArr[i, j] = result[i][j];
            return outArr;
        }

        internal static object[,] GroupBy(object[,] data, int[] gCols, int aCol, string agg = "SUM", bool hasHeaders = true)
        {
            agg = agg.ToUpperInvariant();
            int rows = data.GetLength(0), nG = gCols.Length;
            int startRow = hasHeaders ? 1 : 0;
            var groups = new Dictionary<string, (double val, long cnt)>();
            var keyNames = new List<string[]>(); var seen = new HashSet<string>();
            for (int r = startRow; r < rows; r++)
            {
                var gk = gCols.Select(c => InputNormalizer.ToString(data[r, c])).ToArray();
                string gks = MakeCompoundKey(gk); double v = InputNormalizer.ToDouble(data[r, aCol]);
                if (double.IsNaN(v)) continue;
                if (groups.TryGetValue(gks, out var ex)) groups[gks] = agg switch { "SUM" => (ex.val + v, ex.cnt + 1), "MAX" => (Math.Max(ex.val, v), ex.cnt + 1), "MIN" => (Math.Min(ex.val, v), ex.cnt + 1), "COUNT" => (0, ex.cnt + 1), "AVG" => (ex.val + v, ex.cnt + 1), _ => throw new ArgumentException($"Unknown aggregation '{agg}'. Supported: SUM, AVG, COUNT, MAX, MIN.") };
                else { groups[gks] = (v, 1); if (seen.Add(gks)) keyNames.Add(gk); }
            }
            var result = new object[keyNames.Count, nG + 1];
            for (int i = 0; i < keyNames.Count; i++) { var kn = keyNames[i]; for (int j = 0; j < nG; j++) result[i, j] = kn[j]; var (val, cnt) = groups[MakeCompoundKey(kn)]; result[i, nG] = agg == "COUNT" ? cnt : (agg == "AVG" ? val / cnt : val); }
            return result;
        }

        /// <summary>Build a collision-free compound key from already-stringified segments using length-prefix encoding.</summary>
        private static string MakeCompoundKey(string[] parts)
        {
            var sb = new System.Text.StringBuilder();
            foreach (var s in parts) { sb.Append(s.Length); sb.Append(':'); sb.Append(s); }
            return sb.ToString();
        }

        internal static object[,] CrossJoin(object[,] a, object[,] b)
        {
            int ra = a.GetLength(0), ca = a.GetLength(1), rb = b.GetLength(0), cb = b.GetLength(1);
            const int maxCells = 1_000_000;
            long totalCells = (long)ra * rb * (ca + cb);
            if (totalCells > maxCells)
                throw new ArgumentException(
                    $"Cross join would produce {ra * rb:N0} rows × {ca + cb} cols = {totalCells:N0} cells. " +
                    $"Maximum is {maxCells:N0} cells. Reduce input size or use a join condition instead.");
            var r = new object[ra * rb, ca + cb];
            for (int i = 0; i < ra; i++)
                for (int j = 0; j < rb; j++)
                {
                    int dr = i * rb + j;
                    for (int c = 0; c < ca; c++) r[dr, c] = a[i, c];
                    for (int c = 0; c < cb; c++) r[dr, ca + c] = b[j, c];
                }
            return r;
        }
    }
}
