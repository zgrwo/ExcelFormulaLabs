using ExcelFormulaLabs.Foundation;
using FluentAssertions;
using Xunit;

namespace ExcelFormulaLabs.Foundation.Tests
{
    /// <summary>review-2026-08-29 P2-5：ErrorMsg 此前 0% 行覆盖、无专用测试。</summary>
    public class ErrorMsgTests
    {
        [Fact] public void Get_existing_key_returns_message()
        {
            // Input_NullOrEmpty 是 resx 中既有键
            ErrorMsg.Get("Input_NullOrEmpty").Should().NotBeNullOrEmpty();
        }

        [Fact] public void Get_with_format_args_substitutes_placeholders()
        {
            var msg = ErrorMsg.Get("ARR_CountOutOfRange", 100001, 100000);
            msg.Should().Contain("100001").And.Contain("100000");
        }

        [Fact] public void Get_with_multiple_args_orders_placeholders()
        {
            // Input_IntOutOfRange: "Value {0} is outside the int range [{1}, {2}]."
            var msg = ErrorMsg.Get("Input_IntOutOfRange", 3000000000L, int.MinValue, int.MaxValue);
            msg.Should().Contain("3000000000")
                .And.Contain(int.MinValue.ToString())
                .And.Contain(int.MaxValue.ToString());
        }

        [Fact] public void Get_missing_key_falls_back_to_key_name()
        {
            // fail-safe：缺失键返回键名本身，绝不返回 null
            const string missing = "NONEXISTENT_Key_XYZ";
            ErrorMsg.Get(missing).Should().Be(missing);
        }

        [Fact] public void Get_missing_key_with_args_still_non_null()
        {
            ErrorMsg.Get("NONEXISTENT_Key_XYZ", 1, 2).Should().NotBeNull();
        }
    }
}
