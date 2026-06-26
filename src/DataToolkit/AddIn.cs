using System;
using ExcelDna.Integration;
using ExcelDna.IntelliSense;
using ExcelVbaLibraries.Foundation;

namespace ExcelVbaLibraries.DataToolkit
{
    public class AddIn : IExcelAddIn
    {
        public void AutoOpen() => IntelliSenseServer.Install();

        public void AutoClose()
        {
            FilterUtils.ClearRegexCache();
#if NET48
            try { System.Data.SQLite.SQLiteConnection.ClearAllPools(); }
            catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException and not AccessViolationException) { }
#endif
            // IntelliSenseServer.Uninstall() intentionally NOT called —
            // process-wide server; dual-unload causes intermittent crash.
            FileSystemCore.SandboxRoot = null;
        }
    }
}
