using System;
using System.IO;
using ExcelDna.Integration;
using ExcelDna.IntelliSense;
using ExcelVbaLibraries.Foundation;

namespace ExcelVbaLibraries.DataToolkit
{
    public class AddIn : IExcelAddIn
    {
        public void AutoOpen()
        {
            // Defer IntelliSense installation until the add-in is fully initialised.
            // IntelliSenseServer.Install() must not be called directly in AutoOpen
            // because the ExcelSynchronizationContext is not yet ready — the IntelliSense
            // server's ProcessLoadNotification callback will NRE inside Post().
            ExcelAsyncUtil.QueueAsMacro(() =>
            {
                try { IntelliSenseServer.Install(); }
                catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException and not AccessViolationException)
                {
                    LogError("DataToolkit", $"IntelliSenseServer.Install() failed: {ex.Message}");
                }
            });
        }

        public void AutoClose()
        {
            FilterUtils.ClearRegexCache();
#if NET48
            try { System.Data.SQLite.SQLiteConnection.ClearAllPools(); }
            catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException and not AccessViolationException) { /* best-effort */ }
#endif
            // IntelliSenseServer.Uninstall() is intentionally NOT called here —
            // during unload the ExcelSynchronizationContext is already tearing down
            // and a pending ProcessLoadNotification callback will NRE inside Post().
            // The IntelliSense server process exits on its own when the add-in unloads.
            // Clear sandbox LAST — ensures no cleanup callback runs without sandbox protection
            FileSystemCore.SandboxRoot = null;
        }

        /// <summary>
        /// Best-effort diagnostic log. Writes to %TEMP%\ExcelVbaLibraries\install.log.
        /// Synchronised across both add-in assemblies via a lock on a shared Foundation type.
        /// Must never throw — failure to log must not prevent add-in loading.
        /// </summary>
        private static void LogError(string component, string message)
        {
            try
            {
                string dir = Path.Combine(Path.GetTempPath(), "ExcelVbaLibraries");
                Directory.CreateDirectory(dir);
                lock (typeof(Foundation.ExcelEmpty))
                {
                    File.AppendAllText(Path.Combine(dir, "install.log"),
                        $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{component}] {message}{Environment.NewLine}");
                }
            }
            catch (Exception ex) when (ex is not OutOfMemoryException
                and not StackOverflowException
                and not AccessViolationException) { /* best-effort */ }
        }
    }
}
