using ExcelDna.Integration;
using ExcelFormulaLabs.Foundation;

namespace ExcelFormulaLabs.Analytics
{
    public static partial class LinalgUdf
    {
        private static double[,] M(object d) => AnalyticsHelpers.PrepM(d);
        private static double[] V(object d) => AnalyticsHelpers.PrepV(d);

        // ── SVD (split into 3 individual UDFs) ──────────────────────

        // ── QR (split into 2 individual UDFs) ───────────────────────

        // ── LU (split into 3 individual UDFs) ───────────────────────

        // ── Other LINALG functions ──────────────────────────────────

    }
}
