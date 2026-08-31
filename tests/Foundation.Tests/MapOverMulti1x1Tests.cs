using System;
using ExcelFormulaLabs.Foundation;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace ExcelFormulaLabs.Foundation.Tests
{
    public class MapOverMulti1x1Tests
    {
        private readonly ITestOutputHelper _o;
        public MapOverMulti1x1Tests(ITestOutputHelper o) { _o = o; }

        [Fact]
        public void MapOverMulti_1x1_broadcast_to_nx1()
        {
            var one = new object[,] { { 10.0 } };        // 1×1
            var many = new object[,] { { 1.0 }, { 2.0 }, { 3.0 } };  // 3×1
            try
            {
                var r = ElementWiseMapper.MapOverMulti<double, double, double>(one, many, (a, b) => a + b);
                _o.WriteLine($"1×1 广播结果类型: {r?.GetType().Name}");
                var arr = (object[,])r;
                _o.WriteLine($"shape: {arr.GetLength(0)}×{arr.GetLength(1)}  [0,0]={arr[0,0]}  [2,0]={arr[2,0]}");
                Convert.ToDouble(arr[2, 0]).Should().Be(13.0);
            }
            catch (Exception ex)
            {
                _o.WriteLine($"!! 1×1 广播失败: {ex.GetType().Name}: {ex.Message}");
                throw;
            }
        }
    }
}
