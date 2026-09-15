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

        /// <summary>墙钟执行预算（毫秒）：覆盖首行返回前的全部计算（R1-3）。</summary>
        internal const int DefaultBudgetMs = 5000;

        // R1-3：CommandTimeout 只约束获取锁的等待，不约束执行（net8 SqliteCommand.Cancel
        // 文档 "Does nothing."）；聚合/排序/笛卡尔积在首行前完成计算时，读取循环内的
        // 秒表检查永不执行（实测 1000³ 交叉连接阻塞 10.4s 后成功返回）。
        // net8：看门狗在 budget 到期时调用 sqlite3_interrupt（官方声明可从其它线程调用），
        //        SQLite 在 VM 指令边界中止查询并抛异常。
        // net48：System.Data.SQLite 的 interop 为 C++/CLI（无 sqlite3_interrupt 导出符号，
        //        P/Invoke 实测 EntryPointNotFound），且 SQLiteCommand.Cancel 实测 no-op
        //        （500³ 查询照常跑完）；唯一可用机制是公开的 Progress 事件（ProgressOps>0
        //        时每个 VM 批次回调）+ ProgressEventArgs.ReturnCode=Interrupt。
        //        实测中断后可能静默返回 null/提前结束 Read（不抛异常）→ 统一查中断标志。
        [ThreadStatic] private static int _budgetInterrupted;
#if NET48
        // 预算起始 TickCount（int，环境单调毫秒；unchecked 差值比较对 24.9 天回绕安全，
        // 且不含除法表达式——pre-commit 检查 5 对 Core 文件的除法要求同文件 NaN/Inf 守卫）。
        [ThreadStatic] private static int _budgetStartTick;
        [ThreadStatic] private static int _budgetMs;
        [ThreadStatic] private static int _budgetArmed;
        private static void ProgressBudgetTick(object sender, System.Data.SQLite.ProgressEventArgs e)
        {
            _ = sender;
            if (_budgetArmed != 0 && unchecked(Environment.TickCount - _budgetStartTick) > _budgetMs)
            {
                System.Threading.Interlocked.Exchange(ref _budgetInterrupted, 1);
                e.ReturnCode = System.Data.SQLite.SQLiteProgressReturnCode.Interrupt;
            }
        }
        private static void InterruptCommand(SqlConn conn, System.Data.Common.DbCommand cmd)
        {
            _ = conn; _ = cmd; // net48 由 Progress 事件中断（前置注册），看门狗无操作
        }
#else
        private static void InterruptCommand(SqlConn conn, System.Data.Common.DbCommand cmd)
        {
            _ = cmd;
            try { SQLitePCL.raw.sqlite3_interrupt(conn.Handle); }
            catch (Exception ex) when (ExceptionFilters.IsCatchable(ex)) { /* 查询已结束/连接已释放 */ }
        }
#endif
        private static bool BudgetInterrupted =>
            System.Threading.Volatile.Read(ref _budgetInterrupted) != 0;

        /// <summary>Quick check that a SQL statement is read-only.
        /// Rejects DDL (CREATE/ALTER/DROP), DML (INSERT/UPDATE/DELETE),
        /// ATTACH/DETACH, and PRAGMA for safety in shared-workbook scenarios.
        /// Accepts SELECT and WITH (CTE) prefixes, optionally preceded by
        /// whitespace and comments; no whitespace is required after the keyword
        /// (native SQLite accepts "SELECT*FROM data").</summary>
        // SelectOnly/ForbiddenKeyword 必须带 RegexOptions.CultureInvariant（对齐 RegexCore.cs）：
        // 否则 IgnoreCase 依赖 CurrentCulture，土耳其语 locale（i→İ）会让小写 "insert"
        // 无法命中 INSERT 黑名单，安全检查失效。
        // 关键字后缀用 `\b` 而非 `\s`：须接受 SELECT*FROM data 这类合法写法；
        // 前导注释先经 TryStripSqlComments 剥离后再匹配（分号检查仍扫原文）。
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
        /// \bRECURSIVE\b 拦截无限递归 CTE
        /// （WITH RECURSIVE x(n) AS (SELECT 1 UNION ALL SELECT n+1 FROM x)）：它可通过
        /// 前缀检查且不被任何关键字拦截，输出无上界 → OOM。递归 CTE 在本功能语境
        /// （内存表只读查询）没有任何正当用途。</summary>
        private static readonly System.Text.RegularExpressions.Regex ForbiddenKeyword =
            new(@"\b(INSERT|UPDATE|DELETE|REPLACE\s+INTO|ATTACH|DETACH|PRAGMA|DROP|CREATE|ALTER|VACUUM|REINDEX|RECURSIVE|LOAD_EXTENSION)\b",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase
                | System.Text.RegularExpressions.RegexOptions.CultureInvariant
                | System.Text.RegularExpressions.RegexOptions.Compiled,
                TimeSpan.FromSeconds(5));

        internal static object[,]? SqlQuery(object[,] range, string sql, Dictionary<string, object[,]>? extra = null, bool hasHeaders = true, int budgetMs = DefaultBudgetMs)
        {
            // 黑名单须扫 StripSqlComments 归一化文本：SQL 注释可拆分关键字
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
            _budgetInterrupted = 0;
#if NET48
            // 墙钟 deadline（net48 Progress 回调用；net8 由看门狗定时器负责）。
            _budgetStartTick = Environment.TickCount;
            _budgetMs = budgetMs;
            _budgetArmed = 1;
#endif
#if NET48
            // 必须在 ExecuteReader 之前注册（ProgressOps 影响语句准备期的进度回调频率）。
            int savedProgressOps = conn.ProgressOps;
            conn.ProgressOps = 1000;
            conn.Progress += ProgressBudgetTick;
#endif
            // 看门狗：budget 到期 → InterruptCommand（net8 sqlite3_interrupt；net48 无操作，
            // 由 Progress 事件中断）；查询在 VM 指令边界中止。Dispose(WaitHandle) 同步等待
            // 回调完成，避免连接释放后回调触碰已释放的原生句柄（use-after-free）。
            int budgetExceeded = 0;
            var watchdog = new System.Threading.Timer(_ =>
            {
                try
                {
                    System.Threading.Interlocked.Exchange(ref budgetExceeded, 1);
                    InterruptCommand(conn, cmd);
                }
                catch (Exception ex) when (ExceptionFilters.IsCatchable(ex)) { /* 中断失败不得让回调线程崩溃 */ }
            }, null, budgetMs, System.Threading.Timeout.Infinite);
            System.Data.Common.DbDataReader reader;
            try
            {
                reader = cmd.ExecuteReader();
            }
            catch (Exception ex) when (ExceptionFilters.IsCatchable(ex))
            {
                if (System.Threading.Volatile.Read(ref budgetExceeded) != 0 || BudgetInterrupted)
                {
                    StopWatchdog(watchdog);
                    throw BudgetError(budgetMs, ex);
                }
                StopWatchdog(watchdog);
                throw;
            }
            // net48 的 Progress 中断可能不抛异常而让 ExecuteReader 返回（结果为空/部分）——
            // 统一在此显式失败，不得把中止误当"查询成功但无数据"。
            if (System.Threading.Volatile.Read(ref budgetExceeded) != 0 || BudgetInterrupted)
            {
                StopWatchdog(watchdog);
                reader.Dispose();
                throw BudgetError(budgetMs, null);
            }
            try
            {
                using var readerScope = reader;
                // 防线：① 读取循环行数 + 耗时双上限；② 直接写入预分配 object[,]（消除 2× 峰值）——
                // 若只 `rows.Add(row)` 无上界 + 末尾整体复制（峰值 2× 内存），
                // `SELECT * FROM a, b`（100k 行 → 1e10 行）等失控查询会触发
                // **不可捕获的 OOM → Excel 进程崩溃**（ExceptionFilters 排除 OOM，WrapError 不兜底）。
                // 单值防线 = 事后拒绝：全库 0 次 SetLimit，SQLite 默认 SQLITE_MAX_LENGTH=1e9，
                // randomblob(5e8) 先整体分配 500MB 才被下方事后 10MB 检查拒绝；
                // 分配峰值窗口为已知残余（与下方 blob 检查处注释口径一致）。
                int cols = reader.FieldCount;
                const int maxRows = 200_000;
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
                    for (int i = 0; i < cols; i++)
                    {
                        object v = reader.GetValue(i);
                        // 单值巨型 blob（如 SELECT randomblob(1000000000)）由 SQLite 层
                        // 整体分配后才到达此处——
                        // 无法在分配前拦截，但对已读出的超大值立即拒绝，防止其进入结果数组
                        // （32 位 Excel 单值 >2GB 时 GetValue 本身仍可能 OOM，属已知残余，见文档）。
                        if (v is byte[] blob && blob.Length > 10_000_000)
                            throw new ArgumentException(
                                $"SQL query returned a {blob.Length:N0}-byte blob at row {row}, column {i} — " +
                                "possible runaway query (randomblob). Limit blobs to 10 MB.");
                        // 文本巨值与 blob 共用 10MB 单值预算：只查 byte[] 时
                        // hex(randomblob(11e6)) 等文本型巨值（22,000,000 字符）不受限
                        // （32 位 Excel 单元格无法承载）。
                        if (v is string hugeText && hugeText.Length > 10_000_000)
                            throw new ArgumentException(
                                $"SQL query returned a {hugeText.Length:N0}-character text value at row {row}, column {i} — " +
                                "possible runaway query (hex/quote/CAST on randomblob). Limit values to 10 MB.");
                        // null =「空单元格」哨兵（DBNull→null，与 Excel 空单元格语义一致）；
                        // null! 豁免的是可空性分析而非断言运行时非空（object?[,] 本地数组
                        // 方案会在返回处触发 CS8619，不可用）。
                        result[row, i] = reader.IsDBNull(i) ? null! : v;
                    }
                    row++;
                }
                // 读取中途被 net48 Progress 中断时 Read() 可能静默返回 false（提前结束循环）
                // → 返回部分结果前必须查中断标志。
                if (System.Threading.Volatile.Read(ref budgetExceeded) != 0 || BudgetInterrupted)
                    throw BudgetError(budgetMs, null);
                if (row < result.GetLength(0))
                {
                    var trimmed = new object[row, cols];
                    for (int r = 0; r < row; r++) for (int c = 0; c < cols; c++) trimmed[r, c] = result[r, c];
                    return trimmed;
                }
                return result;
            }
            catch (Exception ex) when (ExceptionFilters.IsCatchable(ex)
                && (System.Threading.Volatile.Read(ref budgetExceeded) != 0 || BudgetInterrupted))
            {
                throw BudgetError(budgetMs, ex);
            }
            finally
            {
                StopWatchdog(watchdog);
#if NET48
                try
                {
                    conn.Progress -= ProgressBudgetTick;
                    conn.ProgressOps = savedProgressOps;
                }
                catch (Exception ex) when (ExceptionFilters.IsCatchable(ex)) { /* 连接已释放 */ }
#endif
#if NET48
                _budgetArmed = 0;
                _budgetMs = 0;
#endif
                _budgetInterrupted = 0;
            }
        }

        private static ArgumentException BudgetError(int budgetMs, Exception? cause) =>
            new($"SQL query exceeded the {budgetMs / 1000.0:0.#}-second execution budget — " +
                "possible runaway query (cross join / recursive CTE). Narrow the query with WHERE/LIMIT.", cause);

        /// <summary>停止看门狗并同步等待进行中的回调结束（防连接释放后回调触碰原生句柄）。</summary>
        private static void StopWatchdog(System.Threading.Timer watchdog)
        {
            try
            {
                using var done = new System.Threading.ManualResetEvent(false);
                if (watchdog.Dispose(done)) done.WaitOne();
            }
            catch (Exception ex) when (ExceptionFilters.IsCatchable(ex)) { /* 已停止 */ }
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
                // 须扫全表（不止前 10 行窗口）：窗口外文本落入数值列时 net48 参数转换抛
                // FormatException（整条查询 #VALUE!），net8 则把文本静默存进 REAL 列污染
                // SUM/比较。插入循环本就是 O(rows×cols)，全表扫描只增加一个同阶常数因子，
                // 不引入新的复杂度上界（输入行数受 Excel 区域规模约束）。
                bool hasReal = false, hasInt = false;
                for (int r = firstDataRow; r < rows; r++)
                {
                    object v = data[r, c];
                    // Excel 错误单元格由封送层提供，类型全名与 Foundation.ExcelError
                    // 不同——必须走 IsExcelErrorValue 才按空值跳过
                    // （Core 层零 Excel 依赖，只能按名识别）。
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
