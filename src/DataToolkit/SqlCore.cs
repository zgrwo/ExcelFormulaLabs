using System;
using System.Collections.Generic;
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
        /// Accepts SELECT and WITH (CTE) prefixes.</summary>
        private static readonly System.Text.RegularExpressions.Regex SelectOnly =
            new(@"^\s*(?:SELECT|WITH)\s", System.Text.RegularExpressions.RegexOptions.IgnoreCase
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
                | System.Text.RegularExpressions.RegexOptions.Compiled,
                TimeSpan.FromSeconds(5));

        internal static object[,]? SqlQuery(object[,] range, string sql, Dictionary<string, object[,]>? extra = null, bool hasHeaders = true)
        {
            if (!SelectOnly.IsMatch(sql))
                throw new ArgumentException(
                    "Only SELECT statements are allowed for security. " +
                    "Use a dedicated database tool for DDL/DML operations.");
            if (ForbiddenKeyword.IsMatch(sql))
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
            // 防线：① SQLITE_LIMIT_LENGTH=100MB（单值超限由 SQLite 层拒绝，randomblob(1e9) 在
            // 分配前拦截）；② 读取循环行数 + 耗时双上限；③ 直接写入预分配 object[,]（消除 2× 峰值）。
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
                    result[row, i] = reader.IsDBNull(i) ? null : v;
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
                // Scan first N rows to determine the widest type; mixed → TEXT.
                // Limit to MaxScanRows to avoid O(rows×cols) on large tables.
                // NOTE: If first 10 rows are integers but later rows contain text,
                // the column is declared INTEGER. SQLite's dynamic typing (type affinity)
                // still allows storing text in INTEGER columns, but comparisons may
                // behave unexpectedly. For critical data, use explicit TEXT columns.
                const int maxScan = 10;
                int scanEnd = Math.Min(rows, firstDataRow + maxScan);
                bool hasReal = false, hasInt = false;
                for (int r = firstDataRow; r < scanEnd; r++)
                {
                    object v = data[r, c];
                    if (v == null || v is DBNull || InputNormalizer.IsExcelEmptyValue(v) || v is ExcelError) continue;
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
            for (int r = firstDataRow; r < rows; r++) { for (int c = 0; c < cols; c++) { object v = data[r, c]; ins.Parameters[$"@p{c}"].Value = (v == null || v is DBNull || InputNormalizer.IsExcelEmptyValue(v) || v is ExcelError) ? DBNull.Value : v; } ins.ExecuteNonQuery(); }
            tx.Commit();
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
