using ExcelFormulaLabs.Foundation;
using FluentAssertions;
using Xunit;

namespace ExcelFormulaLabs.Foundation.Tests
{
    public class ExcelEmptyTests
    {
        [Fact] public void Value_is_singleton()
        {
            ExcelEmpty.Value.Should().BeSameAs(ExcelEmpty.Value);
        }

        [Fact] public void Cannot_be_constructed_by_callers()
        {
            // 私有构造 → 只有 Value 单例可存在
            typeof(ExcelEmpty).GetConstructors(System.Reflection.BindingFlags.Instance |
                                                System.Reflection.BindingFlags.NonPublic |
                                                System.Reflection.BindingFlags.Public)
                .Should().OnlyContain(c => c.IsPrivate);
        }

        [Fact] public void ToString_returns_Empty()
        {
            ExcelEmpty.Value.ToString().Should().Be("Empty");
        }

        [Fact] public void Equals_self_true()
        {
            ExcelEmpty.Value.Equals(ExcelEmpty.Value).Should().BeTrue();
        }

        [Fact] public void Equals_other_object_false()
        {
            ExcelEmpty.Value.Equals(new object()).Should().BeFalse();
        }

        // 同引用两次取 Hash 恒真（零信息），须硬编码期望；契约是
        // `override int GetHashCode() => 0`（对齐 DBNull.Value 语义）。
        [Fact] public void GetHashCode_is_zero_contract()
        {
            ExcelEmpty.Value.GetHashCode().Should().Be(0);
        }

        [Fact] public void Distinct_from_null_empty_string_and_DBNull()
        {
            ExcelEmpty.Value.Should().NotBeNull();
            ExcelEmpty.Value.Equals("").Should().BeFalse();
            ExcelEmpty.Value.Equals(System.DBNull.Value).Should().BeFalse();
        }
    }
}
