using ExcelVbaLibraries.DataToolkit;
using ExcelVbaLibraries.Foundation;
using FluentAssertions;
using Xunit;
namespace ExcelVbaLibraries.DataToolkit.Tests
{
    public class SqlUdfTests
    {
        private static readonly object[,] Data = new object[,] { { "Name", "Age" }, { "Alice", 30.0 }, { "Bob", 25.0 } };
        private static readonly object[,] Extra = new object[,] { { "Name", "Score" }, { "Alice", 95.0 }, { "Bob", 87.0 } };
        private static readonly object[,] TableB = new object[,] { { "Name", "City" }, { "Alice", "NYC" }, { "Bob", "LA" } };
        private static readonly object[,] TableC = new object[,] { { "Name", "Dept" }, { "Alice", "Eng" }, { "Bob", "Sales" } };
        [Fact] public void Query_select_all() { var r=(object[,])SqlUdf.UDF_SQL_QUERY(Data,"SELECT * FROM data"); r.GetLength(0).Should().Be(3); }
        [Fact] public void Query_invalid_sql() => SqlUdf.UDF_SQL_QUERY(Data,"INVALID SQL").Should().BeNull();
        [Fact] public void Join_basic() { var r=(object[,])SqlUdf.UDF_SQL_JOIN(Data,Extra,"SELECT a.Name, b.Score FROM data a JOIN extra b ON a.Name=b.Name"); r.GetLength(0).Should().Be(3); r[1,0].Should().Be("Alice"); r[1,1].Should().Be(95.0); }
        [Fact] public void Join_invalid_sql() => SqlUdf.UDF_SQL_JOIN(Data,Extra,"BAD JOIN").Should().BeNull();
        [Fact] public void Query3_basic() { var r=(object[,])SqlUdf.UDF_SQL_QUERY3(Data,TableB,TableC,"SELECT a.Name, b.City, c.Dept FROM data a JOIN b ON a.Name=b.Name JOIN c ON a.Name=c.Name"); r.GetLength(0).Should().Be(3); }
        [Fact] public void Query3_invalid_sql() => SqlUdf.UDF_SQL_QUERY3(Data,TableB,TableC,"INVALID").Should().BeNull();
    }
}
