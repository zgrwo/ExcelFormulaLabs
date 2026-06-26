using System;
using System.IO;
using ExcelDna.Integration;
using ExcelDna.IntelliSense;

namespace ExcelVbaLibraries.Analytics
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
                    LogError("Analytics", $"IntelliSenseServer.Install() failed: {ex.Message}");
                }
            });
        }

        public void AutoClose()
        {
            LinalgCore.ClearDecompCache();
            // IntelliSenseServer.Uninstall() is intentionally NOT called here.
            // During unload the ExcelSynchronizationContext is already tearing down;
            // the server's asynchronous ProcessLoadNotification callback will NRE
            // inside Post() if Uninstall triggers a pending notification.
            // The IntelliSense server process exits on its own when the add-in unloads.
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
                // Lock on a type shared by both add-in assemblies (both reference Foundation)
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
