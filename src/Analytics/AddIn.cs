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
            try { IntelliSenseServer.Uninstall(); }
            catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException and not AccessViolationException) { }
        }
    }
}
