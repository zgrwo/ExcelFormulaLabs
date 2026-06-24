using System;
using ExcelVbaLibraries.DataToolkit;
using FluentAssertions;
#pragma warning disable CS8625 // null literal for core null-input testing
using Xunit;

namespace ExcelVbaLibraries.DataToolkit.Tests
{
    public class ArrayCoreTests
    {
        [Fact] public void Sort_asc() => ArrayCore.Sort(new object[]{3,1,4,2},true,"numeric").Should().Equal(1,2,3,4);
        [Fact] public void Sort_desc() => ArrayCore.Sort(new object[]{3,1,4,2},false,"numeric").Should().Equal(4,3,2,1);
        [Fact] public void Unique() => ArrayCore.Unique(new object[]{1,2,2,3,1}).Should().Equal(1,2,3);
        [Fact] public void IndexOf_found() => ArrayCore.IndexOf(new object[]{10,20,30},20).Should().Be(1);
        [Fact] public void IndexOf_notfound() => ArrayCore.IndexOf(new object[]{10,20,30},99).Should().Be(-1);
        [Fact] public void Slice() => ArrayCore.Slice(new object[]{1,2,3,4,5},1,3).Should().Equal(2,3,4);
        [Fact] public void Slice_negative() => ArrayCore.Slice(new object[]{1,2,3,4,5},-2).Should().Equal(4,5);
        [Fact] public void Filter_eq() => ArrayCore.Filter(new object[]{1,2,3,2,4},2,"=").Should().Equal(2,2);
        [Fact] public void Filter_gt() => ArrayCore.Filter(new object[]{1,5,2,8,3},3,">").Should().Equal(5,8);
        [Fact] public void Concat() => ArrayCore.Concat(new object[]{1,2},new object[]{3,4}).Should().Equal(1,2,3,4);
        [Fact] public void Reverse() => ArrayCore.Reverse(new object[]{1,2,3}).Should().Equal(3,2,1);
        [Fact] public void Contains_true() => ArrayCore.Contains(new object[]{1,2,3},2).Should().BeTrue();
        [Fact] public void Count() => ArrayCore.Count(new object[]{1,2,3}).Should().Be(3);
        [Fact] public void Flatten2D()
        {
            var d = new object[,] { { 1, 2 }, { 3, 4 }, { 5, 6 } };
            ArrayCore.Flatten2D(d).Should().Equal(1, 2, 3, 4, 5, 6);
        }
        [Fact] public void Flatten2D_colMajor()
        {
            var d = new object[,] { { 1, 2 }, { 3, 4 }, { 5, 6 } };
            ArrayCore.Flatten2D(d, "C").Should().Equal(1, 3, 5, 2, 4, 6);
        }
        [Fact] public void Flatten2D_empty() => ArrayCore.Flatten2D(new object[0, 0]).Should().BeEmpty();
        [Fact] public void CollectNumeric()
        {
            var d = new object[,] { { "Name", "Score", "Note" }, { "Alice", 90, "Good" }, { "Bob", 80, "Ok" } };
            var r = ArrayCore.CollectNumeric(d, 3, 3, out var names);
            r.Should().Equal(1L);
            names.Should().Equal("Name", "Score", "Note");
        }
        [Fact] public void CollectNumeric_noHeader()
        {
            var d = new object[,] { { 1, "text" }, { 2, "more" } };
            var r = ArrayCore.CollectNumeric(d, 2, 2, out var names, false);
            r.Should().Equal(0L);
        }

        // =====================================================================
        // EDGE CASE & ERROR BEHAVIOR TESTS
        // =====================================================================

        [Fact] public void Sort_text_mode()
        {
            // Text sort: "10" < "2" (lexicographic)
            ArrayCore.Sort(new object[] { "a10", "a2", "a1" }, true, "text").Should().Equal("a1", "a10", "a2");
        }

        [Fact] public void Sort_empty()
        {
            ArrayCore.Sort(Array.Empty<object>(), true, "auto").Should().BeEmpty();
        }

        [Fact] public void Unique_empty()
        {
            ArrayCore.Unique(Array.Empty<object>()).Should().BeEmpty();
        }

        [Fact] public void Unique_single()
        {
            ArrayCore.Unique(new object[] { 42 }).Should().Equal(42);
        }

        [Fact] public void Unique_mixed_types()
        {
            // Numeric 1 and string "1" are different via SafeKey
            var r = ArrayCore.Unique(new object[] { 1, "1", 1 });
            r.Should().Equal(1, "1");
        }

        [Fact] public void Filter_contains()
        {
            ArrayCore.Filter(new object[] { "Hello", "World", "HELLO" }, "ell", "contains")
                .Should().Equal("Hello", "HELLO");
        }

        [Fact] public void Filter_startswith()
        {
            ArrayCore.Filter(new object[] { "apple", "banana", "apricot" }, "ap", "startswith")
                .Should().Equal("apple", "apricot");
        }

        [Fact] public void Filter_endswith()
        {
            ArrayCore.Filter(new object[] { "running", "walking", "run" }, "ing", "endswith")
                .Should().Equal("running", "walking");
        }

        [Fact] public void Filter_regex()
        {
            ArrayCore.Filter(new object[] { "abc123", "xyz", "def456" }, @"\d+", "regex")
                .Should().Equal("abc123", "def456");
        }

        [Fact] public void Filter_no_match_returns_empty()
        {
            ArrayCore.Filter(new object[] { 1, 2, 3 }, 99, "=").Should().BeEmpty();
        }

        [Fact] public void Reverse_empty()
        {
            ArrayCore.Reverse(Array.Empty<object>()).Should().BeEmpty();
        }

        [Fact] public void Reverse_single()
        {
            ArrayCore.Reverse(new object[] { 42 }).Should().Equal(42);
        }

        [Fact] public void Concat_first_empty()
        {
            ArrayCore.Concat(Array.Empty<object>(), new object[] { 1, 2 }).Should().Equal(1, 2);
        }

        [Fact] public void Concat_second_empty()
        {
            ArrayCore.Concat(new object[] { 1, 2 }, Array.Empty<object>()).Should().Equal(1, 2);
        }

        [Fact] public void Contains_false()
        {
            ArrayCore.Contains(new object[] { 1, 2, 3 }, 99).Should().BeFalse();
        }

        [Fact] public void Slice_out_of_bounds_returns_empty()
        {
            ArrayCore.Slice(new object[] { 1, 2, 3 }, 10).Should().BeEmpty();
        }

        [Fact] public void Filter_isblank()
        {
            ArrayCore.Filter(new object[] { "hello", "", "   ", "world" }, null, "isblank")
                .Should().Equal("", "   ");
        }
    }

    public class DictSetCoreTests
    {
        [Fact] public void Frequency() { var r=DictSetCore.Frequency(new object[]{"a","b","a","c","b","a"}); r[0,0].Should().Be("a"); r[0,1].Should().Be(3L); }
        [Fact] public void Intersect() => DictSetCore.Intersect(new object[]{1,2,3,4},new object[]{3,4,5,6}).Should().Equal(3,4);
        [Fact] public void Union() => DictSetCore.Union(new object[]{1,2},new object[]{2,3}).Should().Equal(1,2,3);
        [Fact] public void Except() => DictSetCore.Except(new object[]{1,2,3,4},new object[]{2,4}).Should().Equal(1,3);
        [Fact] public void Dict() => DictSetCore.Dict(new object[]{"k1","k2"},new object[]{1,2}).GetLength(0).Should().Be(2);
        [Fact] public void Count()
        {
            var d = new object[,] { { "k1", 1 }, { "k2", 2 } };
            DictSetCore.Count(d).Should().Be(2);
        }

        // =====================================================================
        // EDGE CASE & ERROR BEHAVIOR TESTS
        // =====================================================================

        [Fact] public void Frequency_empty()
        {
            var r = DictSetCore.Frequency(Array.Empty<object>());
            r.GetLength(0).Should().Be(0);
        }

        [Fact] public void Frequency_single()
        {
            var r = DictSetCore.Frequency(new object[] { "x" });
            r[0, 0].Should().Be("x");
            r[0, 1].Should().Be(1L);
        }

        [Fact] public void Frequency_all_unique()
        {
            var r = DictSetCore.Frequency(new object[] { "a", "b", "c" });
            r.GetLength(0).Should().Be(3);
            // Each count = 1
            r[0, 1].Should().Be(1L);
            r[1, 1].Should().Be(1L);
            r[2, 1].Should().Be(1L);
        }

        [Fact] public void Intersect_no_overlap()
        {
            DictSetCore.Intersect(new object[] { 1, 2 }, new object[] { 3, 4 }).Should().BeEmpty();
        }

        [Fact] public void Intersect_empty_second()
        {
            DictSetCore.Intersect(new object[] { 1, 2 }, Array.Empty<object>()).Should().BeEmpty();
        }

        [Fact] public void Intersect_empty_first()
        {
            DictSetCore.Intersect(Array.Empty<object>(), new object[] { 1, 2 }).Should().BeEmpty();
        }

        [Fact] public void Union_empty_both()
        {
            DictSetCore.Union(Array.Empty<object>(), Array.Empty<object>()).Should().BeEmpty();
        }

        [Fact] public void Union_overlapping()
        {
            // Duplicate elements in first array should be deduplicated too
            var r = DictSetCore.Union(new object[] { 1, 1, 2 }, new object[] { 2, 3 });
            r.Should().Equal(1, 2, 3);
        }

        [Fact] public void Except_no_removal()
        {
            DictSetCore.Except(new object[] { 1, 2, 3 }, new object[] { 4, 5 }).Should().Equal(1, 2, 3);
        }

        [Fact] public void Except_all_removed()
        {
            DictSetCore.Except(new object[] { 1, 2 }, new object[] { 1, 2, 3 }).Should().BeEmpty();
        }

        [Fact] public void Dict_mismatched_lengths()
        {
            // Keys longer than values → truncated to values length
            var r = DictSetCore.Dict(new object[] { "a", "b", "c" }, new object[] { 1 });
            r.GetLength(0).Should().Be(1);
        }

        [Fact] public void Dict_empty_arrays()
        {
            var r = DictSetCore.Dict(Array.Empty<object>(), Array.Empty<object>());
            r.GetLength(0).Should().Be(0);
        }
    }
}
