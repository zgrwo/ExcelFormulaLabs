using System;
using System.Collections.Generic;
using System.Linq;
using ExcelVbaLibraries.Analytics;
using ExcelVbaLibraries.DataToolkit;
using ExcelVbaLibraries.Foundation;
using FluentAssertions;
using Xunit;

namespace ExcelVbaLibraries.DataToolkit.Tests
{
    /// <summary>
    /// Cross-module integration tests verifying that multiple toolkit modules
    /// compose correctly in end-to-end data-processing pipelines.
    /// </summary>
    /// <summary>
    /// Integration pipeline tests: C# module composition (String→Stats, Array→Stats, etc.).
    /// Not cross-validated against Python — see StatsCoreTests for Python-verified constants.
    /// </summary>
    public class IntegrationPipelineTests
    {
        // ═══════════════════════════════════════════════════════════════════
        // 1. String-to-Stats Pipeline
        // ═══════════════════════════════════════════════════════════════════

        [Fact]
        public void StringToStats_NumericStrings_Mean()
        {
            var input = new object[] { "1.5", "2.5", "3.5" };
            var doubles = InputNormalizer.ToDoubles(input);
            doubles.Should().Equal(1.5, 2.5, 3.5);
            var mean = StatsCore.Mean(doubles);
            mean.Should().BeApproximately(2.5, 0.0001);
        }

        [Fact]
        public void StringToStats_NumericStrings_Stdev()
        {
            var input = new object[] { "1.5", "2.5", "3.5" };
            var doubles = InputNormalizer.ToDoubles(input);
            var stdev = StatsCore.Stdev(doubles);
            stdev.Should().BeApproximately(1.0, 0.0001);
        }

        [Fact]
        public void StringToStats_MixedValidAndInvalid_ValidExtracted()
        {
            var input = new object[] { "1.5", "abc", "3.5" };
            var doubles = InputNormalizer.ToDoubles(input);
            doubles.Should().Equal(1.5, 3.5);
            var mean = StatsCore.Mean(doubles);
            mean.Should().BeApproximately(2.5, 0.0001);
        }

        [Fact]
        public void StringToStats_AllInvalid_EmptyDoubles_MeanIsNaN()
        {
            var input = new object[] { "abc", "def" };
            var doubles = InputNormalizer.ToDoubles(input);
            doubles.Should().BeEmpty();
            var mean = StatsCore.Mean(doubles);
            double.IsNaN(mean).Should().BeTrue();
        }

        [Fact]
        public void StringToStats_VarianceOfNumericStrings()
        {
            var input = new object[] { "1.0", "2.0", "3.0", "4.0" };
            var doubles = InputNormalizer.ToDoubles(input);
            var variance = StatsCore.Variance(doubles);
            variance.Should().BeApproximately(1.6666666666666667, 0.0001);
        }

        // ═══════════════════════════════════════════════════════════════════
        // 2. Date-Sort Pipeline
        // ═══════════════════════════════════════════════════════════════════

        [Fact]
        public void DateSort_Ascending()
        {
            var dateStrings = new object[] { "2024-01-15", "2023-06-20", "2024-12-01" };
            var dates = dateStrings
                .Select(s => (object)InputNormalizer.ToDateTime(s))
                .ToArray();
            var sorted = ArrayCore.Sort(dates, true, "auto");
            ((DateTime)sorted[0]).Should().Be(new DateTime(2023, 6, 20));
            ((DateTime)sorted[1]).Should().Be(new DateTime(2024, 1, 15));
            ((DateTime)sorted[2]).Should().Be(new DateTime(2024, 12, 1));
        }

        [Fact]
        public void DateSort_Descending()
        {
            var dateStrings = new object[] { "2024-01-15", "2023-06-20", "2024-12-01" };
            var dates = dateStrings
                .Select(s => (object)InputNormalizer.ToDateTime(s))
                .ToArray();
            var sorted = ArrayCore.Sort(dates, false, "auto");
            ((DateTime)sorted[0]).Should().Be(new DateTime(2024, 12, 1));
            ((DateTime)sorted[1]).Should().Be(new DateTime(2024, 1, 15));
            ((DateTime)sorted[2]).Should().Be(new DateTime(2023, 6, 20));
        }

        [Fact]
        public void DateSort_InvalidDates_BecomeMinValue_SortFirst()
        {
            var dateStrings = new object[] { "not-a-date", "2024-01-15", "garbage" };
            var dates = dateStrings
                .Select(s => (object)InputNormalizer.ToDateTime(s))
                .ToArray();
            // Sort ascending: MinValue dates should appear first
            var sorted = ArrayCore.Sort(dates, true, "auto");
            ((DateTime)sorted[0]).Should().Be(DateTime.MinValue);
            ((DateTime)sorted[1]).Should().Be(DateTime.MinValue);
            ((DateTime)sorted[2]).Should().Be(new DateTime(2024, 1, 15));
        }

        // ═══════════════════════════════════════════════════════════════════
        // 3. Regex-Filter-to-Stats Pipeline
        // ═══════════════════════════════════════════════════════════════════

        [Fact]
        public void RegexFilter_FilterThenCount()
        {
            var input = new object[] { "a1", "b2", "c", "d3" };
            var filtered = ArrayCore.Filter(input, @"\d", "regex");
            filtered.Should().Equal("a1", "b2", "d3");
            ArrayCore.Count(filtered).Should().Be(3);
        }

        [Fact]
        public void RegexFilter_FilterPureDigits_ThenStats()
        {
            var input = new object[] { "10", "20", "x30", "40", "y" };
            var filtered = ArrayCore.Filter(input, @"\d", "regex");
            filtered.Should().Equal("10", "20", "x30", "40");
            // x30 is not a pure numeric string → ToDoubles skips it
            var doubles = InputNormalizer.ToDoubles(filtered);
            doubles.Should().Equal(10, 20, 40);
            StatsCore.Mean(doubles).Should().BeApproximately(70.0 / 3.0, 0.0001);
        }

        [Fact]
        public void RegexFilter_NoMatch_ProducesEmptyPipeline()
        {
            var input = new object[] { "abc", "def" };
            var filtered = ArrayCore.Filter(input, @"\d", "regex");
            filtered.Should().BeEmpty();
            var doubles = InputNormalizer.ToDoubles(filtered);
            doubles.Should().BeEmpty();
            double.IsNaN(StatsCore.Mean(doubles)).Should().BeTrue();
        }

        // ═══════════════════════════════════════════════════════════════════
        // 4. JSON-to-String Pipeline
        // ═══════════════════════════════════════════════════════════════════

        [Fact]
        public void JsonToString_ReverseStringValues()
        {
            var json = @"{""name"":""Alice"",""city"":""New York"",""age"":30}";
            var parsed = JsonXmlCore.JsonParse(json);
            parsed.Should().NotBeNull();
            var dict = (Dictionary<string, object?>)parsed!;
            // Reverse all string values in-place
            var keys = dict.Keys.ToArray();
            foreach (var key in keys)
            {
                if (dict[key] is string s)
                    dict[key] = StringCore.ReverseString(s);
            }
            dict["name"].Should().Be("ecilA");
            dict["city"].Should().Be("kroY weN");
            // Non-string values are left unchanged
            dict["age"].Should().Be(30L);
        }

        [Fact]
        public void JsonToString_ExtractStrings_Join()
        {
            var json = @"{""a"":""hello"",""b"":""world"",""c"":42}";
            var parsed = JsonXmlCore.JsonParse(json);
            parsed.Should().NotBeNull();
            var dict = (Dictionary<string, object?>)parsed!;
            var stringValues = dict.Values.OfType<string>().ToArray();
            var joined = StringCore.TextJoin(" ", false, stringValues);
            joined.Should().Be("hello world");
        }

        [Fact]
        public void JsonToString_ParseAndCountStrings()
        {
            var json = @"{""x"":""one"",""y"":2,""z"":""two""}";
            var parsed = JsonXmlCore.JsonParse(json);
            parsed.Should().NotBeNull();
            var dict = (Dictionary<string, object?>)parsed!;
            var stringCount = dict.Values.OfType<string>().Count();
            stringCount.Should().Be(2);
        }

        // ═══════════════════════════════════════════════════════════════════
        // 5. Soundex-Grouping Pipeline
        // ═══════════════════════════════════════════════════════════════════

        [Fact]
        public void SoundexGrouping_Frequency()
        {
            var names = new object[] { "Robert", "Rupert", "Smith", "Smythe" };
            var soundexCodes = names
                .Select(n => (object)StringCore.Soundex((string)n))
                .ToArray();
            // Verify individual Soundex outputs
            soundexCodes[0].Should().Be("R163");
            soundexCodes[1].Should().Be("R163");
            soundexCodes[2].Should().Be("S530");
            soundexCodes[3].Should().Be("S530");
            // Frequency: R163 appears 2 times, S530 appears 2 times
            var freq = DictSetCore.Frequency(soundexCodes);
            freq.GetLength(0).Should().Be(2);
            // Order-independent verification (Dictionary iteration order is not guaranteed)
            var entries = new List<(string Key, long Count)>();
            for (int i = 0; i < freq.GetLength(0); i++)
                entries.Add(((string)freq[i, 0], (long)freq[i, 1]));
            entries.Should().Contain(e => e.Key == "R163" && e.Count == 2L);
            entries.Should().Contain(e => e.Key == "S530" && e.Count == 2L);
        }

        [Fact]
        public void SoundexGrouping_SingleName()
        {
            var names = new object[] { "Single" };
            var soundexCodes = names
                .Select(n => (object)StringCore.Soundex((string)n))
                .ToArray();
            soundexCodes[0].Should().Be("S524");
            var freq = DictSetCore.Frequency(soundexCodes);
            freq.GetLength(0).Should().Be(1);
            ((string)freq[0, 0]).Should().Be("S524");
            ((long)freq[0, 1]).Should().Be(1L);
        }

        [Fact]
        public void SoundexGrouping_EmptyName_ReturnsEmptySoundex()
        {
            StringCore.Soundex("").Should().Be("");
            // Null input is handled by Soundex (string.IsNullOrEmpty returns true for null)
        }

        // ═══════════════════════════════════════════════════════════════════
        // 6. Array-Slice-Sort Pipeline
        // ═══════════════════════════════════════════════════════════════════

        [Fact]
        public void ArraySliceSort_SliceThenSortAscending()
        {
            var input = new object[] { 5, 3, 1, 4, 2 };
            var sliced = ArrayCore.Slice(input, 0, 3);
            sliced.Should().Equal(5, 3, 1);
            var sorted = ArrayCore.Sort(sliced, true, "numeric");
            sorted.Should().Equal(1, 3, 5);
        }

        [Fact]
        public void ArraySliceSort_SliceThenSortDescending()
        {
            var input = new object[] { 5, 3, 1, 4, 2 };
            var sliced = ArrayCore.Slice(input, 1, 3);
            sliced.Should().Equal(3, 1, 4);
            var sorted = ArrayCore.Sort(sliced, false, "numeric");
            sorted.Should().Equal(4, 3, 1);
        }

        [Fact]
        public void ArraySliceSort_SliceFullThenSort()
        {
            var input = new object[] { 100, 50, 75, 25 };
            var sliced = ArrayCore.Slice(input, 0);
            sliced.Should().Equal(100, 50, 75, 25);
            var sorted = ArrayCore.Sort(sliced, true, "numeric");
            sorted.Should().Equal(25, 50, 75, 100);
        }

        // ═══════════════════════════════════════════════════════════════════
        // 7. Cross-Module Error Propagation
        // ═══════════════════════════════════════════════════════════════════

        [Fact]
        public void ErrorPropagation_ExcelError_Through_ToDouble_ReturnsNaN()
        {
            var result = InputNormalizer.ToDouble(ExcelError.NA);
            double.IsNaN(result).Should().BeTrue();
        }

        [Fact]
        public void ErrorPropagation_Null_Through_ToDouble_ReturnsNaN()
        {
            var result = InputNormalizer.ToDouble(null);
            double.IsNaN(result).Should().BeTrue();
        }

        [Fact]
        public void ErrorPropagation_ExcelError_Through_ToDoubles_Skipped()
        {
            var input = new object[] { ExcelError.NA, ExcelError.Value, 1.5 };
            var doubles = InputNormalizer.ToDoubles(input);
            doubles.Should().Equal(1.5);
        }

        [Fact]
        public void ErrorPropagation_ExcelError_Through_MapOver_Preserved()
        {
            var input = new object[] { ExcelError.NA, "hello" };
            var result = (object[])ElementWiseMapper.MapOver<string, string>(
                input, s => s.ToUpper());
            result[0].Should().BeOfType<ExcelError>();
            ((ExcelError)result[0]).Code.Should().Be(2042);
            result[1].Should().Be("HELLO");
        }

        [Fact]
        public void ErrorPropagation_NullAndEmpty_Through_MapOver_Propagated()
        {
            object?[] input = [null, "world"];
            var result = (object[])ElementWiseMapper.MapOver<string, string>(
                input, s => s.ToUpper());
            result[0].Should().BeNull();
            result[1].Should().Be("WORLD");
        }
    }
}
