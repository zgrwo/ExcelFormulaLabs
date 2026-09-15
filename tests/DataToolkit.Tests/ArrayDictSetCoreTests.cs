using System;
using ExcelFormulaLabs.DataToolkit;
using ExcelFormulaLabs.Foundation;
using FluentAssertions;
#pragma warning disable CS8625 // null literal for core null-input testing
using Xunit;

namespace ExcelFormulaLabs.DataToolkit.Tests
{
    public class ArrayCoreTests
    {
        [Fact] public void Sort_asc() => ArrayCore.Sort(new object[]{3,1,4,2},true,ComparerMode.Numeric).Should().Equal(1,2,3,4);
        [Fact] public void Sort_desc() => ArrayCore.Sort(new object[]{3,1,4,2},false,ComparerMode.Numeric).Should().Equal(4,3,2,1);
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

        // =====================================================================
        // EDGE CASE & ERROR BEHAVIOR TESTS
        // =====================================================================

        [Fact] public void Sort_text_mode()
        {
            // Text sort: "10" < "2" (lexicographic)
            ArrayCore.Sort(new object[] { "a10", "a2", "a1" }, true, ComparerMode.Text).Should().Equal("a1", "a10", "a2");
        }

        [Fact] public void Sort_empty()
        {
            ArrayCore.Sort(Array.Empty<object>(), true, ComparerMode.Auto).Should().BeEmpty();
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

        [Fact] public void Slice_start_too_negative()
        {
            // start = -100 for 5-element array → clamped to 0 → returns all 5
            ArrayCore.Slice(new object[] { 1, 2, 3, 4, 5 }, -100).Should().Equal(1, 2, 3, 4, 5);
        }

        [Fact] public void Shuffle_single_element()
        {
            ArrayCore.Shuffle(new object[] { 42 }).Should().Equal(42);
        }

        [Fact] public void Shuffle_empty_array()
        {
            ArrayCore.Shuffle(Array.Empty<object>()).Should().BeEmpty();
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

        [Fact] public void Fill_basic() => ArrayCore.Fill("x", 3).Should().Equal("x", "x", "x");
        [Fact] public void Fill_zero_count() => ArrayCore.Fill("x", 0).Should().BeEmpty();
        [Fact] public void Fill_null_value()
        {
            var r = ArrayCore.Fill(null!, 2);
            r.Should().HaveCount(2);
            r[0].Should().BeNull();
        }
        [Fact] public void Fill_negative_count_throws()
            => ((Action)(() => ArrayCore.Fill("x", -1))).Should().Throw<ArgumentException>();
        [Fact] public void Fill_over_limit_throws()
            => ((Action)(() => ArrayCore.Fill("x", 100_001))).Should().Throw<ArgumentException>()
                .WithMessage("*maximum is 100000*");
        [Fact] public void Sequence_1_to_5() => ArrayCore.Sequence(1, 5, 1).Should().Equal(1.0, 2.0, 3.0, 4.0, 5.0);
        [Fact] public void Sequence_with_step() => ArrayCore.Sequence(1.0, 5.0, 2.0).Should().Equal(1.0, 3.0, 5.0);
        [Fact] public void Sequence_negative_descending() => ArrayCore.Sequence(3, 1, -1).Should().Equal(3.0, 2.0, 1.0);
        [Fact] public void Sequence_start_gt_end_asc_empty() => ArrayCore.Sequence(5, 1, 1).Should().BeEmpty();
        [Fact] public void Sequence_single() => ArrayCore.Sequence(7, 7, 1).Should().Equal(7.0);
        [Fact] public void Sequence_nan_start_empty() => ArrayCore.Sequence(double.NaN, 5, 1).Should().BeEmpty();
        // 非有限 start/end/step 统一返回空数组（与 NaN 一致）：
        // 否则会静默产生退化序列或误导性抛错。
        [Fact] public void Sequence_inf_step_empty() => ArrayCore.Sequence(0, 10, double.PositiveInfinity).Should().BeEmpty();
        [Fact] public void Sequence_inf_start_empty() => ArrayCore.Sequence(double.PositiveInfinity, 10, 1).Should().BeEmpty();

        // 浮点步长丢端点——(int)Floor(2.9999999999999996)=2
        // 使 RANGE(0,0.3,0.1) 缺 0.3；相对容差补端点且末项吸附为精确 end。
        [Fact] public void Sequence_float_step_includes_endpoint()
        {
            var r = ArrayCore.Sequence(0.0, 0.3, 0.1);
            r.Should().HaveCount(4);
            ((double)r[3]).Should().Be(0.3); // 精确端点（非 0.30000000000000004）
            ((double)r[0]).Should().Be(0.0);
        }

        [Fact] public void Sequence_float_step_descending_includes_endpoint()
        {
            var r = ArrayCore.Sequence(0.3, 0.0, -0.1);
            r.Should().HaveCount(4);
            ((double)r[3]).Should().Be(0.0);
        }

        [Fact] public void Sequence_non_dividing_step_does_not_overshoot()
        {
            // 真值 3 项（0, 0.1, 0.2）——0.25/0.1=2.5 不应补端点。
            var r = ArrayCore.Sequence(0.0, 0.25, 0.1);
            r.Should().HaveCount(3);
            ((double)r[2]).Should().BeApproximately(0.2, 1e-12);
        }
        [Fact] public void Sequence_neg_inf_end_empty() => ArrayCore.Sequence(0, double.NegativeInfinity, 1).Should().BeEmpty();
        [Fact] public void Sequence_step_zero_throws()
            => ((Action)(() => ArrayCore.Sequence(1, 3, 0))).Should().Throw<ArgumentException>();
        [Fact] public void Sequence_over_limit_throws()
            => ((Action)(() => ArrayCore.Sequence(0, 1_000_000_000, 1))).Should().Throw<ArgumentException>();
        // end-start 在有限极端值下溢出为 Inf → d=Inf；(int)d 回绕为 int.MinValue 会产生
        // 「-2147483648 elements」误导消息——超限必须抛错。
        [Fact]
        public void Sequence_extreme_range_has_sane_message()
        {
            var ex = Record.Exception(() => ArrayCore.Sequence(-double.MaxValue, double.MaxValue, 1));
            ex.Should().BeOfType<ArgumentException>();
            ex.Message.Should().NotContain("2147483648");
        }

        // R1-5：端点吸附容差不得与 |start| 同尺度。1.7e9 量级、0.7 步长的真实末项
        // 1700000099.4 距 end 0.6（旧容差 1.7 → 被错误吸附成 1700000100）。
        [Fact]
        public void Sequence_large_start_small_step_does_not_snap()
        {
            double end = 1.7e9 + 100;
            var r = ArrayCore.Sequence(1.7e9, end, 0.7);
            r.Should().HaveCount(143);
            ((double)r[142]).Should().Be(1.7e9 + 142 * 0.7);
            ((double)r[142]).Should().NotBe(end);
        }

        // R1-5 镜像：大 start 下 `end - start` 灾难性抵消（1e8+0.3-1e8=0.2999999821），
        // 计数容差须并入 start 的 ulp 粒度才能补回真实端点。
        [Fact]
        public void Sequence_large_start_cancellation_includes_endpoint()
        {
            double end = 1e8 + 0.3;
            var r = ArrayCore.Sequence(1e8, end, 0.1);
            r.Should().HaveCount(4);
            ((double)r[3]).Should().Be(end);
        }

        // R1-5 第三态：非端点可达但末项在纯浮点噪声（数 ulp）内 → 吸附精确端点。
        [Fact]
        public void Sequence_non_reachable_end_not_snapped_on_loose_tolerance()
        {
            var r = ArrayCore.Sequence(1.0, 10.0000000001, 1.0);
            r.Should().HaveCount(10);
            ((double)r[9]).Should().Be(10.0);
        }
    }
}