using System;
using ExcelDna.Integration;
using ExcelDna.IntelliSense;
using ExcelVbaLibraries.Foundation;

namespace ExcelVbaLibraries.DataToolkit
{
    public class AddIn : IExcelAddIn
    {
        public void AutoOpen()
        {
            ExcelIntegration.RegisterUnhandledExceptionHandler(ex =>
                ex is Exception ce && ce is NullReferenceException &&
                ce.StackTrace?.Contains("ExcelSynchronizationContext.Post") == true
                    ? (object)ExcelDna.Integration.ExcelError.ExcelErrorNA
                    : (object)ExcelDna.Integration.ExcelError.ExcelErrorValue
            );

            ExcelAsyncUtil.QueueAsMacro(() => IntelliSenseServer.Install());
        }

        public void AutoClose()
        {
            FilterUtils.ClearRegexCache();
#if NET48
            try { System.Data.SQLite.SQLiteConnection.ClearAllPools(); }
            catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException and not AccessViolationException) { }
#endif
            FileSystemCore.SandboxRoot = null;
        }
    }
}
