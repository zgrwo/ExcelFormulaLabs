# ============================================================================
# run-tests.ps1 — 治理脚本自测运行器
# 用法：powershell -NoProfile -ExecutionPolicy Bypass -File tests/scripts/run-tests.ps1
# 说明：运行 tests/scripts/ 下全部测试脚本；任一失败则退出码非 0。
# ============================================================================
$ErrorActionPreference = "Stop"
$dir = Split-Path -Parent $PSScriptRoot   # tests/
# R5-P3-38 (review 2026-09-06)：① 测试清单改目录扫描（原硬编码 2 项与头注"全部测试脚本"
# 矛盾，新增 test_*.ps1 会被静默漏跑）；② 双宿主覆盖——pwsh7 与 PS5.1 语义差异是本项目
# 已知陷阱域（C2），"pwsh 优先回退"实为单宿主优选，两个宿主都要跑。
$scripts = @(Get-ChildItem -Path $PSScriptRoot -Filter "test_*.ps1" -File |
    Sort-Object Name | ForEach-Object { $_.Name })
if ($scripts.Count -eq 0) {
    Write-Host "[FAIL] no test_*.ps1 found in $PSScriptRoot" -ForegroundColor Red
    exit 1
}
$hostList = @()
if (Get-Command pwsh -ErrorAction SilentlyContinue) { $hostList += "pwsh" }
if (Get-Command powershell -ErrorAction SilentlyContinue) { $hostList += "powershell" }
$failures = @()
foreach ($s in $scripts) {
    foreach ($h in $hostList) {
        Write-Host ""
        Write-Host "===== [$h] $s =====" -ForegroundColor Cyan
        & $h -NoProfile -ExecutionPolicy Bypass -File (Join-Path $dir "scripts\$s")
        if ($LASTEXITCODE -ne 0) { $failures += "[$h] $s" }
    }
}
Write-Host ""
if ($failures.Count -eq 0) {
    Write-Host "[OK] 全部治理脚本自测通过" -ForegroundColor Green
    exit 0
} else {
    Write-Host "[FAIL] 失败: $($failures -join ', ')" -ForegroundColor Red
    exit 1
}
