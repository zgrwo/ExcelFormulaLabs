using System.Linq;
using ExcelDna.Integration;
using ExcelFormulaLabs.Foundation;

namespace ExcelFormulaLabs.DataToolkit
{
    public static partial class RangeExportUdf
    {
        private static object[,] D(object d)=>InputNormalizer.NormalizeTo2D(d)!;
    }
}