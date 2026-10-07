using ExcelDna.Integration;
using ExcelFormulaLabs.Foundation;

namespace ExcelFormulaLabs.DataToolkit
{
    public static partial class DictSetUdf
    {
        private static object[] A(object a)=>InputNormalizer.NormalizeTo1D(a);
    }
}
