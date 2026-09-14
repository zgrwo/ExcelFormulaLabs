using System;
using System.Collections.Generic;
using System.Globalization;
#if NET48
using System.Data.SQLite;
using SqlConn = System.Data.SQLite.SQLiteConnection;
using SqlParam = System.Data.SQLite.SQLiteParameter;
#else
using Microsoft.Data.Sqlite;
using SqlConn = Microsoft.Data.Sqlite.SqliteConnection;
using SqlParam = Microsoft.Data.Sqlite.SqliteParameter;
#endif
using ExcelFormulaLabs.Foundation;

namespace ExcelFormulaLabs.DataToolkit
{
    internal static class SqlCore
    {
        private const int SqlTimeoutSeconds = 30;

        /// <summary>Quick check that a SQL statement is read-only.
        /// Rejects DDL (CREATE/ALTER/DROP), DML (INSERT/UPDATE/DELETE),
        /// ATTACH/DETACH, and PRAGMA for safety in shared-workbook scenarios.
        /// Accepts SELECT and WITH (CTE) prefixes, optionally preceded by
        /// whitespace and comments; no whitespace is required after the keyword
        /// (native SQLite accepts "SELECT*FROM data").</summary>
        // review 2026-09-05（N10）：SelectOnly/ForbiddenKeyword 补 RegexOptions.CultureInvariant，
        // 对齐 RegexCore.cs 的强制风格——IgnoreCulture 下 IgnoreCase 依赖 CurrentCulture，
        // 土耳其语 locale（i→İ）会让小写 "insert" 无法命中 INSERT 黑名单，安全检查失效。
        // review 2026-09-14（SQL 审计 P3）：`\s` → `\b`——原正则拒绝合法写法 SELECT*FROM data；
        // 前导注释由 StripLeadingComments 剥离后再匹配（黑名单/分号检查仍扫原文）。
        private static readonly System.Text.RegularExpressions.Regex SelectOnly =
            new(@"^\s*(?:SELECT|WITH)\b", System.Text.RegularExpressions.RegexOptions.IgnoreCase
                | System.Text.RegularExpressions.RegexOptions.CultureInvariant
                | System.Text.RegularExpressions.RegexOptions.Compiled,
                TimeSpan.FromSeconds(5));

        /// <summary>Blacklist of statement-modifying keywords anywhere in the query.
        /// SQLite allows data-modifying CTEs (e.g. "WITH x AS (SELECT 1) DELETE FROM data"),
        /// which would pass the <see cref="SelectOnly"/> prefix check — this second
        /// scan closes that bypass. REPLACE is matched only in its DML statement
        /// form ("REPLACE INTO ...") so the scalar function REPLACE(X,Y,Z) stays usable.
        /// review 2026-08-31（深度审查 P0-2）：新增 \bRECURSIVE\b——无限递归 CTE
        /// （WITH RECURSIVE x(n) AS (SELECT 1 UNION ALL SELECT n+1 FROM x)）可通过
        /// 前缀检查且不被任何关键字拦截，输出无上界 → OOM。递归 CTE 在本功能语境
        /// （内存表只读查询）没有任何正当用途。</summary>
        private static readonly System.Text.RegularExpressions.Regex ForbiddenKeyword =
            new(@"\b(INSERT|UPDATE|DELETE|REPLACE\s+INTO|ATTACH|DETACH|PRAGMA|DROP|CREATE|ALTER|VACUUM|REINDEX|RECURSIVE)\b",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase
                | System.Text.RegularExpressions.RegexOptions.CultureInvariant
                | System.Text.RegularExpressions.RegexOptions.Compiled,
                TimeSpan.FromSeconds(5));

        internal static object[,]? SqlQuery(object[,] range, string sql, Dictionary<string, object[,]>? extra = null, bool hasHeaders = true)
        {
            // review 2026-09-14（模块审查 P0 SEC-01）：黑名单原扫原文，SQL 注释可拆分关键字
            // （REPLACE/**/INTO、REPLACE--x\nINTO）使其不被正则匹配而实际执行 DML，绕过
            // 行数/耗时预算 → 不可捕获 OOM。所有结构检查（前缀/黑名单）统一扫
            // StripSqlComments 归一化文本（注释替换为空格，保留字符串字面量与引号标识符）。
            if (!TryStripSqlComments(sql, out string normalized))
                throw new ArgumentException(
                    "SQL query contains an unterminated block comment and cannot be safely validated.");
            if (!SelectOnly.IsMatch(normalized))
                throw new ArgumentException(
                    "Only SELECT statements are allowed for security. " +
                    "Use a dedicated database tool for DDL/DML operations.");
            if (ForbiddenKeyword.IsMatch(normalized))
                throw new ArgumentException(
                    "Data-modifying or schema statements (INSERT/UPDATE/DELETE/RECURSIVE/DDL/PRAGMA/ATTACH) " +
                    "are forbidden inside SQL queries, including WITH (CTE) prefixes.");
            // Reject semicolons to prevent multi-statement injection
            // (e.g. SELECT 1; ATTACH DATABASE …).  SQLite single-statement
            // execution doesn't require a terminating semicolon, and the
            // rare case of semicolons inside string literals is not a
            // realistic Excel-formula scenario.
            if (sql.IndexOf(';') >= 0)
                throw new ArgumentException(
                    "Semicolons are not allowed in SQL queries for security. " +
                    "Multi-statement queries are blocked to prevent data exfiltration.");
            using var conn = new SqlConn("Data Source=:memory:");
            conn.Open();
            CreateTable(conn, "data", range, hasHeaders);
            if (extra != null) foreach (var kv in extra) CreateTable(conn, kv.Key, kv.Value, hasHeaders);
            using var cmd = conn.CreateCommand(); cmd.CommandText = sql; cmd.CommandTimeout = SqlTimeoutSeconds;
            using var reader = cmd.ExecuteReader();
            // review 2026-08-31（深度审查 P0-2）：原实现 `rows.Add(row)` 无上界 + 末尾整体复制
            // （峰值 2× 内存）——`SELECT * FROM a, b`（100k 行 → 1e10 行）等失控查询触发
            // **不可捕获的 OOM → Excel 进程崩溃**（ExceptionFilters 排除 OOM，WrapError 不兜底）。
            // 防线：① 读取循环行数 + 耗时双上限；② 直接写入预分配 object[,]（消除 2× 峰值）。
            // review 2026-09-05（R11）：原此处声称「SQLITE_LIMIT_LENGTH=100MB，randomblob 在
            // 分配前拦截」与事实不符——全库 0 次 SetLimit，SQLite 默认 SQLITE_MAX_LENGTH=1e9，
            // randomblob(5e8) 先整体分配 500MB 才被下方事后 10MB 检查拒绝。当前防线 = 事后拒绝，
            // 分配峰值窗口为已知残余（与下方 blob 检查处注释口径一致，消除自相矛盾）。
            int cols = reader.FieldCount;
            const int maxRows = 200_000;
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var result = new object[Math.Min(1024, maxRows + 1), cols];
            for (int i = 0; i < cols; i++) result[0, i] = reader.GetName(i);
            int row = 1;
            while (reader.Read())
            {
                if (row >= result.GetLength(0))
                {
                    if (row >= maxRows)
                        throw new ArgumentException(
                            $"SQL query returned more than {maxRows:N0} rows — possible runaway query " +
                            "(cross join / recursive CTE). Narrow the query with WHERE/LIMIT.");
                    var bigger = new object[Math.Min(result.GetLength(0) * 2, maxRows + 1), cols];
                    Array.Copy(result, bigger, result.Length);
                    result = bigger;
                }
                if (sw.ElapsedMilliseconds > 5000)
                    throw new ArgumentException(
                        "SQL query exceeded the 5-second execution budget — possible runaway query. " +
                        "Narrow the query with WHERE/LIMIT.");
                for (int i = 0; i < cols; i++)
                {
                    object v = reader.GetValue(i);
                    // review 2026-08-31（深度审查 P0-2 补充）：单值巨型 blob（如
                    // SELECT randomblob(1000000000)）由 SQLite 层整体分配后才到达此处——
                    // 无法在分配前拦截，但对已读出的超大值立即拒绝，防止其进入结果数组
                    // （32 位 Excel 单值 >2GB 时 GetValue 本身仍可能 OOM，属已知残余，见文档）。
                    if (v is byte[] blob && blob.Length > 10_000_000)
                        throw new ArgumentException(
                            $"SQL query returned a {blob.Length:N0}-byte blob at row {row}, column {i} — " +
                            "possible runaway query (randomblob). Limit blobs to 10 MB.");
                    // review 2026-09-14（模块审查 P2 SEC-02）：单值上限原只查 byte[]，
                    // hex(randomblob(11e6)) 等文本型巨值（22,000,000 字符）不受限——
                    // 与 blob 共用 10MB 单值预算（32 位 Excel 单元格无法承载）。
                    if (v is string hugeText && hugeText.Length > 10_000_000)
                        throw new ArgumentException(
                            $"SQL query returned a {hugeText.Length:N0}-character text value at row {row}, column {i} — " +
                            "possible runaway query (hex/quote/CAST on randomblob). Limit values to 10 MB.");
                    // review 2026-09-05（R10）：null =「空单元格」哨兵（DBNull→null，与 Excel 空单元格
                    // 语义一致），null! 豁免的是可空性分析而非断言运行时非空（经 warnlab 实证：
                    // object?[,] 本地数组方案会在返回处触发 CS8619，不可用）。
                    result[row, i] = reader.IsDBNull(i) ? null! : v;
                }
                row++;
            }
            if (row < result.GetLength(0))
            {
                var trimmed = new object[row, cols];
                for (int r = 0; r < row; r++) for (int c = 0; c < cols; c++) trimmed[r, c] = result[r, c];
                return trimmed;
            }
            return result;
        }

        private static void CreateTable(SqlConn conn, string name, object[,] data, bool hasHeaders = true)
        {
            int rows = data.GetLength(0), cols = data.GetLength(1); if (rows == 0) return;
            name = Sanitize(name, 0);  // table name gets the same sanitisation as column names
            int firstDataRow = hasHeaders ? 1 : 0;   // header contract: row 0 = column names when hasHeaders
            var names = new string[cols]; var types = new string[cols];
            var usedNames = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int c = 0; c < cols; c++)
            {
                string raw = hasHeaders ? InputNormalizer.ToString(data[0, c]) : ""; string baseName = Sanitize(raw, c);
                // De-duplicate: append _2, _3... if sanitised names collide
                string colName = baseName;
                for (int dedup = 2; !usedNames.Add(colName); dedup++)
                    colName = baseName + "_" + dedup;
                names[c] = colName;
                // Scan EVERY data row to determine the widest type; mixed → TEXT.
                // review 2026-09-14（SQL 审计 P2）：原限前 10 行（MaxScanRows）——
                // 窗口外文本落入数值列时 net48 参数转换抛 FormatException（整条查询
                // #VALUE!），net8 则把文本静默存进 REAL 列污染 SUM/比较。插入循环
                // 本就是 O(rows×cols)，全表扫描只增加一个同阶常数因子，不引入新的
                // 复杂度上界（输入行数受 Excel 区域规模约束）。
                bool hasReal = false, hasInt = false;
                for (int r = firstDataRow; r < rows; r++)
                {
                    object v = data[r, c];
                    // review 2026-09-14（SQL 审计 P1）：真实 Excel 错误单元格由封送层
                    // 提供，类型全名与 Foundation.ExcelError 不同——必须走
                    // IsExcelErrorValue 才按空值跳过（Core 层零 Excel 依赖，只能按名识别）。
                    if (v == null || v is DBNull || InputNormalizer.IsExcelEmptyValue(v) || InputNormalizer.IsExcelErrorValue(v)) continue;
                    if (v is double or float) hasReal = true;
                    else if (v is int or long) hasInt = true;
                    else { hasReal = false; hasInt = false; break; }  // non-numeric → TEXT
                }
                types[c] = hasReal ? "REAL" : hasInt ? "INTEGER" : "TEXT";
            }
            var parts = new string[cols]; for (int c = 0; c < cols; c++) parts[c] = $"\"{names[c]}\" {types[c]}";
            using var create = conn.CreateCommand(); create.CommandText = $"CREATE TABLE \"{name}\" ({string.Join(",", parts)})"; create.CommandTimeout = SqlTimeoutSeconds; create.ExecuteNonQuery();
            using var tx = conn.BeginTransaction();
            var ph = new string[cols]; for (int c = 0; c < cols; c++) ph[c] = $"@p{c}";
            using var ins = conn.CreateCommand(); ins.CommandText = $"INSERT INTO \"{name}\" VALUES ({string.Join(",", ph)})"; ins.CommandTimeout = SqlTimeoutSeconds;
            for (int c = 0; c < cols; c++) ins.Parameters.Add(new SqlParam($"@p{c}", types[c] == "INTEGER" ? System.Data.DbType.Int64 : types[c] == "REAL" ? System.Data.DbType.Double : System.Data.DbType.String));
            for (int r = firstDataRow; r < rows; r++) { for (int c = 0; c < cols; c++) ins.Parameters[$"@p{c}"].Value = NormalizeForSql(data[r, c], types[c]); ins.ExecuteNonQuery(); }
            tx.Commit();
        }

        /// <summary>Map an Excel cell to the SQLite value bound for its column.
        /// null/DBNull/ExcelEmpty/ExcelError → DBNull (empty cell).
        /// TEXT columns canonicalise non-string values (bool → TRUE/FALSE,
        /// DateTime → invariant "yyyy-MM-dd HH:mm:ss", other IConvertible →
        /// invariant string) so that System.Data.SQLite (net48) and
        /// Microsoft.Data.Sqlite (net8) store identical values instead of
        /// provider/culture-specific conversions.</summary>
        private static object NormalizeForSql(object? v, string columnType)
        {
            if (v == null || v is DBNull || InputNormalizer.IsExcelEmptyValue(v) || InputNormalizer.IsExcelErrorValue(v))
                return DBNull.Value;
            if (columnType == "TEXT")
            {
                if (v is bool b) return b ? "TRUE" : "FALSE";
                if (v is DateTime dt) return dt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                return v is string s ? s : InputNormalizer.ToString(v);
            }
            return v;
        }

        /// <summary>Remove SQL comments (<c>--</c> to end of line and <c>/* ... */</c>)
        /// while preserving string literals (<c>'...'</c> with <c>''</c> escapes) and
        /// quoted identifiers (<c>"..."</c>, <c>`...`</c>, <c>[...]</c>). Comments are
        /// replaced by a single space so keywords split by comments
        /// (e.g. <c>REPLACE/**/INTO</c>) are re-joined as separate tokens and become
        /// visible to the keyword blacklist.
        /// Returns false when a block comment is unterminated — the caller must reject
        /// the statement (SQLite would also fail, but this is an explicit fail-closed path).</summary>
        private static bool TryStripSqlComments(string sql, out string normalized)
        {
            var sb = new System.Text.StringBuilder(sql.Length);
            int i = 0;
            while (i < sql.Length)
            {
                char ch = sql[i];
                if (ch == '\'') // string literal — copy verbatim, support '' escape
                {
                    sb.Append(ch); i++;
                    while (i < sql.Length)
                    {
                        if (sql[i] == '\'')
                        {
                            if (i + 1 < sql.Length && sql[i + 1] == '\'') { sb.Append("''"); i += 2; continue; }
                            sb.Append('\''); i++; break;
                        }
                        sb.Append(sql[i]); i++;
                    }
                    continue;
                }
                if (ch == '"' || ch == '`') // quoted identifier — copy verbatim, doubled quote escapes
                {
                    char quote = ch;
                    sb.Append(ch); i++;
                    while (i < sql.Length)
                    {
                        if (sql[i] == quote)
                        {
                            if (i + 1 < sql.Length && sql[i + 1] == quote) { sb.Append(quote); sb.Append(quote); i += 2; continue; }
                            sb.Append(quote); i++; break;
                        }
                        sb.Append(sql[i]); i++;
                    }
                    continue;
                }
                if (ch == '[') // bracket identifier — no escape inside
                {
                    int end = sql.IndexOf(']', i + 1);
                    if (end < 0) { sb.Append(sql, i, sql.Length - i); break; }
                    sb.Append(sql, i, end - i + 1); i = end + 1;
                    continue;
                }
                if (ch == '-' && i + 1 < sql.Length && sql[i + 1] == '-') // line comment
                {
                    int nl = sql.IndexOf('\n', i + 2);
                    sb.Append(' ');
                    if (nl < 0) break;
                    i = nl + 1;
                    continue;
                }
                if (ch == '/' && i + 1 < sql.Length && sql[i + 1] == '*') // block comment
                {
                    int end = sql.IndexOf("*/", i + 2, StringComparison.Ordinal);
                    if (end < 0) { normalized = string.Empty; return false; }
                    sb.Append(' ');
                    i = end + 2;
                    continue;
                }
                sb.Append(ch); i++;
            }
            normalized = sb.ToString();
            return true;
        }

        private static string Sanitize(string raw, int idx)
        {
            if (string.IsNullOrWhiteSpace(raw)) return $"Col{idx + 1}";
            var ca = raw.ToCharArray();
            for (int i = 0; i < ca.Length; i++)
                if (!char.IsLetterOrDigit(ca[i]) && ca[i] != '_')
                    ca[i] = '_';
            string n = new(ca);
            if (char.IsDigit(n[0])) n = "_" + n;
            return n;
        }
    }
}
