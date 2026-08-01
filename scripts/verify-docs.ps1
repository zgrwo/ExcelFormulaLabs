# verify-docs.ps1 - Document consistency verification (PowerShell equivalent of verify-docs.sh)
# Usage: .\scripts\verify-docs.ps1
# Checks: UDF count, UDF coverage, skill terms, version match, bare catch, .dna templates
$ErrorActionPreference = "Continue"
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

$script:pass = 0; $script:fail = 0
function Check {
    param([string]$Label, [string]$Result)
    if ($Result -eq "OK") { Write-Host "  [PASS] $Label"; $script:pass++ }
    else { Write-Host "  [FAIL] ${Label}: ${Result}"; $script:fail++ }
}

# 1. UDF count: api-reference.md as source of truth
$docUdfs = (Select-String -Path "rules\api-reference.md" -Pattern '^\| `[A-Z]+\.[A-Z]' | Measure-Object).Count
$codeUdfs = (Get-ChildItem -Path src -Recurse -Filter "*.cs" |
    Where-Object { $_.FullName -notmatch "\\obj\\" -and $_.FullName -notmatch "\\bin\\" } |
    Select-String -Pattern 'ExcelFunction\(Name\s*=\s*"([^"]*)"' -AllMatches |
    ForEach-Object { $_.Matches } | ForEach-Object { $_.Groups[1].Value } |
    Sort-Object -Unique | Measure-Object).Count
if ($docUdfs -eq $codeUdfs) { Check "UDF count ($docUdfs)" "OK" }
else { Check "UDF count" "doc=$docUdfs code=$codeUdfs" }

# 2. Every code UDF has an entry in api-reference.md
$apiContent = Get-Content "rules\api-reference.md" -Raw
$missing = @()
Get-ChildItem -Path src -Recurse -Filter "*.cs" |
    Where-Object { $_.FullName -notmatch "\\obj\\" -and $_.FullName -notmatch "\\bin\\" } |
    Select-String -Pattern 'ExcelFunction\(Name\s*=\s*"([^"]*)"' -AllMatches |
    ForEach-Object { $_.Matches } | ForEach-Object { $_.Groups[1].Value } |
    Sort-Object -Unique | ForEach-Object {
        if ($apiContent -notmatch [regex]::Escape($_)) { $missing += $_ }
    }
if ($missing.Count -eq 0) { Check "UDF full coverage" "OK" }
else { Check "UDF full coverage" "missing: $($missing -join ', ')" }

# 3. skill.md contains RangeExport
$skillContent = Get-Content "skills\excel-dna-project.md" -Raw -ErrorAction SilentlyContinue
if ($skillContent -match 'RangeExport') { Check "skill.md RangeExport" "OK" }
else { Check "skill.md RangeExport" "missing" }

# 4. Architecture terminology
if ($skillContent -match 'MapOver') { Check "skill.md MapOver term" "OK" }
else { Check "skill.md MapOver term" "missing" }
$readmeContent = Get-Content "README.md" -Raw -ErrorAction SilentlyContinue
if ($readmeContent -match 'ElementWiseMapper') { Check "README no internal class names" "should use MapOver not internal class" }
else { Check "README no internal impl details" "OK" }

# 5. Version match (context.md vs csproj)
$docVer = if ((Get-Content "rules\context.md" -Raw) -match 'MathNet\.Numerics\s+([0-9.]+)') { $Matches[1] } else { "?" }
$csprojVer = if ((Get-Content "src\Analytics\Analytics.csproj" -Raw) -match 'MathNet\.Numerics.*Version="([0-9.]+)"') { $Matches[1] } else { "?" }
if ($docVer -eq $csprojVer) { Check "MathNet version ($docVer)" "OK" }
else { Check "MathNet version" "doc=$docVer csproj=$csprojVer" }

# 6. No bare catch blocks
$bareCatches = Get-ChildItem -Path src -Recurse -Filter "*.cs" |
    Where-Object { $_.FullName -notmatch "\\obj\\" -and $_.FullName -notmatch "\\bin\\" } |
    Select-String -Pattern 'catch\s*\{'
if ($bareCatches.Count -eq 0) { Check "No bare catch" "OK" }
else { Check "No bare catch" "$($bareCatches.Count) found" }

# 7. .dna templates exist
if (Test-Path "src\DataToolkit\DataToolkit-AddIn-net8.dna.tpl") { Check "net8 .dna template" "OK" }
else { Check "net8 .dna template" "missing" }
if (Test-Path "src\DataToolkit\DataToolkit-AddIn-net48.dna.tpl") { Check "net48 .dna template" "OK" }
else { Check "net48 .dna template" "missing" }

# 8. No residual generated .dna
$residual = Get-ChildItem -Path "src\DataToolkit" -Filter "DataToolkit-AddIn.dna" -File -ErrorAction SilentlyContinue |
    Where-Object { $_.Name -notlike "*.tpl" }
if (-not $residual) { Check "No residual .dna" "OK" }
else { Check "No residual .dna" "found residual" }

# Summary
Write-Host ""
Write-Host "=== Pass: $($script:pass)  Fail: $($script:fail) ==="
if ($script:fail -gt 0) { exit 1 } else { exit 0 }
