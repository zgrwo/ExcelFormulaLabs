using System;
using System.Collections.Generic;
using System.Linq;
using ExcelDna.Integration;
using ExcelFormulaLabs.Foundation;

namespace ExcelFormulaLabs.DataToolkit
{
    public static partial class ArrayUdf
    {
        private static object[] A(object a)=>InputNormalizer.NormalizeTo1D(a);
        private static ComparerMode ParseSortMode(object mode) => InputNormalizer.ToString(mode) switch { "numeric" => ComparerMode.Numeric, "text" => ComparerMode.Text, _ => ComparerMode.Auto };
    }
}
