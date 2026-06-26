using ExcelDna.Integration;
using ExcelDna.IntelliSense;

namespace ExcelVbaLibraries.Analytics
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
            LinalgCore.ClearDecompCache();
            // IntelliSenseServer.Uninstall() intentionally NOT called.
            // Process-wide server; dual-unload causes intermittent crash.
        }
    }
}
