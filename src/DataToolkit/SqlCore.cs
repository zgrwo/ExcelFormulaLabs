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
        /// form ("REPLACE INTO ...") so the scalar function REPLACE(X,Y,Z) stays usable.</summary>
        private static readonly System.Text.RegularExpressions.Regex ForbiddenKeyword =
            new(@"\b(INSERT|UPDATE|DELETE|REPLACE\s+INTO|ATTACH|DETACH|PRAGMA|DROP|CREATE|ALTER|VACUUM|REINDEX)\b",
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
                    "Data-modifying or schema statements (INSERT/UPDATE/DELETE/DDL/PRAGMA/ATTACH) " +
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
            int cols = reader.FieldCount; var rows = new List<object[]>(); var hdr = new object[cols];
            for (int i = 0; i < cols; i++) hdr[i] = reader.GetName(i); rows.Add(hdr);
            while (reader.Read()) { var row = new object[cols]; for (int i = 0; i < cols; i++) row[i] = reader.IsDBNull(i) ? ExcelEmpty.Value : reader.GetValue(i); rows.Add(row); }
            var result = new object[rows.Count, cols]; for (int r = 0; r < rows.Count; r++) for (int c = 0; c < cols; c++) result[r, c] = rows[r][c]; return result;
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
