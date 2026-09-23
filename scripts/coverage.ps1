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
    (Analytics 53.5% instead of the real 89.05%). Always measure through this
    script (or CI) before comparing against the gate.
.NOTES
    Usage: powershell -File scripts/coverage.ps1 [-Foundation 80] [-Analytics 85] [-DataToolkit 85]
    默认阈值与 ci.yml 保持一致；调低阈值仅用于本地排查，不得提交降低后的 ci.yml。
#>
param(
    [string]$RepoRoot = (Split-Path -Parent $PSScriptRoot),
    [int]$Foundation = 80,
    [int]$Analytics = 85,
    [int]$DataToolkit = 85
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
        dotnet test $m.Project -f $m.Tfm -p:CollectCoverage=true -p:CoverletOutputFormat=cobertura `
            -p:CoverletOutput="coverage/$($m.Name.ToLower())" -p:Threshold=$($m.Threshold) `
            -p:ThresholdType=line -p:ThresholdStat=total -p:Include="$($m.Include)" --nologo
        $dotnetExit = $LASTEXITCODE

        # 解析报告做汇总（coverlet 阈值已决定成败，这里只负责可读输出）
        $projDir = Join-Path $RepoRoot (Split-Path $m.Project -Parent)
        $report = Get-ChildItem -Path (Join-Path $projDir 'coverage') -Filter '*.cobertura.xml' -ErrorAction SilentlyContinue |
            Sort-Object LastWriteTime -Descending | Select-Object -First 1
        if ($report) {
            $doc = New-Object System.Xml.XmlDocument
            $doc.Load($report.FullName)
            $rate = [double]$doc.DocumentElement.GetAttribute('line-rate')
            $covered = $doc.DocumentElement.GetAttribute('lines-covered')
            $valid = $doc.DocumentElement.GetAttribute('lines-valid')
            $pct = [Math]::Round($rate * 100, 2)
            if ($rate * 100 -ge $m.Threshold) {
                Write-Host "  [PASS] $($m.Name): $pct% ($covered/$valid lines)" -ForegroundColor Green
            } else {
                Write-Host "  [FAIL] $($m.Name): $pct% ($covered/$valid lines) < $($m.Threshold)%" -ForegroundColor Red
                $failures += "$($m.Name) $pct% < $($m.Threshold)%"
            }
        } elseif ($dotnetExit -ne 0) {
            $failures += "$($m.Name) dotnet test exit $dotnetExit"
        } else {
            Write-Host "  [WARN] $($m.Name): cobertura report not found" -ForegroundColor DarkYellow
        }
        if ($dotnetExit -ne 0 -and -not $report) { $failures += "$($m.Name) dotnet test exit $dotnetExit" }
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
