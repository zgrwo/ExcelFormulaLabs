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
using ExcelVbaLibraries.Foundation;

namespace ExcelVbaLibraries.DataToolkit
{
    internal static class SqlCore
    {
        internal static object[,]? SqlQuery(object[,] range, string sql, Dictionary<string, object[,]>? extra = null)
        {
            try
            {
                using var conn = new SqlConn("Data Source=:memory:");
                conn.Open();
                CreateTable(conn, "data", range);
                if (extra != null) foreach (var kv in extra) CreateTable(conn, kv.Key, kv.Value);
                using var cmd = conn.CreateCommand(); cmd.CommandText = sql;
                using var reader = cmd.ExecuteReader();
                int cols = reader.FieldCount; var rows = new List<object[]>(); var hdr = new object[cols];
                for (int i = 0; i < cols; i++) hdr[i] = reader.GetName(i); rows.Add(hdr);
                while (reader.Read()) { var row = new object[cols]; for (int i = 0; i < cols; i++) row[i] = reader.IsDBNull(i) ? ExcelEmpty.Value : reader.GetValue(i); rows.Add(row); }
                var result = new object[rows.Count, cols]; for (int r = 0; r < rows.Count; r++) for (int c = 0; c < cols; c++) result[r, c] = rows[r][c]; return result;
            }
            catch { return null; }
        }

        private static void CreateTable(SqlConn conn, string name, object[,] data)
        {
            int rows = data.GetLength(0), cols = data.GetLength(1); if (rows == 0) return;
            var names = new string[cols]; var types = new string[cols];
            for (int c = 0; c < cols; c++)
            {
                string raw = InputNormalizer.ToString(data[0, c]); names[c] = Sanitize(raw, c);
                types[c] = (rows > 1 && data[1, c] is double or int or long or float) ? "REAL" :
                    (rows > 1 && data[1, c] is long or int) ? "INTEGER" : "TEXT";
            }
            var parts = new string[cols]; for (int c = 0; c < cols; c++) parts[c] = $"\"{names[c]}\" {types[c]}";
            using var create = conn.CreateCommand(); create.CommandText = $"CREATE TABLE \"{name}\" ({string.Join(",", parts)})"; create.ExecuteNonQuery();
            using var tx = conn.BeginTransaction();
            var ph = new string[cols]; for (int c = 0; c < cols; c++) ph[c] = $"@p{c}";
            using var ins = conn.CreateCommand(); ins.CommandText = $"INSERT INTO \"{name}\" VALUES ({string.Join(",", ph)})";
            for (int c = 0; c < cols; c++) ins.Parameters.Add(new SqlParam($"@p{c}", types[c] == "INTEGER" ? System.Data.DbType.Int64 : types[c] == "REAL" ? System.Data.DbType.Double : System.Data.DbType.String));
            for (int r = 1; r < rows; r++) { for (int c = 0; c < cols; c++) { object v = data[r, c]; ins.Parameters[$"@p{c}"].Value = (v == null || v is DBNull || ReferenceEquals(v, ExcelEmpty.Value) || v is ExcelError) ? DBNull.Value : v; } ins.ExecuteNonQuery(); }
            tx.Commit();
        }

        private static string Sanitize(string raw, int idx)
        { if (string.IsNullOrWhiteSpace(raw)) return $"Col{idx + 1}"; var ca = raw.ToCharArray(); for (int i = 0; i < ca.Length; i++) if (!char.IsLetterOrDigit(ca[i]) && ca[i] != '_') ca[i] = '_'; string n = new(ca); if (char.IsDigit(n[0])) n = "_" + n; return n; }
    }
}
