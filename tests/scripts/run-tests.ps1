# ============================================================================
# run-tests.ps1 — 治理脚本自测运行器
# 用法：powershell -NoProfile -ExecutionPolicy Bypass -File tests/scripts/run-tests.ps1
# 说明：运行 tests/scripts/ 下全部测试脚本；任一失败则退出码非 0。
# ============================================================================
$ErrorActionPreference = "Stop"
$dir = Split-Path -Parent $PSScriptRoot   # tests/
$scripts = @(
    "test_precommit_check.ps1",
    "test_verify_docs.ps1"
)
$failures = @()
foreach ($s in $scripts) {
    Write-Host ""
    Write-Host "===== 运行 $s =====" -ForegroundColor Cyan
    & powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $dir "scripts\$s")
    if ($LASTEXITCODE -ne 0) { $failures += $s }
}
Write-Host ""
if ($failures.Count -eq 0) {
    Write-Host "[OK] 全部治理脚本自测通过" -ForegroundColor Green
    exit 0
} else {
    Write-Host "[FAIL] 失败: $($failures -join ', ')" -ForegroundColor Red
    exit 1
}
