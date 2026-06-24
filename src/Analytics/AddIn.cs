using ExcelDna.Integration;
using ExcelDna.IntelliSense;

namespace ExcelVbaLibraries.Analytics
{
    public class AddIn : IExcelAddIn
    {
        public void AutoOpen() => IntelliSenseServer.Install();
        public void AutoClose() { }
    }
}
