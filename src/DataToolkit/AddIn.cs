using ExcelDna.Integration;
using ExcelDna.IntelliSense;

namespace ExcelVbaLibraries.DataToolkit
{
    public class AddIn : IExcelAddIn
    {
        public void AutoOpen() => IntelliSenseServer.Install();
        public void AutoClose()
        {
            try { IntelliSenseServer.Uninstall(); }
            catch { /* best-effort: server may already be unloaded */ }
        }
    }
}
