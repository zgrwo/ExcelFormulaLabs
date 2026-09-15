# verify-all.ps1 - One-command local verification (6-step gate)
# Usage: .\scripts\verify-all.ps1 [-Configuration Release] [-SkipCrossVal]
# Runs all verification steps required before a PR or release.

param(
    [string]$Configuration = "Debug",
    [switch]$SkipCrossVal,
    [switch]$SkipManual
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$sw = [System.Diagnostics.Stopwatch]::StartNew()
$failures = @()

function Step {
    param([string]$Label, [scriptblock]$Block, [int]$Retries = 0)
    Write-Host ""
    Write-Host "=== $Label ==="
    $attempt = 0
    while ($true) {
        $attempt++
        try {
            & $Block
            if ($LASTEXITCODE -ne 0) { throw "Exit code $LASTEXITCODE" }
            Write-Host "[PASS] $Label"
            return
        } catch {
            if ($attempt -le $Retries) {
                # R7-3 (review-2026-09-13)：ExcelDnaPack 偶发瞬时文件锁（Win32Exception 110，
                # Defender 扫描刚写出的 .xll）——对构建步骤自动重试；真实错误重试后仍失败。
                Write-Host "[RETRY] $Label - $_ （第 $attempt 次失败，5s 后重试）"
                Start-Sleep -Seconds 5
                continue
            }
            Write-Host "[FAIL] $Label - $_"
            $script:failures += $Label
            return
        }
    }
}

Write-Host "============================================"
Write-Host " ExcelFormulaLabs - Full Verification Gate"
Write-Host " Config: $Configuration"
Write-Host "============================================"

# Step 1: verify-docs（文档一致性 20 项）
Step "1/6 verify-docs" {
    powershell -NoProfile -File "$root\scripts\verify-docs.ps1"
}

# Step 2: Build
Step "2/6 Build ($Configuration)" -Retries 1 -Block {
    dotnet build "$root\ExcelFormulaLabs.sln" -c $Configuration --nologo -v q
    # R7-4 (review-2026-09-13)：构建后校验 4 个模块/TFM 的 publish 产物。verify-pack 原先
    # 只在 csproj 的 Release 目标中运行，Debug 下中断构建留下的跨 TFM 过期 XLL / base 尺寸
    # 坏产物无人拦截。此处对当前 $Configuration 全覆盖（含 Debug）。
    foreach ($pc in @(
        @{ Module = "Analytics";   Tfm = "net8.0-windows" },
        @{ Module = "Analytics";   Tfm = "net48" },
        @{ Module = "DataToolkit"; Tfm = "net8.0-windows" },
        @{ Module = "DataToolkit"; Tfm = "net48" }
    )) {
        $pub = Join-Path $root "src\$($pc.Module)\bin\$Configuration\$($pc.Tfm)\publish"
        powershell -NoProfile -File "$root\scripts\verify-pack.ps1" -PublishDir $pub -Module $pc.Module -Tfm $pc.Tfm
        if ($LASTEXITCODE -ne 0) { throw "verify-pack FAILED: $($pc.Module) $($pc.Tfm)" }
    }
}

# Step 3: Unit Tests (all TFMs: net8.0 + net8.0-windows + net48)
Step "3/6 Unit Tests (all TFMs)" {
    dotnet test "$root\ExcelFormulaLabs.sln" -c $Configuration --no-build --nologo -v q
}

# Step 4: CrossVal (C# CrossValRunner + Python verify-manual.py)
if (-not $SkipCrossVal) {
    Step "4/6 CrossVal (verify-manual.py)" {
        python "$root\scripts\verify-manual.py"
    }
} else {
    Write-Host ""
    Write-Host "=== 4/6 CrossVal [SKIPPED] ==="
}

# Step 5: Pre-commit checks (bare catch / self-validation / IntelliSense / Core isolation)
Step "5/6 Pre-commit Checks" {
    powershell -NoProfile -File "$root\scripts\pre-commit-check.ps1"
}

# Step 6: Release build (dual TFM packaging verification)
Step "6/6 Release Build" -Retries 1 -Block {
    dotnet build "$root\ExcelFormulaLabs.sln" -c Release --nologo -v q
}

# Summary
$sw.Stop()
Write-Host ""
Write-Host "============================================"
if ($failures.Count -eq 0) {
    Write-Host " RESULT: ALL PASS ($([math]::Round($sw.Elapsed.TotalSeconds, 1))s)"
} else {
    Write-Host " RESULT: $($failures.Count) FAILURE(S)"
    foreach ($f in $failures) { Write-Host "   - $f" }
}
Write-Host "============================================"
exit $failures.Count
