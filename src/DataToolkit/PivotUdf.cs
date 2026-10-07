using System.Linq;
using ExcelDna.Integration;
using ExcelFormulaLabs.Foundation;

namespace ExcelFormulaLabs.DataToolkit
{
    public static partial class PivotUdf
    {
        private static string Agg(object agg) { var s = InputNormalizer.ToString(agg); return s.Length == 0 ? "SUM" : s; }
    }
}
