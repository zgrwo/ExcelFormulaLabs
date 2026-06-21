using ExcelVbaLibraries.DataToolkit;
using FluentAssertions;
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
    }
}
