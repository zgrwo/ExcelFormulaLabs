using ExcelDna.Integration;

namespace ExcelVbaLibraries.Analytics
{
    public class AddIn : IExcelAddIn
    {
        public void AutoOpen() { }
        public void AutoClose() => LinalgCore.ClearDecompCache();
    }
}
