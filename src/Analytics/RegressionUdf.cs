using System.Linq;
using ExcelDna.Integration;
using ExcelFormulaLabs.Foundation;

namespace ExcelFormulaLabs.Analytics
{
    public static partial class RegressionUdf
    {
        private static double[,] M(object d) => AnalyticsHelpers.PrepM(d);
        private static double[] V(object d) => AnalyticsHelpers.PrepV(d);

    }
}
