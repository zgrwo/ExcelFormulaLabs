using System;
using ExcelDna.Integration;
using ExcelDna.IntelliSense;
using ExcelVbaLibraries.Foundation;

namespace ExcelVbaLibraries.DataToolkit
{
    public class AddIn : IExcelAddIn
    {
        // QueueAsMacro defers IntelliSenseServer.Install() until the Excel
        // synchronization context is fully initialised. Calling it directly in
        // AutoOpen triggers ProcessLoadNotification before the sync context is
        // ready, causing NullReferenceException in Post().
        public void AutoOpen() => ExcelAsyncUtil.QueueAsMacro(() => IntelliSenseServer.Install());

        public void AutoClose()
        {
            FilterUtils.ClearRegexCache();
#if NET48
            try { System.Data.SQLite.SQLiteConnection.ClearAllPools(); }
            catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException and not AccessViolationException) { }
#endif
            // IntelliSenseServer.Uninstall() intentionally NOT called.
            FileSystemCore.SandboxRoot = null;
        }
    }
}
