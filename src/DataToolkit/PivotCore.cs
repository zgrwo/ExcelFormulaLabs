using System;
using System.Collections.Generic;
using System.Linq;
using ExcelFormulaLabs.Foundation;

namespace ExcelFormulaLabs.DataToolkit
{
    /// <summary>Pivot, unpivot, grouping, cross join. Ported from PivotUtils.bas.</summary>
    internal static class PivotCore
    {
        private static void ValidateAgg(string agg)
        {
            if (agg is not ("SUM" or "AVG" or "COUNT" or "MAX" or "MIN"))
                throw new ArgumentException(ErrorMsg.Get("PIVOT_UnknownAgg", agg));
        }

        private static (double val, double comp) Accumulate(string agg, (double val, double comp) current, double incoming) => agg switch
        {
            "MAX" => (Math.Max(current.val, incoming), 0),
            "MIN" => (Math.Min(current.val, incoming), 0),
            _ => NeumaierAdd(current, incoming)  // SUM, AVG
        };

        /// <summary>Compute final aggregation cell value from accumulator and count.</summary>
        private static object AggResult(string agg, double val, double comp, long cnt) => agg switch
        {
            // review 2026-08-29（发行前 max level 复审）：SUM/AVG 累加 `current+incoming` 在极端值下
            // 可溢出为 ±Inf，原实现原样返回 → PIVOT/GROUPBY 单元格泄漏 Inf，违反防错原则①
            // （IEEE 传播无显式守卫，与 StatsCore.Sum/Product 的 Inf→NaN 约定不一致）。
            // 累加产生非有限值时返回 NaN 而非透传 Inf。
            // review 2026-08-31（深度审查 P2-10）：SUM/AVG 改用 Neumaier 补偿求和——
            // 朴素 `ex+v` 在大数+小数（灾难性抵消）场景丢精度；补偿项累积舍入误差，
            // 最终结果 = val + comp。
            "AVG"   => cnt == 0 ? double.NaN : (double.IsNaN(val + comp) || double.IsInfinity(val + comp) ? double.NaN : (val + comp) / cnt),
            "COUNT" => (object)cnt,  // keep as long, not double
            _       => (double.IsNaN(val + comp) || double.IsInfinity(val + comp)) ? double.NaN : (val + comp),  // SUM, MAX, MIN
        };

        /// <summary>Neumaier compensated addition: returns (t, comp) with t = a.sum + b (round-to-nearest) and
        /// comp accumulating the rounding error. Final sum = t + comp.</summary>
        private static (double sum, double comp) NeumaierAdd((double sum, double comp) acc, double v)
        {
            double t = acc.sum + v;
            if (Math.Abs(acc.sum) >= Math.Abs(v)) acc.comp += (acc.sum - t) + v;
            else acc.comp += (v - t) + acc.sum;
            return (t, acc.comp);
        }

        internal static object[,] Pivot(object[,] data, int keyCol, int pivotCol, int valueCol, string agg = "SUM", bool hasHeaders = true)
        {
            agg = agg.ToUpperInvariant();
            ValidateAgg(agg);
            int cols = data.GetLength(1);
            if (keyCol < 0 || keyCol >= cols || pivotCol < 0 || pivotCol >= cols || valueCol < 0 || valueCol >= cols)
                throw new ArgumentException(ErrorMsg.Get("PIVOT_ColumnOutOfRange", cols) +
                    $" keyCol={keyCol}, pivotCol={pivotCol}, valueCol={valueCol}.");
            int rows = data.GetLength(0);
            int startRow = hasHeaders ? 1 : 0;
            var map = new Dictionary<(string k, string p), (double val, double comp)>();
            var cnt = new Dictionary<(string k, string p), long>(); // for AVG/COUNT
            var keySet = new HashSet<string>(); var keyList = new List<string>();
            var pivotSet = new HashSet<string>(); var pivotList = new List<string>();
            for (int r = startRow; r < rows; r++)
            {
                string k = InputNormalizer.ToString(data[r, keyCol]);
                string p = InputNormalizer.ToString(data[r, pivotCol]);
                double v = InputNormalizer.ToDouble(data[r, valueCol]);
                bool numeric = !(double.IsNaN(v) || double.IsInfinity(v));
                // review 2026-08-31（深度审查 P1-10）：COUNT 的本意是统计行数（含空/文本值行）——
                // 原实现非数值行在 key/pivot 收录前 continue → 该分组的 COUNT 少计、纯空值分组的
                // 行/列标签整体丢失。COUNT 单独分支：非数值行也计数并收录 key/pivot。
                if (agg == "COUNT")
                {
                    if (keySet.Add(k)) keyList.Add(k);
                    if (pivotSet.Add(p)) pivotList.Add(p);
                    var kvC = (k, p);
                    if (cnt.TryGetValue(kvC, out long exC)) cnt[kvC] = exC + 1;
                    else { map[kvC] = (0, 0); cnt[kvC] = 1; }
                    continue;
                }
                if (!numeric) continue;
                if (keySet.Add(k)) keyList.Add(k);
                if (pivotSet.Add(p)) pivotList.Add(p);
                var kv = (k, p);
                if (map.TryGetValue(kv, out var ex))
                {
                    cnt[kv] = cnt[kv] + 1;
                    map[kv] = Accumulate(agg, ex, v);
                }
                else { map[kv] = (v, 0); cnt[kv] = 1; }
            }
            var keys = keyList; var pivots = pivotList;
            // review 2026-08-29：输出 cell 上限守卫（原仅 CrossJoin 有）。1M 行全异键值 → 1e12 cells OOM。
            const long maxCells = 1_000_000;
            long outCells = (long)(keys.Count + 1) * (pivots.Count + 1);
            if (outCells > maxCells)
                throw new ArgumentException(
                    $"Pivot would produce {keys.Count + 1:N0} rows × {pivots.Count + 1:N0} cols = {outCells:N0} cells. " +
                    $"Maximum is {maxCells:N0}. Reduce distinct key/pivot values.");
            var result = new object[keys.Count + 1, pivots.Count + 1];
            result[0, 0] = "Key \\ Pivot";
            for (int c = 0; c < pivots.Count; c++) result[0, c + 1] = pivots[c];
            for (int r = 0; r < keys.Count; r++) { result[r + 1, 0] = keys[r]; for (int c = 0; c < pivots.Count; c++)
            {
                var kv = (keys[r], pivots[c]);
                result[r + 1, c + 1] = map.TryGetValue(kv, out var cell)
                    ? AggResult(agg, cell.val, cell.comp, cnt[kv])
                    : null;
            } }
            return result;
        }

        internal static object[,] Unpivot(object[,] data, int[] idCols, int[] valueCols, bool hasHeaders = true)
        {
            int cols = data.GetLength(1);
            // review 2026-08-31（深度审查 P2-14）：valueCols 为空数组时原实现静默产出 0 行，
            // 用户无法区分"参数传错"与"无数据"。显式抛错。
            if (valueCols.Length == 0)
                throw new ArgumentException(ErrorMsg.Get("PIVOT_ColumnOutOfRange", cols));
            if (idCols.Any(c => c < 0 || c >= cols) || valueCols.Any(c => c < 0 || c >= cols))
                throw new ArgumentException(ErrorMsg.Get("PIVOT_ColumnOutOfRange", cols));
            int rows = data.GetLength(0); int nId = idCols.Length;
            int dataStartRow = hasHeaders ? 1 : 0;
            if (hasHeaders && rows < 2) throw new ArgumentException(
                "Unpivot requires at least one data row (header + data).");
            if (!hasHeaders && rows < 1) throw new ArgumentException(
                "Unpivot requires at least one data row.");
            // review 2026-08-29：输出 cell 上限守卫（rows × valueCols 无上限会 OOM）
            const long maxCells = 1_000_000;
            int outWidth = nId + 2;
            long estRows = (long)(rows - dataStartRow) * valueCols.Length;
            if (estRows * outWidth > maxCells)
                throw new ArgumentException(
                    $"Unpivot would produce {estRows:N0} rows × {outWidth} cols = {estRows * outWidth:N0} cells. " +
                    $"Maximum is {maxCells:N0}. Reduce input rows or value fields.");
            var result = new List<object[]>();
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
            ValidateAgg(agg);
            int gc = data.GetLength(1);
            if (gCols.Any(c => c < 0 || c >= gc) || aCol < 0 || aCol >= gc)
                throw new ArgumentException(ErrorMsg.Get("PIVOT_ColumnOutOfRange", gc));
            int rows = data.GetLength(0), nG = gCols.Length;
            int startRow = hasHeaders ? 1 : 0;
            var groups = new Dictionary<string, (double val, double comp, long cnt)>();
            var keyNames = new List<string[]>(); var seen = new HashSet<string>();
            for (int r = startRow; r < rows; r++)
            {
                var gk = gCols.Select(c => InputNormalizer.ToString(data[r, c])).ToArray();
                string gks = MakeCompoundKey(gk);
                double v = InputNormalizer.ToDouble(data[r, aCol]);
                bool numeric = !(double.IsNaN(v) || double.IsInfinity(v));
                // review 2026-08-31（深度审查 P1-10）：COUNT 统计行数（含空/文本值行）——
                // 原实现非数值行 continue → 分组少计、纯空值分组整行消失。
                if (agg == "COUNT")
                {
                    if (groups.TryGetValue(gks, out var exC)) groups[gks] = (0, 0, exC.cnt + 1);
                    else { groups[gks] = (0, 0, 1); if (seen.Add(gks)) keyNames.Add(gk); }
                    continue;
                }
                if (!numeric) continue;
                if (groups.TryGetValue(gks, out var ex))
                {
                    if (agg == "SUM" || agg == "AVG")
                    {
                        var n = NeumaierAdd((ex.val, ex.comp), v);
                        groups[gks] = (n.sum, n.comp, ex.cnt + 1);
                    }
                    else
                        groups[gks] = agg == "MAX"
                            ? (Math.Max(ex.val, v), 0, ex.cnt + 1)
                            : (Math.Min(ex.val, v), 0, ex.cnt + 1);
                }
                else { groups[gks] = (v, 0, 1); if (seen.Add(gks)) keyNames.Add(gk); }
            }
            // review 2026-08-29：输出 cell 上限守卫（GroupBy 输出列 = 分组列+1，可被 nG 放大）。
            // 复审修正：守卫必须位于数组分配之前——原实现先 new object[keyNames.Count, nG+1] 再检查，
            // 无法阻止其针对的大分配（如 1M 行 × 100 分组列 ≈ 800MB，32 位可先 OOM）。
            const long maxCells = 1_000_000;
            if ((long)keyNames.Count * (nG + 1) > maxCells)
                throw new ArgumentException(
                    $"GroupBy would produce {keyNames.Count:N0} rows × {nG + 1} cols = {(long)keyNames.Count * (nG + 1):N0} cells. " +
                    $"Maximum is {maxCells:N0}. Reduce group fields or input rows.");
            var result = new object[keyNames.Count, nG + 1];
            for (int i = 0; i < keyNames.Count; i++) { var kn = keyNames[i]; for (int j = 0; j < nG; j++) result[i, j] = kn[j]; var (val, comp, cnt) = groups[MakeCompoundKey(kn)]; result[i, nG] = AggResult(agg, val, comp, cnt); }
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
                    $"Cross join would produce {(long)ra * rb:N0} rows × {ca + cb} cols = {totalCells:N0} cells. " +
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
