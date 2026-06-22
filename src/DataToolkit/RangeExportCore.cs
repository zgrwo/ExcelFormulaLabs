using System;
using System.Text;
using System.Text.Json;
using System.Text.Encodings.Web;
using ExcelVbaLibraries.Foundation;

namespace ExcelVbaLibraries.DataToolkit
{
    /// <summary>Range export to HTML, JSON, Markdown, CSV + row/column operations. Ported from RangeUtils.bas.</summary>
    internal static class RangeExportCore
    {
        internal static string RangeToHtml(object[,] data, bool hasHeader = true, string? tableClass = null)
        {
            int rows = data.GetLength(0), cols = data.GetLength(1); var sb = new StringBuilder();
            string cls = tableClass != null ? $" class=\"{tableClass}\"" : "";
            sb.Append($"<table{cls}>");
            for (int r = 0; r < rows; r++) { sb.Append("<tr>"); string tag = (hasHeader && r == 0) ? "th" : "td"; for (int c = 0; c < cols; c++) sb.Append('<').Append(tag).Append('>').Append(System.Net.WebUtility.HtmlEncode(InputNormalizer.ToString(data[r, c]))).Append("</").Append(tag).Append('>'); sb.Append("</tr>"); }
            sb.Append("</table>"); return sb.ToString();
        }

        internal static string RangeToJson(object[,] data, bool hasHeader = true, bool pretty = false)
        {
            int rows = data.GetLength(0), cols = data.GetLength(1); var sb = new StringBuilder();
            string nl = pretty ? "\n" : "", sp = pretty ? "  " : ""; sb.Append('[');
            int startRow = hasHeader ? 1 : 0;
            string[]? headers = null;
            if (hasHeader && rows > 0) { headers = new string[cols]; for (int c = 0; c < cols; c++) headers[c] = InputNormalizer.ToString(data[0, c]); }
            for (int r = startRow; r < rows; r++) { if (r > startRow) sb.Append(',').Append(nl); sb.Append(sp).Append('{'); for (int c = 0; c < cols; c++) { if (c > 0) sb.Append(',').Append(' '); string key = headers != null ? $"\"{JsonEncodedText.Encode(headers[c], JavaScriptEncoder.UnsafeRelaxedJsonEscaping).Value}\"" : $"\"col{c}\""; sb.Append(key).Append(": "); sb.Append(JsonVal(data[r, c])); } sb.Append('}'); }
            sb.Append(nl).Append(']'); return sb.ToString();
        }

        internal static string RangeToMarkdown(object[,] data, bool hasHeader = true)
        {
            int rows = data.GetLength(0), cols = data.GetLength(1); if (rows == 0) return ""; var sb = new StringBuilder();
            for (int c = 0; c < cols; c++) { if (c > 0) sb.Append(" | "); sb.Append(hasHeader ? InputNormalizer.ToString(data[0, c]) : $"Col{c + 1}"); } sb.AppendLine();
            for (int c = 0; c < cols; c++) { if (c > 0) sb.Append(" | "); sb.Append("---"); } sb.AppendLine();
            for (int r = hasHeader ? 1 : 0; r < rows; r++) { for (int c = 0; c < cols; c++) { if (c > 0) sb.Append(" | "); sb.Append(InputNormalizer.ToString(data[r, c])); } sb.AppendLine(); }
            return sb.ToString();
        }

        internal static string RangeToCsv(object[,] data, string delim = ",", bool quote = true)
        { int rows = data.GetLength(0), cols = data.GetLength(1); var sb = new StringBuilder(); for (int r = 0; r < rows; r++) { for (int c = 0; c < cols; c++) { if (c > 0) sb.Append(delim); string v = InputNormalizer.ToString(data[r, c]); if (quote && (v.Contains(delim) || v.Contains("\"") || v.Contains("\n"))) v = "\"" + v.Replace("\"", "\"\"") + "\""; sb.Append(v); } sb.AppendLine(); } return sb.ToString(); }

        internal static object[,] Transpose(object[,] d) { int r = d.GetLength(0), c = d.GetLength(1); var t = new object[c, r]; for (int i = 0; i < r; i++) for (int j = 0; j < c; j++) t[j, i] = d[i, j]; return t; }
        internal static object[,] SelectColumns(object[,] d, int[] ci) { int r = d.GetLength(0); var t = new object[r, ci.Length]; for (int i = 0; i < r; i++) for (int j = 0; j < ci.Length; j++) t[i, j] = d[i, ci[j]]; return t; }
        internal static object[,] SelectRows(object[,] d, int[] ri) { int c = d.GetLength(1); var t = new object[ri.Length, c]; for (int i = 0; i < ri.Length; i++) for (int j = 0; j < c; j++) t[i, j] = d[ri[i], j]; return t; }

        private static string JsonVal(object? v) { if (v == null || v is DBNull) return "null"; if (ReferenceEquals(v, ExcelEmpty.Value)) return "null"; if (v is string s) return $"\"{JsonEncodedText.Encode(s, JavaScriptEncoder.UnsafeRelaxedJsonEscaping).Value}\""; if (v is bool b) return b ? "true" : "false"; if (v is double d && double.IsNaN(d)) return "null"; if (v is long l) return l.ToString(); if (v is int i) return i.ToString(); return $"\"{JsonEncodedText.Encode(InputNormalizer.ToString(v), JavaScriptEncoder.UnsafeRelaxedJsonEscaping).Value}\""; }
    }
}
