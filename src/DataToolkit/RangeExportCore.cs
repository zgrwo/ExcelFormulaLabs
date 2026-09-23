using System;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Encodings.Web;
using ExcelFormulaLabs.Foundation;

namespace ExcelFormulaLabs.DataToolkit
{
    /// <summary>Range export to HTML, JSON, Markdown, CSV + row/column operations. Ported from RangeUtils.bas.</summary>
    internal static class RangeExportCore
    {
        // 输出规模守卫与 PivotCore 的 maxCells=1e6 纪律对齐。
        // 整列选择（1,048,576 行 × 多列）会建巨型 StringBuilder（几十 MB～GB 级字符串），
        // 32 位 Excel 冻结 + OOM 不可捕获（ExceptionFilters 排除 OOM）。分配前检查 + long 乘法防回绕。
        private const long MaxExportCells = 1_000_000;
        // 字符预算（R3-14）：1e6 单元格 × 单值上限 32,767 字符 → 理论出口 ~3.3e10 字符。
        // 单元格计数无法约束字符串导出体量，分配 StringBuilder 前线性累加（long 域）。
        private const long MaxExportChars = 50_000_000;

        /// <summary>Guard: (rows × cols) must not exceed MaxExportCells, and the text payload
        /// must not exceed MaxExportChars. Call before any StringBuilder allocation.</summary>
        internal static void GuardExportSize(Array data, string op, long maxChars = MaxExportChars)
        {
            long cells = (long)data.GetLength(0) * data.GetLength(1);
            if (cells > MaxExportCells)
                throw new ArgumentException(
                    $"{op} would export {data.GetLength(0):N0} rows × {data.GetLength(1):N0} cols = {cells:N0} cells. " +
                    $"Maximum is {MaxExportCells:N0}. Reduce the range before exporting.");
            long chars = 0;
            for (int r = 0; r < data.GetLength(0); r++)
                for (int c = 0; c < data.GetLength(1); c++)
                {
                    if (data.GetValue(r, c) is string s) chars += Math.Min(s.Length, 32_767);
                    if (chars > maxChars)
                        throw new ArgumentException(
                            $"{op} text payload exceeds {maxChars:N0} characters " +
                            $"(~{chars:N0} chars in at least {r + 1} rows). Reduce the range or shorten values.");
                }
        }

        internal static string RangeToHtml(object[,] data, bool hasHeaders = true, string? tableClass = null)
        {
            GuardExportSize(data, "RANGE.TOHTML");
            int rows = data.GetLength(0), cols = data.GetLength(1); var sb = new StringBuilder();
            string cls = tableClass != null ? $" class=\"{System.Net.WebUtility.HtmlEncode(tableClass)}\"" : "";
            sb.Append("<table").Append(cls).Append('>');
            for (int r = 0; r < rows; r++) { sb.Append("<tr>"); string tag = (hasHeaders && r == 0) ? "th" : "td"; for (int c = 0; c < cols; c++) sb.Append('<').Append(tag).Append('>').Append(System.Net.WebUtility.HtmlEncode(InputNormalizer.ToString(data[r, c]))).Append("</").Append(tag).Append('>'); sb.Append("</tr>"); }
            sb.Append("</table>"); return sb.ToString();
        }

        internal static string RangeToJson(object[,] data, bool hasHeaders = true, bool pretty = false)
        {
            GuardExportSize(data, "RANGE.TOJSON");
            int rows = data.GetLength(0), cols = data.GetLength(1); var sb = new StringBuilder();
            string nl = pretty ? "\n" : "", sp = pretty ? "  " : ""; sb.Append('[');
            int startRow = hasHeaders ? 1 : 0;
            string[]? headers = null;
            if (hasHeaders && rows > 0)
            {
                // 重复表头会产出重复 JSON 键（解析端后者覆盖前者，数据静默丢失），
                // 按 _2/_3 后缀去重（同 SqlCore 列名规则）。
                headers = new string[cols];
                var usedKeys = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
                for (int c = 0; c < cols; c++)
                {
                    string name = InputNormalizer.ToString(data[0, c]);
                    string candidate = name;
                    for (int dedup = 2; !usedKeys.Add(candidate); dedup++)
                        candidate = name + "_" + dedup;
                    headers[c] = candidate;
                }
            }
            for (int r = startRow; r < rows; r++) { if (r > startRow) sb.Append(',').Append(nl); sb.Append(sp).Append('{'); for (int c = 0; c < cols; c++) { if (c > 0) sb.Append(',').Append(' '); string key = headers != null ? $"\"{JsonEncodedText.Encode(headers[c], JavaScriptEncoder.Default).Value}\"" : $"\"Col{c + 1}\""; sb.Append(key).Append(": "); sb.Append(JsonVal(data[r, c])); } sb.Append('}'); }
            sb.Append(nl).Append(']'); return sb.ToString();
        }

        internal static string RangeToMarkdown(object[,] data, bool hasHeaders = true)
        {
            GuardExportSize(data, "RANGE.TOMD");
            int rows = data.GetLength(0), cols = data.GetLength(1); if (rows == 0) return ""; var sb = new StringBuilder();
            for (int c = 0; c < cols; c++) { if (c > 0) sb.Append(" | "); sb.Append(EscapeMarkdownCell(hasHeaders ? InputNormalizer.ToString(data[0, c]) : $"Col{c + 1}")); } sb.AppendLine();
            for (int c = 0; c < cols; c++) { if (c > 0) sb.Append(" | "); sb.Append("---"); } sb.AppendLine();
            for (int r = hasHeaders ? 1 : 0; r < rows; r++) { for (int c = 0; c < cols; c++) { if (c > 0) sb.Append(" | "); sb.Append(EscapeMarkdownCell(InputNormalizer.ToString(data[r, c]))); } sb.AppendLine(); }
            return sb.ToString();
        }

        /// <summary>Escape pipe characters in cell values to prevent Markdown table breakage.</summary>
        private static string EscapeMarkdownCell(string v)
        {
            if (string.IsNullOrEmpty(v)) return v;
            // Backslash-escape pipe characters which would break the table column layout.
            // Newlines are replaced with spaces to stay within one table row.
            return v.Replace("\\", "\\\\").Replace("|", "\\|").Replace("\r\n", " ").Replace("\n", " ");
        }

        /// <param name="hasHeaders">Deprecated: has no effect on CSV output (CSV treats all rows as data).
        /// Kept for API compatibility with RangeToJson / RangeToMarkdown / RangeToHtml.</param>
        /// <remarks>quote=true（默认）→ **所有字段**加引号（符合手册/Description 的语义）；
        /// quote=false → 仅按 RFC 4180 最小引号（含 delimiter/引号/CR/LF 时转义；CR 不可漏引号，
        /// TSV 的 tab/换行须被处理）。公式注入 defang 对两种模式都生效，BOM 前缀与 +/- 对称。</remarks>
        internal static string RangeToCsv(object[,] data, string delim = ",", bool quote = true, bool hasHeaders = true)
        {
            GuardExportSize(data, "RANGE.TOCSV");
            int rows = data.GetLength(0), cols = data.GetLength(1); var sb = new StringBuilder(); int startRow = 0;
            for (int r = startRow; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    if (c > 0) sb.Append(delim);
                    string v = DefangFormulaInjection(InputNormalizer.ToString(data[r, c]));
                    if (quote || v.Contains(delim) || v.Contains("\"") || v.Contains('\r') || v.Contains('\n'))
                        v = "\"" + v.Replace("\"", "\"\"") + "\"";
                    sb.Append(v);
                }
                sb.AppendLine();
            }
            return sb.ToString();
        }

        /// <summary>CSV formula-injection defang. 检查原始首字符（TrimStart 会吃掉 Tab/CR）：
        /// 以 = @，或非数值的 +/−，或 Tab/CR 开头的值前置单引号。BOM 前缀不参与首字符判定
        /// （U+FEFF=... 会绕过 defang）；+/− 走同一数值判定（+42 与 −42 对称）。</summary>
        private static string DefangFormulaInjection(string v)
        {
            if (string.IsNullOrEmpty(v)) return v;
            int start = v[0] == '\uFEFF' ? 1 : 0;
            if (start >= v.Length) return v;
            bool tabCrPrefix = v[start] == '\t' || v[start] == '\r';
            string trimmed = v.Substring(start).TrimStart();
            if (trimmed.Length == 0) return v;
            char first = trimmed[0];
            // P3-4：显式排除 ±Infinity/NaN——net8 的 double.TryParse("+Infinity") 返回 true，
            // net48 返回 false，同一输入的 defang 结果双 TFM 分裂（注入面按 net48 留空）。
            bool signedNumeric = (first == '+' || first == '-') && trimmed.Length > 1
                && double.TryParse(trimmed, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out double parsed)
                && !double.IsNaN(parsed) && !double.IsInfinity(parsed);
            bool defang = tabCrPrefix || first == '=' || first == '@'
                || ((first == '+' || first == '-') && !signedNumeric);
            return defang ? "'" + v : v;
        }

        internal static object[,] Transpose(object[,] d) { int r = d.GetLength(0), c = d.GetLength(1); var t = new object[c, r]; for (int i = 0; i < r; i++) for (int j = 0; j < c; j++) t[j, i] = d[i, j]; return t; }
        internal static object[,] SelectColumns(object[,] d, int[] ci)
        {
            int cols = d.GetLength(1);
            if (ci.Any(j => j < 0 || j >= cols))
                throw new ArgumentException(ErrorMsg.Get("RANGE_ColumnOutOfRange", cols - 1));
            int r = d.GetLength(0);
            var t = new object[r, ci.Length];
            for (int i = 0; i < r; i++)
                for (int j = 0; j < ci.Length; j++)
                    t[i, j] = d[i, ci[j]];
            return t;
        }
        internal static object[,] SelectRows(object[,] d, int[] ri)
        {
            int rows = d.GetLength(0);
            if (ri.Any(j => j < 0 || j >= rows))
                throw new ArgumentException(ErrorMsg.Get("RANGE_RowOutOfRange", rows - 1));
            int c = d.GetLength(1);
            var t = new object[ri.Length, c];
            for (int i = 0; i < ri.Length; i++)
                for (int j = 0; j < c; j++)
                    t[i, j] = d[ri[i], j];
            return t;
        }

        private static string JsonVal(object? v)
        {
            if (v == null || v is DBNull) return "null";
            if (InputNormalizer.IsExcelEmptyValue(v)) return "null";
            if (v is string s) return $"\"{JsonEncodedText.Encode(s, JavaScriptEncoder.Default).Value}\"";
            if (v is bool b) return b ? "true" : "false";
            if (v is double d && (double.IsNaN(d) || double.IsInfinity(d))) return "null";
            // "R" 最短往返格式（等价 Python repr/JSON 默认）：G17 会输出 0.10000000000000001 等噪声；
            // float 须有非有限守卫（float NaN/Inf 走 f.ToString 会产出非法 JSON）。
            // net48 的 "R" 有已知不往返缺陷（如 2.2250738585072011e-308）→ 该 TFM 用 G17，
            // 保证 JSON 输出可被 JSON.parse 无损读回（R2-13）。
#if NET48
            if (v is double fd)
            {
                // net48 的 "R" 有已知不往返缺陷（如 2.2250738585072011e-308）→ 先试 R，
                // 仅当解析回同一位模式（往返验证）时采用最短形式，否则退 G17 保证无损。
                var inv = System.Globalization.CultureInfo.InvariantCulture;
                string rs = fd.ToString("R", inv);
                if (double.TryParse(rs, System.Globalization.NumberStyles.Float, inv, out double back)
                    && System.BitConverter.DoubleToInt64Bits(back) == System.BitConverter.DoubleToInt64Bits(fd))
                    return rs;
                return fd.ToString("G17", inv);
            }
#else
            if (v is double fd) return fd.ToString("R", System.Globalization.CultureInfo.InvariantCulture);
#endif
            // long/int 分支必须 Invariant（R3-13）：sv-SE 等文化用 U+2212 负号 → 输出 JSON
            // 自身 JSON.VALIDATE=false；紧邻 double/decimal 分支均已 Invariant。
            if (v is long l) return l.ToString(System.Globalization.CultureInfo.InvariantCulture);
            if (v is int i) return i.ToString(System.Globalization.CultureInfo.InvariantCulture);
            if (v is float f)
                return float.IsNaN(f) || float.IsInfinity(f)
                    ? "null"
                    : f.ToString("R", System.Globalization.CultureInfo.InvariantCulture);
            if (v is decimal m) return m.ToString(System.Globalization.CultureInfo.InvariantCulture);
            return $"\"{JsonEncodedText.Encode(InputNormalizer.ToString(v), JavaScriptEncoder.Default).Value}\"";
        }
    }
}
