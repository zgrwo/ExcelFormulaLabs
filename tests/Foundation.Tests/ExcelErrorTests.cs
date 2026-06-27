using FormulaLabs.Foundation;
using FluentAssertions;
using Xunit;

namespace FormulaLabs.Foundation.Tests
{
    public class ExcelErrorTests
    {
        [Fact] public void Value_ErrorName_is_VALUE()
            => ExcelError.Value.ErrorName.Should().Be("#VALUE!");
        [Fact] public void NA_ErrorName_is_NA()
            => ExcelError.NA.ErrorName.Should().Be("#N/A");
        [Fact] public void Div0_ErrorName_is_DIV0()
            => ExcelError.Div0.ErrorName.Should().Be("#DIV/0!");
        [Fact] public void Null_ErrorName_is_NULL()
            => ExcelError.Null.ErrorName.Should().Be("#NULL!");
        [Fact] public void Ref_ErrorName_is_REF()
            => ExcelError.Ref.ErrorName.Should().Be("#REF!");
        [Fact] public void Name_ErrorName_is_NAME()
            => ExcelError.Name.ErrorName.Should().Be("#NAME?");
        [Fact] public void Num_ErrorName_is_NUM()
            => ExcelError.Num.ErrorName.Should().Be("#NUM!");

        [Fact] public void Unknown_code_returns_ERR_format()
            => new ExcelError(9999).ErrorName.Should().Be("#ERR(9999)");

        [Fact] public void ToString_delegates_to_ErrorName()
        {
            ExcelError.Value.ToString().Should().Be("#VALUE!");
            ExcelError.NA.ToString().Should().Be("#N/A");
            new ExcelError(9999).ToString().Should().Be("#ERR(9999)");
        }

        [Fact] public void ErrorName_does_not_affect_equality()
        {
            var a = new ExcelError(2015);
            var b = ExcelError.Value;
            a.Should().Be(b);
            a.ErrorName.Should().Be("#VALUE!");
            b.ErrorName.Should().Be("#VALUE!");
        }
    }
}
