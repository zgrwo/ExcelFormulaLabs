using ExcelDna.Integration;
using ExcelDna.IntelliSense;

namespace ExcelVbaLibraries.DataToolkit
{
    public class AddIn : IExcelAddIn
    {
        public void AutoOpen() => IntelliSenseServer.Install();
        public void AutoClose() { }
    }
}
