using System;
using ExcelVbaLibraries.Analytics;
using FluentAssertions;
using Xunit;

namespace ExcelVbaLibraries.Analytics.Tests
{
    public class RegressionCoreTests
    {
        private static readonly double[,] X = {{1,1},{1,2},{1,3}};
        private static readonly double[] y = {5,8,11};
        [Fact] public void FitOLS_keys() => RegressionCore.FitOLS(X,y).Should().ContainKeys("coefficients","sse","r_squared","t_stats","p_values");
        [Fact] public void FitOLS_r2() => ((double)RegressionCore.FitOLS(X,y)["r_squared"]).Should().BeApproximately(1.0,1e-10);
        [Fact] public void FitOLS_coef() { var c=(double[])RegressionCore.FitOLS(X,y)["coefficients"]; c[0].Should().BeApproximately(2.0,1e-8); c[1].Should().BeApproximately(3.0,1e-8); }
        [Fact] public void FitRidge_keys() => RegressionCore.FitRidge(X,y,0.1).Should().ContainKeys("coefficients","sse","r_squared","lambda");
        [Fact] public void AnovaOneWay_keys() => RegressionCore.AnovaOneWay(new[]{new[]{5.0,6,7},new[]{8.0,9,10}}).Should().ContainKeys("ss_between","f_stat","p_value");
        [Fact] public void FactorImportance() { var r=RegressionCore.FactorImportance(X,y); r.Length.Should().Be(2); }
        [Fact] public void FitWLS_equal_weights() { var o=RegressionCore.FitOLS(X,y); var w=RegressionCore.FitWLS(X,y,new[]{1.0,1,1}); var oc=(double[])o["coefficients"]; var wc=(double[])w["coefficients"]; oc[0].Should().BeApproximately(wc[0],1e-10); oc[1].Should().BeApproximately(wc[1],1e-10); }
        // WLS with unequal weights: verify keys exist and coefficients are finite
        [Fact] public void FitWLS_unequal_weights() { var w=RegressionCore.FitWLS(X,y,new[]{1.0,5,1}); w.Should().ContainKeys("coefficients","sse","r_squared"); var c=(double[])w["coefficients"]; c[0].Should().NotBe(double.NaN); c[1].Should().NotBe(double.NaN); }
        // FactorImportance returns |t-stat| sorted indices; test with 2 meaningful features
        private static readonly double[,] Xf = {{1,1,3},{1,2,1},{1,3,4},{1,4,1},{1,5,6}};
        private static readonly double[] yf = {4,5,10,11,16};
        [Fact] public void FactorImportance_ranking() { var r=RegressionCore.FactorImportance(Xf,yf); r.Length.Should().Be(3); r.Should().OnlyHaveUniqueItems(); }
    }
}
