using System;
using ExcelVbaLibraries.Foundation;
using FluentAssertions;
using Xunit;

namespace ExcelVbaLibraries.Foundation.Tests;

public class NumericGuardMatrixTests
{
    [Fact] public void Finite_matrix_passes() { var m = new double[,] { { 1.0, 2.0 }, { 3.0, 4.0 } }; var a = () => NumericGuard.AgainstNonFinite(m); a.Should().NotThrow(); }
    [Fact] public void NaN_throws_with_position() { var m = new double[,] { { 1.0, double.NaN } }; var a = () => NumericGuard.AgainstNonFinite(m); a.Should().Throw<ArgumentException>().WithMessage("*NaN*[0,1]*"); }
    [Fact] public void PositiveInfinity_throws() { var m = new double[,] { { double.PositiveInfinity } }; var a = () => NumericGuard.AgainstNonFinite(m); a.Should().Throw<ArgumentException>().WithMessage("*Infinity*[0,0]*"); }
    [Fact] public void NegativeInfinity_throws() { var m = new double[,] { { 1.0 }, { double.NegativeInfinity } }; var a = () => NumericGuard.AgainstNonFinite(m); a.Should().Throw<ArgumentException>().WithMessage("*Infinity*[1,0]*"); }
    [Fact] public void Empty_matrix_passes() { var m = new double[0, 0]; var a = () => NumericGuard.AgainstNonFinite(m); a.Should().NotThrow(); }
    [Fact] public void Single_finite_element_passes() { var m = new double[,] { { 42.0 } }; var a = () => NumericGuard.AgainstNonFinite(m); a.Should().NotThrow(); }
    [Fact] public void First_element_is_checked() { var m = new double[,] { { double.NaN, 1.0 }, { 2.0, 3.0 } }; var a = () => NumericGuard.AgainstNonFinite(m); a.Should().Throw<ArgumentException>().WithMessage("*[0,0]*"); }
    [Fact] public void Last_element_is_checked() { var m = new double[,] { { 1.0, 2.0 }, { 3.0, double.NaN } }; var a = () => NumericGuard.AgainstNonFinite(m); a.Should().Throw<ArgumentException>().WithMessage("*[1,1]*"); }
    [Fact] public void All_finite_values_pass() { var m = new double[100, 100]; for (int r = 0; r < 100; r++) for (int c = 0; c < 100; c++) m[r, c] = r * 100 + c; var a = () => NumericGuard.AgainstNonFinite(m); a.Should().NotThrow(); }
}

public class NumericGuardMatrixVectorTests
{
    [Fact] public void Both_finite_pass() { var m = new double[,] { { 1.0 } }; var v = new[] { 2.0 }; var a = () => NumericGuard.AgainstNonFinite(m, v); a.Should().NotThrow(); }
    [Fact] public void NaN_in_matrix_throws() { var m = new double[,] { { double.NaN } }; var v = new[] { 1.0 }; var a = () => NumericGuard.AgainstNonFinite(m, v); a.Should().Throw<ArgumentException>().WithMessage("*[0,0]*"); }
    [Fact] public void NaN_in_vector_throws() { var m = new double[,] { { 1.0 } }; var v = new[] { double.NaN }; var a = () => NumericGuard.AgainstNonFinite(m, v); a.Should().Throw<ArgumentException>().WithMessage("*index 0*"); }
    [Fact] public void Inf_in_vector_throws() { var m = new double[,] { { 1.0 } }; var v = new[] { 1.0, double.PositiveInfinity }; var a = () => NumericGuard.AgainstNonFinite(m, v); a.Should().Throw<ArgumentException>().WithMessage("*Infinity*index 1*"); }
    [Fact] public void Empty_vector_passes() { var m = new double[,] { { 1.0 } }; var v = Array.Empty<double>(); var a = () => NumericGuard.AgainstNonFinite(m, v); a.Should().NotThrow(); }
    [Fact] public void Matrix_error_supersedes_vector() { var m = new double[,] { { double.NaN } }; var v = new[] { double.NaN }; var a = () => NumericGuard.AgainstNonFinite(m, v); a.Should().Throw<ArgumentException>().WithMessage("*[0,0]*"); }
}

public class NumericGuardScalarTests
{
    [Fact] public void Finite_value_passes() { var a = () => NumericGuard.AgainstNonFinite(42.0, "testParam"); a.Should().NotThrow(); }
    [Fact] public void NaN_throws_with_param_name() { var a = () => NumericGuard.AgainstNonFinite(double.NaN, "lambda"); a.Should().Throw<ArgumentException>().WithMessage("*lambda*"); }
    [Fact] public void PositiveInfinity_throws() { var a = () => NumericGuard.AgainstNonFinite(double.PositiveInfinity, "weight"); a.Should().Throw<ArgumentException>(); }
    [Fact] public void NegativeInfinity_throws() { var a = () => NumericGuard.AgainstNonFinite(double.NegativeInfinity, "p"); a.Should().Throw<ArgumentException>(); }
    [Fact] public void Zero_passes() { var a = () => NumericGuard.AgainstNonFinite(0.0, "x"); a.Should().NotThrow(); }
    [Fact] public void DoubleMinValue_passes() { var a = () => NumericGuard.AgainstNonFinite(double.MinValue, "x"); a.Should().NotThrow(); }
    [Fact] public void DoubleMaxValue_passes() { var a = () => NumericGuard.AgainstNonFinite(double.MaxValue, "x"); a.Should().NotThrow(); }
    [Fact] public void Epsilon_passes() { var a = () => NumericGuard.AgainstNonFinite(double.Epsilon, "x"); a.Should().NotThrow(); }
}
