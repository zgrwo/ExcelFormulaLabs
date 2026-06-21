using System;
using ExcelVbaLibraries.Foundation;
using FluentAssertions;
using Xunit;

namespace ExcelVbaLibraries.Foundation.Tests
{
    /// <summary>
    /// Unit tests for OutputWrapper — error-safe execution and output reshaping.
    /// Cross-validated against Python's try/except behavior and numpy reshape.
    /// </summary>
    public class WrapErrorObjectTests
    {
        [Fact]
        public void Normal_execution_returns_result()
        {
            var result = OutputWrapper.WrapError(() => 42);
            result.Should().Be(42);
        }

        [Fact]
        public void Normal_execution_returns_string()
        {
            var result = OutputWrapper.WrapError(() => "hello");
            result.Should().Be("hello");
        }

        [Fact]
        public void Normal_execution_returns_null()
        {
            var result = OutputWrapper.WrapError(() => null!);
            result.Should().BeNull();
        }

        [Fact]
        public void Exception_returns_Value_error()
        {
            var result = OutputWrapper.WrapError(() => throw new InvalidOperationException("test"));
            result.Should().Be(ExcelError.Value);
        }

        [Fact]
        public void DivideByZero_returns_Value_error()
        {
            // Python: try: 1/0 ==> except: return sentinel (CVErr pattern)
            var result = OutputWrapper.WrapError(() =>
            {
                int x = 0; return 1 / x;
            });
            result.Should().Be(ExcelError.Value);
        }

        [Fact]
        public void NullReferenceException_returns_Value_error()
        {
            var result = OutputWrapper.WrapError(() =>
            {
                string? s = null; return s!.Length;
            });
            result.Should().Be(ExcelError.Value);
        }

        [Fact]
        public void Nested_exception_returns_Value_error()
        {
            // Python: nested try/except returns error sentinel
            var result = OutputWrapper.WrapError(() =>
            {
                try { throw new ArgumentException("inner"); }
                catch { throw new InvalidOperationException("outer"); }
            });
            result.Should().Be(ExcelError.Value);
        }
    }

    public class WrapErrorTypedTests
    {
        [Fact]
        public void Normal_returns_typed_value()
        {
            var result = OutputWrapper.WrapError(() => 3.14, double.NaN);
            result.Should().BeApproximately(3.14, 1e-15);
        }

        [Fact]
        public void Exception_returns_errorResult()
        {
            // Python: try: raise ==> except: return sentinel
            var result = OutputWrapper.WrapError<double>(() =>
                throw new Exception("fail"), double.NaN);
            result.Should().Be(double.NaN);
        }

        [Fact]
        public void Custom_errorResult_returned_on_exception()
        {
            var result = OutputWrapper.WrapError(() => "ok", "ERROR");
            result.Should().Be("ok");
        }

        [Fact]
        public void Custom_errorResult_on_failure()
        {
            var result = OutputWrapper.WrapError<string>(() =>
                throw new Exception("fail"), "ERROR");
            result.Should().Be("ERROR");
        }

        [Fact]
        public void Zero_errorResult_on_exception()
        {
            // Python: try: 1/0 ==> except: return 0
            var result = OutputWrapper.WrapError<int>(() =>
            {
                int x = 0; return 1 / x;
            }, 0);
            result.Should().Be(0);
        }
    }

    public class ReshapeOutputTests
    {
        [Fact]
        public void Scalar_to_1x1()
        {
            // Python: np.array(42).reshape(1,1)
            var result = OutputWrapper.ReshapeOutput(42, 1, 1);
            result[0, 0].Should().Be(42);
        }

        [Fact]
        public void Flat_to_2x3()
        {
            // Python: np.array([1,2,3,4,5,6]).reshape(2,3)
            var flat = new object[] { 1, 2, 3, 4, 5, 6 };
            var result = OutputWrapper.ReshapeOutput(flat, 2, 3);
            result[0, 0].Should().Be(1);
            result[0, 1].Should().Be(2);
            result[0, 2].Should().Be(3);
            result[1, 0].Should().Be(4);
            result[1, 1].Should().Be(5);
            result[1, 2].Should().Be(6);
        }

        [Fact]
        public void Flat_too_short_pads_with_empty()
        {
            // Python: np.pad(arr, (0, target_size - len(arr)), constant_values=np.nan)
            var flat = new object[] { 1, 2, 3 };
            var result = OutputWrapper.ReshapeOutput(flat, 2, 3);
            result[0, 0].Should().Be(1);
            result[0, 1].Should().Be(2);
            result[0, 2].Should().Be(3);
            result[1, 0].Should().Be(ExcelEmpty.Value);
            result[1, 1].Should().Be(ExcelEmpty.Value);
            result[1, 2].Should().Be(ExcelEmpty.Value);
        }

        [Fact]
        public void Flat_too_long_truncates()
        {
            // Python: arr[:target_rows * target_cols].reshape(target_rows, target_cols)
            var flat = new object[] { 1, 2, 3, 4, 5, 6, 7, 8 };
            var result = OutputWrapper.ReshapeOutput(flat, 2, 2);
            result[0, 0].Should().Be(1);
            result[0, 1].Should().Be(2);
            result[1, 0].Should().Be(3);
            result[1, 1].Should().Be(4);
        }

        [Fact]
        public void Null_input_returns_all_empty()
        {
            // Python: np.full((rows, cols), np.nan)
            var result = OutputWrapper.ReshapeOutput(null!, 2, 2);
            result[0, 0].Should().Be(ExcelEmpty.Value);
            result[0, 1].Should().Be(ExcelEmpty.Value);
            result[1, 0].Should().Be(ExcelEmpty.Value);
            result[1, 1].Should().Be(ExcelEmpty.Value);
        }

        [Fact]
        public void Null_elements_become_empty()
        {
            var flat = new object?[] { 1, null, 3, 4 };
            var result = OutputWrapper.ReshapeOutput(flat!, 2, 2);
            result[0, 0].Should().Be(1);
            result[0, 1].Should().Be(ExcelEmpty.Value);
            result[1, 0].Should().Be(3);
            result[1, 1].Should().Be(4);
        }

        [Fact]
        public void Single_row_output()
        {
            // Python: np.array([10,20,30]).reshape(1,3)
            var flat = new object[] { 10, 20, 30 };
            var result = OutputWrapper.ReshapeOutput(flat, 1, 3);
            result[0, 0].Should().Be(10);
            result[0, 1].Should().Be(20);
            result[0, 2].Should().Be(30);
        }

        [Fact]
        public void Single_column_output()
        {
            // Python: np.array([10,20,30]).reshape(3,1)
            var flat = new object[] { 10, 20, 30 };
            var result = OutputWrapper.ReshapeOutput(flat, 3, 1);
            result[0, 0].Should().Be(10);
            result[1, 0].Should().Be(20);
            result[2, 0].Should().Be(30);
        }

        [Fact]
        public void Empty_flat_array_returns_all_empty()
        {
            var flat = Array.Empty<object>();
            var result = OutputWrapper.ReshapeOutput(flat, 2, 2);
            result[0, 0].Should().Be(ExcelEmpty.Value);
            result[0, 1].Should().Be(ExcelEmpty.Value);
            result[1, 0].Should().Be(ExcelEmpty.Value);
            result[1, 1].Should().Be(ExcelEmpty.Value);
        }
    }
}
