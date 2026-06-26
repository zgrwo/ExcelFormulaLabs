using System;
using ExcelDna.Integration;
using ExcelDna.IntelliSense;

namespace ExcelVbaLibraries.Analytics
{
    public class AddIn : IExcelAddIn
    {
        public void AutoOpen()
        {
            // Catch async NRE from IntelliSense ProcessLoadNotification callback.
            // This is a known Excel-DNA bug (Issue #343): the ExcelSynchronizationContext
            // may not be ready when the IntelliSense server asynchronously posts back.
            ExcelIntegration.RegisterUnhandledExceptionHandler(ex =>
                ex is Exception ce && ce is NullReferenceException &&
                ce.StackTrace?.Contains("ExcelSynchronizationContext.Post") == true
                    ? (object)ExcelError.ExcelErrorNA
                    : (object)ExcelError.ExcelErrorValue
            );

            ExcelAsyncUtil.QueueAsMacro(() => IntelliSenseServer.Install());
        }

        public void AutoClose()
        {
            LinalgCore.ClearDecompCache();
        }
    }
}
