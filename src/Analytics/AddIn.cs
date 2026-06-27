using System;
using ExcelDna.Integration;
#if NET48
using ExcelDna.IntelliSense;
#endif

namespace ExcelFormulaLabs.Analytics
{
    public class AddIn : IExcelAddIn
    {
        public void AutoOpen()
        {
#if NET48
            ExcelAsyncUtil.QueueAsMacro(() => IntelliSenseServer.Install());
#endif
            // IntelliSense disabled for net8.0: Excel-DNA Issue #343 —
            // ProcessLoadNotification callback triggers NRE in
            // ExcelSynchronizationContext.Post() under .NET 8.
        }

        public void AutoClose()
        {
            LinalgCore.ClearDecompCache();
#if NET48
            try { IntelliSenseServer.Uninstall(); }
            catch (Exception ex) when (ex is not OutOfMemoryException
                and not StackOverflowException and not AccessViolationException)
            { /* best-effort: server may already be unloaded */ }
#endif
        }
    }
}
