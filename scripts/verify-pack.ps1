# verify-pack.ps1 - Validate ExcelDnaPack output integrity
# Usage: powershell -File verify-pack.ps1 -PublishDir <path> -Module <Analytics|DataToolkit> -Tfm <net48|net8.0-windows>
param(
    [Parameter(Mandatory=$true)] [string] $PublishDir,
    [Parameter(Mandatory=$true)] [string] $Module,
    [Parameter(Mandatory=$true)] [string] $Tfm
)

$ErrorActionPreference = "Stop"
$errors = @()
$warnings = @()

# TFM -> XLL filename suffix mapping
$tfmSuffix = if ($Tfm -eq "net48") { "net48" } else { "net8.0" }

# 1. Check packed XLLs exist and have reasonable size (>= 100 KB)
$minSize = 100 * 1024
$xllFiles = @(
    "$PublishDir\$Module-AddIn-$tfmSuffix-packed.xll",
    "$PublishDir\$Module-AddIn-$tfmSuffix-64-packed.xll"
)

foreach ($xll in $xllFiles) {
    if (-not (Test-Path $xll)) {
        $errors += "Missing packed XLL: $xll"
    } else {
        $size = (Get-Item $xll).Length
        if ($size -lt $minSize) {
            $errors += "$xll size too small: $size bytes (min $minSize)"
        } else {
            $kb = [math]::Round($size / 1024)
            $name = Split-Path $xll -Leaf
            Write-Host "  [OK] $name ($kb KB)"
        }
    }
}

# 2. Check SQLite native DLL
#    net48: embedded in DataToolkit.dll, publish copy is a filesystem fallback
#    net8.0: packed in XLL as NATIVE_LIBRARY_LZMA
if ($Module -eq "DataToolkit") {
    if ($tfmSuffix -eq "net48") {
        $nativeName = "SQLite.Interop.dll"
    } else {
        $nativeName = "e_sqlite3.dll"
    }
    $interopX86 = "$PublishDir\x86\$nativeName"
    $interopX64 = "$PublishDir\x64\$nativeName"
    if (-not (Test-Path $interopX86)) {
        $warnings += "Unpacked fallback missing: $interopX86 (packed mode is unaffected)"
    } else {
        $kb = [math]::Round((Get-Item $interopX86).Length / 1024)
        Write-Host "  [OK] x86\$nativeName ($kb KB)"
    }
    if (-not (Test-Path $interopX64)) {
        $warnings += "Unpacked fallback missing: $interopX64 (packed mode is unaffected)"
    } else {
        $kb = [math]::Round((Get-Item $interopX64).Length / 1024)
        Write-Host "  [OK] x64\$nativeName ($kb KB)"
    }
}

# 3. Check for cross-TFM contamination
$otherTfm = if ($tfmSuffix -eq "net48") { "net8.0" } else { "net48" }
$staleXll = "$PublishDir\$Module-AddIn-$otherTfm-packed.xll"
if (Test-Path $staleXll) {
    $warnings += "Stale XLL from other TFM found: $staleXll"
}

# Report
if ($errors.Count -gt 0) {
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Red
    Write-Host "  VERIFY-PACK FAILED -- $Module $Tfm" -ForegroundColor Red
    Write-Host "========================================" -ForegroundColor Red
    foreach ($e in $errors) {
        Write-Host "  ERROR: $e" -ForegroundColor Red
    }
    exit 1
}

if ($warnings.Count -gt 0) {
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Yellow
    Write-Host "  VERIFY-PACK PASSED (with warnings)" -ForegroundColor Yellow
    Write-Host "========================================" -ForegroundColor Yellow
    foreach ($w in $warnings) {
        Write-Host "  WARN: $w" -ForegroundColor Yellow
    }
    exit 0
}

Write-Host ""
Write-Host "  VERIFY-PACK PASSED -- $Module $Tfm" -ForegroundColor Green
exit 0
