using System;
using DnaError = ExcelDna.Integration.ExcelError;
using ExcelFormulaLabs.Foundation;
using FluentAssertions;
using Xunit;

namespace ExcelFormulaLabs.Foundation.Tests
{
    /// <summary>
    /// Regression tests for P0-1 (pre-release review): real Excel error cells
    /// arrive as ExcelDna.Integration.ExcelError (an enum), which Foundation
    /// previously treated as an unknown object — silently converting
    /// #VALUE!→15.0, #DIV/0!→7.0, #N/A→42.0 and feeding those numbers into
    /// computations. Per L3 sentinel contract, error signals must never be
    /// silently assigned; they are rejected by converters and passed through
    /// by MapOver.
    /// </summary>
    public class ExcelErrorInteropTests
    {
        // ── Converters return sentinels, not the enum's underlying number ──

        [Fact]
        public void ToDouble_excel_dna_error_returns_NaN()
            => InputNormalizer.ToDouble(DnaError.ExcelErrorValue).Should().Be(double.NaN);

        [Fact]
        public void ToString_excel_dna_error_returns_empty()
            => InputNormalizer.ToString(DnaError.ExcelErrorValue).Should().Be("");

        [Fact]
        public void ToLong_excel_dna_error_returns_zero()
            => InputNormalizer.ToLong(DnaError.ExcelErrorValue).Should().Be(0);

        [Fact]
        public void ToBool_excel_dna_error_returns_false()
            => InputNormalizer.ToBool(DnaError.ExcelErrorValue).Should().BeFalse();

        [Fact]
        public void ToBool_with_sentinel_excel_dna_error_returns_sentinel()
            => InputNormalizer.ToBool(DnaError.ExcelErrorValue, true).Should().BeTrue();

        [Fact]
        public void ToDateTime_excel_dna_error_returns_min_value()
            => InputNormalizer.ToDateTime(DnaError.ExcelErrorValue).Should().Be(DateTime.MinValue);

        [Fact]
        public void IsNumericCell_excel_dna_error_returns_false()
            => InputNormalizer.IsNumericCell(DnaError.ExcelErrorValue).Should().BeFalse();

        [Fact]
        public void IsExcelErrorValue_detects_both_sentinel_types()
        {
            InputNormalizer.IsExcelErrorValue(DnaError.ExcelErrorValue).Should().BeTrue();
            InputNormalizer.IsExcelErrorValue(new ExcelFormulaLabs.Foundation.ExcelError(2015)).Should().BeTrue();
            InputNormalizer.IsExcelErrorValue("text").Should().BeFalse();
            InputNormalizer.IsExcelErrorValue(42.0).Should().BeFalse();
            InputNormalizer.IsExcelErrorValue(null).Should().BeFalse();
        }

        // ── MapOver passes the error through instead of computing with it ──

        [Fact]
        public void MapOver_single_excel_dna_error_passthrough()
        {
            object result = ElementWiseMapper.MapOver<double, double>(
                DnaError.ExcelErrorValue, x => x * 2);
            result.Should().Be(DnaError.ExcelErrorValue);
        }

        [Fact]
        public void MapOverFlat_excel_dna_error_passthrough()
        {
            object[] result = ElementWiseMapper.MapOverFlat<double, double>(
                DnaError.ExcelErrorValue, x => x * 2);
            result.Should().HaveCount(1);
            result[0].Should().Be(DnaError.ExcelErrorValue);
        }

        [Fact]
        public void MapOverMulti_excel_dna_error_passthrough()
        {
            object result = ElementWiseMapper.MapOverMulti<string, string, bool>(
                DnaError.ExcelErrorValue, "abc", (a, b) => true);
            result.Should().Be(DnaError.ExcelErrorValue);
        }

        [Fact]
        public void MapOverMulti_second_arg_excel_dna_error_passthrough()
        {
            object result = ElementWiseMapper.MapOverMulti<string, string, bool>(
                "abc", DnaError.ExcelErrorDiv0, (a, b) => true);
            result.Should().Be(DnaError.ExcelErrorDiv0);
        }

        [Fact]
        public void MapOver_array_containing_excel_dna_error_passthrough_elements()
        {
            object result = ElementWiseMapper.MapOver<double, double>(
                new object[,] { { 1.0 }, { DnaError.ExcelErrorNA } }, x => x * 10);
            result.Should().BeOfType<object[,]>();
            var arr = (object[,])result;
            arr[0, 0].Should().Be(10.0);
            arr[1, 0].Should().Be(DnaError.ExcelErrorNA);
        }

        // ── Filter / Compare / Dict treat the enum as an error signal ──

        [Fact]
        public void FilterPasses_excel_dna_error_rejected()
        {
            // Previously the enum converted to 15.0 and compared equal to 15.
            FilterUtils.FilterPasses(DnaError.ExcelErrorValue, 15.0, "=").Should().BeFalse();
            FilterUtils.FilterPasses(DnaError.ExcelErrorValue, 5.0, ">").Should().BeFalse();
        }

        [Fact]
        public void ValuesEqual_excel_dna_error_vs_number_false()
        {
            // Previously 15 (underlying #VALUE! code) == 15.0 → true.
            ComparisonUtils.ValuesEqual(DnaError.ExcelErrorValue, 15.0).Should().BeFalse();
        }

        [Fact]
        public void ValuesEqual_two_excel_dna_errors_true()
        {
            // Different error codes are still "both errors" for equality purposes.
            ComparisonUtils.ValuesEqual(DnaError.ExcelErrorValue, DnaError.ExcelErrorDiv0).Should().BeTrue();
        }

        [Fact]
        public void Compare_excel_dna_error_sorts_last()
        {
            ComparisonUtils.Compare(DnaError.ExcelErrorValue, 1.0).Should().BeGreaterThan(0);
            ComparisonUtils.Compare(1.0, DnaError.ExcelErrorValue).Should().BeLessThan(0);
            ComparisonUtils.Compare(DnaError.ExcelErrorValue, DnaError.ExcelErrorDiv0).Should().Be(0);
        }

        [Fact]
        public void FromKeys_skips_excel_dna_error_keys()
        {
            var dict = DictOperations.FromKeys(new object[] { DnaError.ExcelErrorValue, "keep" });
            dict.ContainsKey("ExcelErrorValue").Should().BeFalse();
            dict.ContainsKey("keep").Should().BeTrue();
        }
    }
}