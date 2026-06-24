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
        // Python statsmodels cross-validation: X=[1..10], y≈2x (realistic, not perfect fit)
        private static readonly double[,] Xcv = {{1,1},{1,2},{1,3},{1,4},{1,5},{1,6},{1,7},{1,8},{1,9},{1,10}};
        private static readonly double[] ycv = {2.1,3.8,5.2,7.1,8.9,10.8,13.1,14.9,16.8,18.9};
        [Fact] public void OLS_crossval_coef() { var c=(double[])RegressionCore.FitOLS(Xcv,ycv)["coefficients"]; c[0].Should().BeApproximately(-0.193333333333334,1e-8); c[1].Should().BeApproximately(1.882424242424243,1e-8); }
        [Fact] public void OLS_crossval_r2() => ((double)RegressionCore.FitOLS(Xcv,ycv)["r_squared"]).Should().BeApproximately(0.997871700442665,1e-10);
        [Fact] public void OLS_crossval_sse() => ((double)RegressionCore.FitOLS(Xcv,ycv)["sse"]).Should().BeApproximately(0.62351515151515,1e-10);
        [Fact] public void OLS_crossval_se() { var se=(double[])RegressionCore.FitOLS(Xcv,ycv)["standard_errors"]; se[0].Should().BeApproximately(0.190713704729673,1e-8); se[1].Should().BeApproximately(0.030736296565105,1e-8); }
        [Fact] public void OLS_crossval_tstat() { var t=(double[])RegressionCore.FitOLS(Xcv,ycv)["t_stats"]; t[0].Should().BeApproximately(-1.013735922163402,1e-6); t[1].Should().BeApproximately(61.244341472204,1e-6); }
        [Fact] public void OLS_crossval_pval() { var p=(double[])RegressionCore.FitOLS(Xcv,ycv)["p_values"]; p[0].Should().BeApproximately(0.340383606094307,1e-6); p[1].Should().BeApproximately(5.6151214e-12,1e-4); }
        // ANOVA cross-validation with scipy.stats.f_oneway
        [Fact] public void ANOVA_crossval() { var a=RegressionCore.AnovaOneWay(new[]{new[]{5.0,6,7,8},new[]{7.0,8,9,10},new[]{9.0,10,11,12}}); ((double)a["f_stat"]).Should().BeApproximately(9.6,1e-8); ((double)a["p_value"]).Should().BeApproximately(0.00586098088586,1e-8); }

        // =====================================================================
        // EDGE CASE & INPUT VALIDATION TESTS
        // (systematic coverage following H3/M4 pattern)
        // =====================================================================

        [Fact] public void FitWLS_negative_weight_throws()
        {
            // Guard added in round 2 — verify it rejects negative weights
            var act = () => RegressionCore.FitWLS(X, y, new[] { 1.0, -0.5, 2.0 });
            act.Should().Throw<ArgumentException>().WithMessage("*negative*");
        }

        [Fact] public void FitRidge_lambda_zero_approximates_OLS()
        {
            // Ridge with λ=0 should give coefficients very close to OLS
            var ridge = RegressionCore.FitRidge(Xcv, ycv, 0.0);
            var ols = RegressionCore.FitOLS(Xcv, ycv);
            var rc = (double[])ridge["coefficients"];
            var oc = (double[])ols["coefficients"];
            rc[0].Should().BeApproximately(oc[0], 1e-8);
            rc[1].Should().BeApproximately(oc[1], 1e-8);
        }

        [Fact] public void FitRidge_large_lambda_shrinks_coefficients()
        {
            // Large λ shrinks coefficients toward zero
            var ridge = RegressionCore.FitRidge(Xcv, ycv, 1e6);
            var coef = (double[])ridge["coefficients"];
            // Coefficients should be near zero (heavily penalized)
            Math.Abs(coef[0]).Should().BeLessThan(0.01);
            Math.Abs(coef[1]).Should().BeLessThan(0.01);
        }

        [Fact] public void AnovaOneWay_single_group()
        {
            // Single group → df_between=0 → now throws (guard added in P0 audit)
            var act = () => RegressionCore.AnovaOneWay(new[] { new[] { 1.0, 2, 3, 4 } });
            act.Should().Throw<ArgumentException>().WithMessage("*at least 2 groups*");
        }

        [Fact] public void FitOLS_constant_y_throws()
        {
            // tss=0 → constant response → R² undefined (P0 guard)
            var constY = new double[] { 5, 5, 5 };
            var act = () => RegressionCore.FitOLS(X, constY);
            act.Should().Throw<ArgumentException>().WithMessage("*constant*");
        }

        [Fact] public void FitOLS_saturated_throws()
        {
            // n=p → df=0 → SE undefined (P0 guard)
            var satX = new double[,] { { 1, 2 }, { 3, 4 } };
            var satY = new double[] { 5, 6 };
            var act = () => RegressionCore.FitOLS(satX, satY);
            act.Should().Throw<ArgumentException>().WithMessage("*degrees of freedom*");
        }

        [Fact] public void FitRidge_constant_y_throws()
        {
            // tss=0 → constant response (P0 guard)
            var constY = new double[] { 5, 5, 5 };
            var act = () => RegressionCore.FitRidge(X, constY, 0.1);
            act.Should().Throw<ArgumentException>().WithMessage("*constant*");
        }

        [Fact] public void FactorImportance_single_observation_throws()
        {
            // n=1 → sd undefined → can't standardize (P0 guard)
            var singleX = new double[,] { { 1, 5 } };
            var singleY = new double[] { 7 };
            var act = () => RegressionCore.FactorImportance(singleX, singleY);
            act.Should().Throw<ArgumentException>().WithMessage("*at least 2 observations*");
        }

        [Fact] public void FactorImportance_constant_column()
        {
            // Column with zero variance → sd<1e-12 guard → normalized to 1
            // Should not throw and should return valid rankings
            var constX = new double[,] { { 1, 5 }, { 1, 2 }, { 1, 3 }, { 1, 4 }, { 1, 6 } };
            var r = RegressionCore.FactorImportance(constX, yf);
            r.Length.Should().Be(2);
            r.Should().OnlyHaveUniqueItems();
        }
    }
}
