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
    # P2-6 (review-2026-08-31): 优先 pwsh 7（与 release.yml 一致，暴露 pwsh7 语义差异），回退 powershell 5.1
    # R14 (review-2026-09-05)：原 L17-18 畸形拼接——$hostCmd 计算后未使用，L18 实为双重
    # -File 拼接（$hostCmd 连同第二组参数被当脚本位置参数吞进 $args），pwsh 优先策略从未
    # 生效、恒以 powershell 运行。改为单一 & $hostCmd 调用。
    $hostCmd = if (Get-Command pwsh -ErrorAction SilentlyContinue) { "pwsh" } else { "powershell" }
    & $hostCmd -NoProfile -ExecutionPolicy Bypass -File (Join-Path $dir "scripts\$s")
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
