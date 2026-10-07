using System.Linq;
using ExcelDna.Integration;
using ExcelFormulaLabs.Foundation;

namespace ExcelFormulaLabs.DataToolkit
{
    public static partial class StringUdf
    {

        /// <summary>MapOver 会在 mapper 前短路
        /// ExcelEmpty（按空白透传），使 COALESCE/ISNULLEMPTY/ISNULLWS 对真空白单元格失效。
        /// 按产品决策「空白 = 空值」，这三个 UDF 先逐元素把空白哨兵归一化为空串再映射
        /// （object[]/object[,] 与标量；COM Range 仍由 MapOver 内部提取，属直调残余）。</summary>
        private static object BlankAsEmpty(object? input)
        {
            // P3-5：须先提取 COM Range——否则 COM 对象落标量分支（非 object[]/[,]），
            // MapOver 内部再提取时空白单元格已绕过本归一化 → ISNULLEMPTY/ISNULLWS/
            // COALESCE 对 COM 区域空白列失效。Foundation 的提取器为 internal（设计上
            // 只给 ElementWiseMapper 用），此处经公开的 NormalizeTo2D 走同一提取路径。
            if (input is not null && System.Runtime.InteropServices.Marshal.IsComObject(input))
            {
                var extracted = InputNormalizer.NormalizeTo2D(input);
                if (extracted != null) input = extracted;
            }
            if (input is object[,] a2)
            {
                var r = new object[a2.GetLength(0), a2.GetLength(1)];
                for (int i = 0; i < a2.GetLength(0); i++)
                    for (int j = 0; j < a2.GetLength(1); j++)
                        r[i, j] = InputNormalizer.IsExcelEmptyValue(a2[i, j]) ? "" : a2[i, j];
                return r;
            }
            if (input is object[] a1)
                return a1.Select(x => InputNormalizer.IsExcelEmptyValue(x) ? (object)"" : x).ToArray();
            return InputNormalizer.IsExcelEmptyValue(input) ? "" : input;
        }
    }
}
