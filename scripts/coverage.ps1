#Requires -Version 5.1
<#
.SYNOPSIS
    ExcelFormulaLabs CI-equivalent coverage gate (net8.0).
.DESCRIPTION
    Runs the same coverlet commands as .github/workflows/ci.yml coverage job
    (per-module Include filter, ThresholdStat=total) and prints a summary table.
    Exit code is non-zero when any module is below its threshold.

    Why this script exists: the checked-in local reports under
    tests/*/coverage-local/ were produced WITHOUT the Include filter and show
    Foundation classes at ~0% inside Analytics reports — misleading numbers
    (Analytics 53.5% instead of the real 90.2%). Always measure through this
    script (or CI) before comparing against the gate.
.NOTES
    Usage: powershell -File scripts/coverage.ps1 [-Foundation 92] [-Analytics 86] [-DataToolkit 86]
    默认阈值与 ci.yml 保持一致；调低阈值仅用于本地排查，不得提交降低后的 ci.yml。
#>
param(
    [string]$RepoRoot = (Split-Path -Parent $PSScriptRoot),
    [int]$Foundation = 92,
    [int]$Analytics = 86,
    [int]$DataToolkit = 86
)

$ErrorActionPreference = "Stop"
$RepoRoot = [System.IO.Path]::GetFullPath($RepoRoot)

$modules = @(
    [PSCustomObject]@{ Name = 'Foundation';  Project = 'tests/Foundation.Tests/Foundation.Tests.csproj';   Tfm = 'net8.0';         Include = '[Foundation]*';  Threshold = $Foundation },
    [PSCustomObject]@{ Name = 'Analytics';   Project = 'tests/Analytics.Tests/Analytics.Tests.csproj';     Tfm = 'net8.0-windows'; Include = '[Analytics]*';   Threshold = $Analytics },
    [PSCustomObject]@{ Name = 'DataToolkit'; Project = 'tests/DataToolkit.Tests/DataToolkit.Tests.csproj'; Tfm = 'net8.0-windows'; Include = '[DataToolkit]*'; Threshold = $DataToolkit }
)

$failures = @()
Push-Location $RepoRoot
try {
    foreach ($m in $modules) {
        Write-Host ""
        Write-Host "===== Coverage: $($m.Name) (threshold $($m.Threshold)%) =====" -ForegroundColor Cyan

        # R1-07 假绿修复：运行前清理旧报告。coverlet 在测试失败/构建失败时**不重写**报告，
        # 残留的旧报告会被下方"按最新文件读取"逻辑当成本轮结果 → 失败被静默读成 PASS。
        $projDir = Join-Path $RepoRoot (Split-Path $m.Project -Parent)
        $coverageDir = Join-Path $projDir 'coverage'
        if (Test-Path $coverageDir) {
            Get-ChildItem -Path $coverageDir -Filter '*.cobertura.xml' -File -ErrorAction SilentlyContinue |
                Remove-Item -Force -ErrorAction SilentlyContinue
        }

        dotnet test $m.Project -f $m.Tfm -p:CollectCoverage=true -p:CoverletOutputFormat=cobertura `
            -p:CoverletOutput="coverage/$($m.Name.ToLower())" -p:Threshold=$($m.Threshold) `
            -p:ThresholdType=line -p:ThresholdStat=total -p:Include="$($m.Include)" --nologo
        $dotnetExit = $LASTEXITCODE

        # 解析报告做汇总（可读输出）；退出码始终是权威判据（报告存在也不豁免）。
        $report = Get-ChildItem -Path $coverageDir -Filter '*.cobertura.xml' -ErrorAction SilentlyContinue |
            Sort-Object LastWriteTime -Descending | Select-Object -First 1
        $rateOk = $false
        $pct = 0.0
        if ($report) {
            $doc = New-Object System.Xml.XmlDocument
            $doc.Load($report.FullName)
            $rate = [double]$doc.DocumentElement.GetAttribute('line-rate')
            $covered = $doc.DocumentElement.GetAttribute('lines-covered')
            $valid = $doc.DocumentElement.GetAttribute('lines-valid')
            $pct = [Math]::Round($rate * 100, 2)
            $rateOk = $rate * 100 -ge $m.Threshold
            if ($rateOk -and $dotnetExit -eq 0) {
                Write-Host "  [PASS] $($m.Name): $pct% ($covered/$valid lines)" -ForegroundColor Green
            } else {
                Write-Host "  [FAIL] $($m.Name): $pct% ($covered/$valid lines) < $($m.Threshold)% (exit $dotnetExit)" -ForegroundColor Red
            }
        }
        if ($dotnetExit -ne 0) {
            $failures += "$($m.Name) dotnet test exit $dotnetExit"
        } elseif (-not $report) {
            $failures += "$($m.Name) cobertura report not found (test/build did not complete)"
        } elseif (-not $rateOk) {
            $failures += "$($m.Name) $pct% < $($m.Threshold)%"
        }
    }
} finally {
    Pop-Location
}

Write-Host ""
Write-Host "============================================================"
if ($failures.Count -eq 0) {
    Write-Host "  [PASS] All coverage gates passed." -ForegroundColor Green
    Write-Host "============================================================"
    exit 0
} else {
    Write-Host "  [BLOCKED] $($failures.Count) coverage gate failure(s):" -ForegroundColor Red
    foreach ($f in $failures) { Write-Host "    - $f" -ForegroundColor Red }
    Write-Host "============================================================"
    exit 1
}
