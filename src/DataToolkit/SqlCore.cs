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

        internal static object[,]? SqlQuery(object[,] range, string sql, Dictionary<string, object[,]>? extra = null)
        {
            using var conn = new SqlConn("Data Source=:memory:");
            conn.Open();
            CreateTable(conn, "data", range);
            if (extra != null) foreach (var kv in extra) CreateTable(conn, kv.Key, kv.Value);
            using var cmd = conn.CreateCommand(); cmd.CommandText = sql; cmd.CommandTimeout = SqlTimeoutSeconds;
            using var reader = cmd.ExecuteReader();
            int cols = reader.FieldCount; var rows = new List<object[]>(); var hdr = new object[cols];
            for (int i = 0; i < cols; i++) hdr[i] = reader.GetName(i); rows.Add(hdr);
            while (reader.Read()) { var row = new object[cols]; for (int i = 0; i < cols; i++) row[i] = reader.IsDBNull(i) ? ExcelEmpty.Value : reader.GetValue(i); rows.Add(row); }
            var result = new object[rows.Count, cols]; for (int r = 0; r < rows.Count; r++) for (int c = 0; c < cols; c++) result[r, c] = rows[r][c]; return result;
        }

        private static void CreateTable(SqlConn conn, string name, object[,] data)
        {
            int rows = data.GetLength(0), cols = data.GetLength(1); if (rows == 0) return;
            name = Sanitize(name, 0);  // table name gets the same sanitisation as column names
            var names = new string[cols]; var types = new string[cols];
            var usedNames = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int c = 0; c < cols; c++)
            {
                string raw = InputNormalizer.ToString(data[0, c]); string baseName = Sanitize(raw, c);
                // De-duplicate: append _2, _3... if sanitised names collide
                string colName = baseName;
                for (int dedup = 2; !usedNames.Add(colName); dedup++)
                    colName = baseName + "_" + dedup;
                names[c] = colName;
                // Scan all data rows to determine the widest type; mixed → TEXT
                bool hasReal = false, hasInt = false;
                for (int r = 1; r < rows; r++)
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
            for (int r = 1; r < rows; r++) { for (int c = 0; c < cols; c++) { object v = data[r, c]; ins.Parameters[$"@p{c}"].Value = (v == null || v is DBNull || InputNormalizer.IsExcelEmptyValue(v) || v is ExcelError) ? DBNull.Value : v; } ins.ExecuteNonQuery(); }
            tx.Commit();
        }

        private static string Sanitize(string raw, int idx)
        { if (string.IsNullOrWhiteSpace(raw)) return $"Col{idx + 1}"; var ca = raw.ToCharArray(); for (int i = 0; i < ca.Length; i++) if (!char.IsLetterOrDigit(ca[i]) && ca[i] != '_') ca[i] = '_'; string n = new(ca); if (char.IsDigit(n[0])) n = "_" + n; return n; }
    }
}
