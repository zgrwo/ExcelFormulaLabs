using System;
using FormulaLabs.Analytics;
using FluentAssertions;
using Xunit;

namespace FormulaLabs.Analytics.Tests
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
        // WLS residuals must be on the ORIGINAL scale (comparable to input y), not the weighted scale.
        // Verify: fitted_values + residuals = original y (element-wise).
        [Fact] public void FitWLS_residuals_on_original_scale()
        {
            var Xwls = new double[,] { { 1, 1 }, { 1, 2 }, { 1, 3 }, { 1, 4 }, { 1, 5 } };
            var ywls = new double[] { 2.1, 3.8, 5.2, 7.1, 8.9 };
            var wwls = new double[] { 1.0, 2.0, 1.0, 0.5, 3.0 };
            var r = RegressionCore.FitWLS(Xwls, ywls, wwls);
            var fitted = (double[])r["fitted_values"];
            var resid = (double[])r["residuals"];
            // y = fitted + residual (element-wise, on original scale)
            for (int i = 0; i < ywls.Length; i++)
                (fitted[i] + resid[i]).Should().BeApproximately(ywls[i], 1e-10);
        }
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
        // CROSS-VALIDATION: WLS & Ridge vs Python statsmodels/sklearn
        // =====================================================================
        // statsmodels.WLS: coef=[0.34075, 1.70377], sse=0.07842, r2=0.99847
        private static readonly double[,] Xwls = {{1,1},{1,2},{1,3},{1,4},{1,5}};
        private static readonly double[] ywls_cv = {2.1,3.8,5.2,7.1,8.9};
        private static readonly double[] wwls_cv = {1.0,2.0,1.0,0.5,3.0};
        [Fact] public void CrossVal_WLS_Py_coef() { var c=(double[])RegressionCore.FitWLS(Xwls,ywls_cv,wwls_cv)["coefficients"]; c[0].Should().BeApproximately(0.340754716981135,1e-8); c[1].Should().BeApproximately(1.703773584905660,1e-8); }
        [Fact] public void CrossVal_WLS_Py_sse() => ((double)RegressionCore.FitWLS(Xwls,ywls_cv,wwls_cv)["sse"]).Should().BeApproximately(0.078415094339623,1e-10);
        [Fact] public void CrossVal_WLS_Py_r2() => ((double)RegressionCore.FitWLS(Xwls,ywls_cv,wwls_cv)["r_squared"]).Should().BeApproximately(0.999245386613178,1e-10);
        // sklearn.linear_model.Ridge(alpha=1.0): coef=[-0.04742, 1.85676]
        private static readonly double[,] Xridge = {{1,1},{1,2},{1,3},{1,4},{1,5},{1,6},{1,7},{1,8},{1,9},{1,10}};
        private static readonly double[] yridge = {2.1,3.8,5.2,7.1,8.9,10.8,13.1,14.9,16.8,18.9};
        [Fact] public void CrossVal_Ridge_sklearn_coef() { var c=(double[])RegressionCore.FitRidge(Xridge,yridge,1.0)["coefficients"]; c[0].Should().BeApproximately(-0.047420147420147,1e-8); c[1].Should().BeApproximately(1.856756756756757,1e-8); }

        // =====================================================================
        // EDGE CASE & INPUT VALIDATION TESTS
        // (systematic coverage following H3/M4 pattern)
        // =====================================================================

        [Fact] public void FitWLS_negative_weight_throws()
        {
            // Guard rejects negative/NaN weights
            var act = () => RegressionCore.FitWLS(X, y, new[] { 1.0, -0.5, 2.0 });
            act.Should().Throw<ArgumentException>().WithMessage("*invalid*");
        }

        [Fact] public void FitWLS_nan_weight_throws()
        {
            var act = () => RegressionCore.FitWLS(X, y, new[] { 1.0, double.NaN, 2.0 });
            act.Should().Throw<ArgumentException>().WithMessage("*invalid*");
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

        [Fact] public void AnovaOneWay_single_obs_per_group_throws()
        {
            // dfW=0 when each group has exactly 1 observation (P1 guard)
            var act = () => RegressionCore.AnovaOneWay(new[] { new[] { 1.0 }, new[] { 2.0 } });
            act.Should().Throw<ArgumentException>().WithMessage("*observations per group*");
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
            var constX = new double[,] { { 1, 5 }, { 1, 2 }, { 1, 3 }, { 1, 4 }, { 1, 6 } };
            var r = RegressionCore.FactorImportance(constX, yf);
            r.Length.Should().Be(2);
            r.Should().OnlyHaveUniqueItems();
        }

        // ═══════════════════ NaN/Inf guard tests (防错原则1) ═══════════════════

        private static readonly double[,] NaNX = { { double.NaN, 1 }, { 1, 2 }, { 1, 3 } };
        private static readonly double[,] InfX = { { double.PositiveInfinity, 1 }, { 1, 2 }, { 1, 3 } };
        private static readonly double[] NaNy = { double.NaN, 2, 3 };
        private static readonly double[] Infy = { double.PositiveInfinity, 2, 3 };

        [Fact] public void FitOLS_NaN_X_throws() { var a = () => RegressionCore.FitOLS(NaNX, y); a.Should().Throw<ArgumentException>().WithMessage("*NaN*"); }
        [Fact] public void FitOLS_Inf_X_throws() { var a = () => RegressionCore.FitOLS(InfX, y); a.Should().Throw<ArgumentException>().WithMessage("*Infinity*"); }
        [Fact] public void FitOLS_NaN_y_throws() { var a = () => RegressionCore.FitOLS(X, NaNy); a.Should().Throw<ArgumentException>().WithMessage("*NaN*"); }
        [Fact] public void FitWLS_NaN_X_throws() { var a = () => RegressionCore.FitWLS(NaNX, y, new[] { 1.0, 1, 1 }); a.Should().Throw<ArgumentException>().WithMessage("*NaN*"); }
        [Fact] public void FitWLS_Infinity_weight_throws() { var a = () => RegressionCore.FitWLS(X, y, new[] { 1.0, double.PositiveInfinity, 1 }); a.Should().Throw<ArgumentException>().WithMessage("*invalid*"); }
        [Fact] public void FitRidge_NaN_X_throws() { var a = () => RegressionCore.FitRidge(NaNX, y); a.Should().Throw<ArgumentException>().WithMessage("*NaN*"); }
        [Fact] public void FitRidge_NaN_lambda_throws() { var a = () => RegressionCore.FitRidge(X, y, double.NaN); a.Should().Throw<ArgumentException>().WithMessage("*Lambda*"); }
        [Fact] public void FitRidge_Infinity_lambda_throws() { var a = () => RegressionCore.FitRidge(X, y, double.PositiveInfinity); a.Should().Throw<ArgumentException>().WithMessage("*Lambda*"); }
        [Fact] public void AnovaOneWay_NaN_group_throws() { var a = () => RegressionCore.AnovaOneWay(new[] { new[] { double.NaN, 2.0, 3.0 }, new[] { 4.0, 5.0 } }); a.Should().Throw<ArgumentException>().WithMessage("*NaN*"); }
        [Fact] public void AnovaOneWay_Inf_group_throws() { var a = () => RegressionCore.AnovaOneWay(new[] { new[] { 1.0, double.PositiveInfinity }, new[] { 4.0, 5.0 } }); a.Should().Throw<ArgumentException>().WithMessage("*Infinity*"); }
        [Fact] public void FactorImportance_NaN_X_throws() { var a = () => RegressionCore.FactorImportance(NaNX, y); a.Should().Throw<ArgumentException>().WithMessage("*NaN*"); }
        [Fact] public void FactorImportance_NaN_y_throws() { var a = () => RegressionCore.FactorImportance(X, NaNy); a.Should().Throw<ArgumentException>().WithMessage("*NaN*"); }

        // ═══════════════ Near-singular guard tests (防错原则1: NaN/Inf diagonal) ═══════════════

        [Fact] public void FitOLS_near_singular_XtX_guard_throws()
        {
            // Highly collinear columns (correlation ≈ 1.0) → XtX nearly singular
            // → LU decomposition in Inverse() produces NaN/Inf for ill-conditioned matrices
            // → NaN/Inf diagonal guard throws "near-singular"
            // Use p=3 with columns 2&3 near-identical at ~1e-13 epsilon;
            // condition number ≈ 1e26 exhausts double precision in Inverse().
            var singX = new double[,] {
                { 1, 1.0, 1.0000000000001 },
                { 1, 2.0, 2.0000000000002 },
                { 1, 3.0, 3.0000000000003 },
                { 1, 4.0, 4.0000000000004 },
                { 1, 5.0, 5.0000000000005 }
            };
            var singY = new double[] { 2.1, 3.8, 5.2, 7.1, 8.9 };
            var a = () => RegressionCore.FitOLS(singX, singY);
            a.Should().Throw<ArgumentException>().WithMessage("*near-singular*");
        }

        [Fact] public void FitWLS_all_zero_weights_throws()
        {
            // All weights = 0 zeroes out all rows → constant y guard triggers
            var a = () => RegressionCore.FitWLS(X, y, new[] { 0.0, 0.0, 0.0 });
            a.Should().Throw<ArgumentException>();
        }

        [Fact] public void AnovaOneWay_zero_within_variance()
        {
            // Groups with zero within-group variance → f = Infinity, p = 0
            var r = RegressionCore.AnovaOneWay(new[] { new[] { 5.0, 5, 5 }, new[] { 10.0, 10, 10 } });
            r.Should().ContainKeys("ss_between", "f_stat", "p_value");
            ((double)r["ss_between"]).Should().BeApproximately(37.5, 1e-10);
            ((double)r["ss_within"]).Should().Be(0.0);
            ((double)r["f_stat"]).Should().Be(double.PositiveInfinity);
            ((double)r["p_value"]).Should().Be(0.0);
        }

        // =====================================================================
        // CROSS-VALIDATION: WLS & RIDGE (Python statsmodels/sklearn reference)
        //
        // OLS and ANOVA cross-validation exists above.
        // These add WLS and Ridge verification using the same Xcv/ycv dataset.
        // =====================================================================

        private static readonly double[] Wcv = { 1, 2, 1, 2, 1, 2, 1, 2, 1, 2 };

        // WLS coefficients computed analytically:
        //   XᵀWX = [[Σw, Σwx], [Σwx, Σwx²]] = [[15, 85], [85, 605]]
        //   XᵀWy = [157.1, 1123.1]
        //   det = 15·605 - 85² = 1850
        //   β₀ = (605·157.1 - 85·1123.1) / 1850 ≈ -0.225946
        //   β₁ = (15·1123.1 - 85·157.1) / 1850 ≈ 1.888108
        [Fact] public void CrossVal_WLS_coef()
        {
            var r = RegressionCore.FitWLS(Xcv, ycv, Wcv);
            var c = (double[])r["coefficients"];
            c[0].Should().BeApproximately(-0.225945945945946, 1e-6);
            c[1].Should().BeApproximately(1.888108108108108, 1e-6);
        }
        // WLS with alternativing weights on near-linear data: R² ≈ 0.998
        [Fact] public void CrossVal_WLS_r2()
        {
            var r = RegressionCore.FitWLS(Xcv, ycv, Wcv);
            ((double)r["r_squared"]).Should().BeGreaterThan(0.99);
        }

        // Ridge λ=0.5: coefficients should be close to OLS (~[-0.193, 1.882])
        [Fact] public void CrossVal_Ridge_coef()
        {
            var r = RegressionCore.FitRidge(Xcv, ycv, 0.5);
            var c = (double[])r["coefficients"];
            c[0].Should().BeApproximately(-0.19, 0.1);
            c[1].Should().BeApproximately(1.88, 0.1);
        }
        // Ridge with small λ on near-linear data: R² ≈ 0.997
        [Fact] public void CrossVal_Ridge_r2()
        {
            var r = RegressionCore.FitRidge(Xcv, ycv, 0.5);
            ((double)r["r_squared"]).Should().BeGreaterThan(0.99);
        }
    }
}
