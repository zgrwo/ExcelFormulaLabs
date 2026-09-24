# ============================================================================
# run-tests.ps1 — 治理脚本自测运行器
# 用法：powershell -NoProfile -ExecutionPolicy Bypass -File tests/scripts/run-tests.ps1 [-MaxSkips 8]
# 说明：运行 tests/scripts/ 下全部测试脚本；任一失败则退出码非 0。
# F-09：各测试脚本内部的 SKIP（如 CI 无 Release 产物时 patch-xll 重试场景、
# .qoder 镜像缺失、真实仓库 src 不干净）此前不设上限——场景被整体跳过也能全绿。
# 汇总所有 "Skip: N" 输出，超过 -MaxSkips 即 FAIL（当前预期：2 宿主 × ~2 场景 = 4）。
# ============================================================================
param(
    [int]$MaxSkips = 8
)
$ErrorActionPreference = "Stop"
$dir = Split-Path -Parent $PSScriptRoot   # tests/
# 测试清单须目录扫描：硬编码清单与头注"全部测试脚本"矛盾，新增 test_*.ps1 会被静默漏跑。
# 双宿主覆盖——pwsh7 与 PS5.1 语义差异是本项目已知陷阱域（C2），"pwsh 优先回退"实为
# 单宿主优选，两个宿主都要跑。
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
$totalSkips = 0
foreach ($s in $scripts) {
    foreach ($h in $hostList) {
        Write-Host ""
        Write-Host "===== [$h] $s =====" -ForegroundColor Cyan
        $out = (& $h -NoProfile -ExecutionPolicy Bypass -File (Join-Path $dir "scripts\$s") 2>&1 | Out-String)
        Write-Host $out
        if ($LASTEXITCODE -ne 0) { $failures += "[$h] $s" }
        # 汇总测试脚本自身的 "Skip: N" 计数（不同脚本的汇总行格式统一为 `Skip: N`）。
        foreach ($m in [regex]::Matches($out, 'Skip:\s*(\d+)')) {
            $totalSkips += [int]$m.Groups[1].Value
        }
    }
}
Write-Host ""
if ($failures.Count -eq 0 -and $totalSkips -le $MaxSkips) {
    Write-Host "[OK] 全部治理脚本自测通过（跳过场景 $totalSkips <= $MaxSkips）" -ForegroundColor Green
    exit 0
} else {
    if ($failures.Count -gt 0) { Write-Host "[FAIL] 失败: $($failures -join ', ')" -ForegroundColor Red }
    if ($totalSkips -gt $MaxSkips) {
        Write-Host "[FAIL] 跳过场景 $totalSkips > 上限 $MaxSkips——新增 SKIP 必须显式评估（F-09）" -ForegroundColor Red
    }
    exit 1
}
