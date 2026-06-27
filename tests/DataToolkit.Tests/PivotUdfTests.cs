using FormulaLabs.DataToolkit;
using FormulaLabs.Foundation;
using FluentAssertions;
using Xunit;

namespace FormulaLabs.DataToolkit.Tests
{
    public class PivotUdfTests
    {
        private static readonly object[,] Data = { { "Dept", "Q", "Sales" }, { "A", 1, 100.0 }, { "A", 2, 200.0 }, { "B", 1, 300.0 } };

        // ══════════════════════════════════════════════════════════════════
        //  PIVOT.PIVOT  (NormalizeTo2D! — null → ExcelError.Value)
        // ══════════════════════════════════════════════════════════════════
        [Fact] public void Pivot_basic()
        {
            var r = (object[,])PivotUdf.UDF_PIVOT_PIVOT(Data, 0, 1, 2, "SUM");
            r.GetLength(0).Should().BeGreaterThan(1);
            r.GetLength(1).Should().BeGreaterThan(1);
        }
        [Fact] public void Pivot_header_row()
        {
            var r = (object[,])PivotUdf.UDF_PIVOT_PIVOT(Data, 0, 1, 2, "SUM");
            r[0, 0].Should().Be("Key \\ Pivot");
        }
        [Fact] public void Pivot_dept_keys()
        {
            var r = (object[,])PivotUdf.UDF_PIVOT_PIVOT(Data, 0, 1, 2, "SUM");
            r[1, 0].Should().Be("A");
            r[2, 0].Should().Be("B");
        }
        [Fact] public void Pivot_agg_max()
        {
            var r = (object[,])PivotUdf.UDF_PIVOT_PIVOT(Data, 0, 1, 2, "MAX");
            r.GetLength(0).Should().BeGreaterThan(1);
        }
        [Fact] public void Pivot_agg_min()
        {
            var r = (object[,])PivotUdf.UDF_PIVOT_PIVOT(Data, 0, 1, 2, "MIN");
            r.GetLength(0).Should().BeGreaterThan(1);
        }
        [Fact] public void Pivot_null_data() => PivotUdf.UDF_PIVOT_PIVOT(null!, 0, 1, 2, "SUM").Should().Be(ExcelError.Value);
        [Fact] public void Pivot_default_agg()
        {
            var r = (object[,])PivotUdf.UDF_PIVOT_PIVOT(Data, 0, 1, 2, "SUM");
            r[1, 1].Should().Be(100.0);
        }

        // ══════════════════════════════════════════════════════════════════
        //  PIVOT.UNPIVOT  (NormalizeTo2D! — null → ExcelError.Value)
        // ══════════════════════════════════════════════════════════════════
        [Fact] public void Unpivot_basic()
        {
            var r = (object[,])PivotUdf.UDF_PIVOT_UNPIVOT(Data, new int[] { 0 }, new int[] { 1, 2 });
            r.GetLength(0).Should().BeGreaterThan(1);
            r.GetLength(1).Should().Be(3);
        }
        [Fact] public void Unpivot_row_count()
        {
            var r = (object[,])PivotUdf.UDF_PIVOT_UNPIVOT(Data, new int[] { 0 }, new int[] { 1, 2 });
            r.GetLength(0).Should().Be(6);  // 3 data rows × 2 value cols (header skipped)
        }
        [Fact] public void Unpivot_id_column_present()
        {
            var r = (object[,])PivotUdf.UDF_PIVOT_UNPIVOT(Data, new int[] { 0 }, new int[] { 1, 2 });
            r[0, 0].Should().Be("A");    // first data row (header skipped)
            r[2, 0].Should().Be("A");
        }
        [Fact] public void Unpivot_value_headers()
        {
            var r = (object[,])PivotUdf.UDF_PIVOT_UNPIVOT(Data, new int[] { 0 }, new int[] { 1, 2 });
            r[1, 0].Should().Be("A");
            r[1, 1].Should().Be("Sales");
            r[1, 2].Should().Be(100.0);
        }
        [Fact] public void Unpivot_single_value_col()
        {
            var r = (object[,])PivotUdf.UDF_PIVOT_UNPIVOT(Data, new int[] { 0 }, new int[] { 2 });
            r.GetLength(1).Should().Be(3);
            r.GetLength(0).Should().Be(3);  // 3 data rows × 1 value col (header skipped)
        }
        [Fact] public void Unpivot_null_data() => PivotUdf.UDF_PIVOT_UNPIVOT(null!, new int[] { 0 }, new int[] { 1 }).Should().Be(ExcelError.Value);
        [Fact] public void Unpivot_two_id_cols()
        {
            var r = (object[,])PivotUdf.UDF_PIVOT_UNPIVOT(Data, new int[] { 0, 1 }, new int[] { 2 });
            r.GetLength(1).Should().Be(4);
            r.GetLength(0).Should().Be(3);  // 3 data rows × 1 value col (header skipped)
        }

        // ══════════════════════════════════════════════════════════════════
        //  PIVOT.GROUPBY  (NormalizeTo2D! — null → ExcelError.Value)
        // ══════════════════════════════════════════════════════════════════
        [Fact] public void GroupBy_basic()
        {
            var r = (object[,])PivotUdf.UDF_PIVOT_GROUPBY(Data, new int[] { 0 }, 2, "SUM");
            r.GetLength(0).Should().BeGreaterThan(0);
            r.GetLength(1).Should().Be(2);
        }
        [Fact] public void GroupBy_sum()
        {
            var r = (object[,])PivotUdf.UDF_PIVOT_GROUPBY(Data, new int[] { 0 }, 2, "SUM");
            r[0, 0].Should().Be("A");
            r[0, 1].Should().Be(300.0);
            r[1, 0].Should().Be("B");
            r[1, 1].Should().Be(300.0);
        }
        [Fact] public void GroupBy_count()
        {
            var r = (object[,])PivotUdf.UDF_PIVOT_GROUPBY(Data, new int[] { 0 }, 2, "COUNT");
            r[0, 1].Should().Be(2L);
            r[1, 1].Should().Be(1L);
        }
        [Fact] public void GroupBy_avg()
        {
            var r = (object[,])PivotUdf.UDF_PIVOT_GROUPBY(Data, new int[] { 0 }, 2, "AVG");
            r[0, 1].Should().Be(150.0);
            r[1, 1].Should().Be(300.0);
        }
        [Fact] public void GroupBy_max()
        {
            var r = (object[,])PivotUdf.UDF_PIVOT_GROUPBY(Data, new int[] { 0 }, 2, "MAX");
            r[0, 1].Should().Be(200.0);
        }
        [Fact] public void GroupBy_min()
        {
            var r = (object[,])PivotUdf.UDF_PIVOT_GROUPBY(Data, new int[] { 0 }, 2, "MIN");
            r[0, 1].Should().Be(100.0);
        }
        [Fact] public void GroupBy_multi_col()
        {
            var r = (object[,])PivotUdf.UDF_PIVOT_GROUPBY(Data, new int[] { 0, 1 }, 2, "SUM");
            r.GetLength(0).Should().BeGreaterThan(0);
            r.GetLength(1).Should().Be(3);
        }
        [Fact] public void GroupBy_null_data() => PivotUdf.UDF_PIVOT_GROUPBY(null!, new int[] { 0 }, 2, "SUM").Should().Be(ExcelError.Value);
        [Fact] public void GroupBy_row_count()
        {
            var r = (object[,])PivotUdf.UDF_PIVOT_GROUPBY(Data, new int[] { 0 }, 2, "SUM");
            r.GetLength(0).Should().Be(2);
        }

        // ══════════════════════════════════════════════════════════════════
        //  PIVOT.CROSSJOIN  (NormalizeTo2D! on both — null → ExcelError.Value)
        // ══════════════════════════════════════════════════════════════════
        [Fact] public void CrossJoin_basic()
        {
            var r = (object[,])PivotUdf.UDF_PIVOT_CROSSJOIN(Data, Data);
            r.GetLength(0).Should().Be(Data.GetLength(0) * Data.GetLength(0));
            r.GetLength(1).Should().Be(Data.GetLength(1) + Data.GetLength(1));
        }
        [Fact] public void CrossJoin_small()
        {
            var a = new object[,] { { 1 }, { 2 } };
            var b = new object[,] { { "x" }, { "y" }, { "z" } };
            var r = (object[,])PivotUdf.UDF_PIVOT_CROSSJOIN(a, b);
            r.GetLength(0).Should().Be(6);
            r.GetLength(1).Should().Be(2);
        }
        [Fact] public void CrossJoin_single_row()
        {
            var a = new object[,] { { 1, 2 } };
            var b = new object[,] { { 3 }, { 4 } };
            var r = (object[,])PivotUdf.UDF_PIVOT_CROSSJOIN(a, b);
            r.GetLength(0).Should().Be(2);
            r.GetLength(1).Should().Be(3);
        }
        [Fact] public void CrossJoin_null_first() => PivotUdf.UDF_PIVOT_CROSSJOIN(null!, Data).Should().Be(ExcelError.Value);
        [Fact] public void CrossJoin_null_second() => PivotUdf.UDF_PIVOT_CROSSJOIN(Data, null!).Should().Be(ExcelError.Value);
        [Fact] public void CrossJoin_null_both() => PivotUdf.UDF_PIVOT_CROSSJOIN(null!, null!).Should().Be(ExcelError.Value);
        [Fact] public void CrossJoin_preserves_first_row()
        {
            var a = new object[,] { { "X" }, { 10 } };
            var b = new object[,] { { "Y" }, { 20 } };
            var r = (object[,])PivotUdf.UDF_PIVOT_CROSSJOIN(a, b);
            r[0, 0].Should().Be("X");
            r[0, 1].Should().Be("Y");
            r[2, 0].Should().Be(10);
            r[3, 0].Should().Be(10);
            r[3, 1].Should().Be(20);
        }
    }
}
