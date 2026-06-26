using System;
using ExcelDna.Integration;
using ExcelDna.IntelliSense;

namespace ExcelVbaLibraries.Analytics
{
    public class AddIn : IExcelAddIn
    {
        public void AutoOpen() => IntelliSenseServer.Install();

        public void AutoClose()
        {
            LinalgCore.ClearDecompCache();
            // IntelliSenseServer.Uninstall() intentionally NOT called.
            // The IntelliSense server is process-wide — when two add-ins share it,
            // one calling Uninstall() tears down the server while the other may
            // still be using it, causing intermittent crashes on unload.
            // The server process exits cleanly when Excel closes.
        }
    }
}
