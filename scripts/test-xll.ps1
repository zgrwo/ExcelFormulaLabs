# test-xll.ps1 - 本地 Excel XLL 加载/卸载冒烟测试（不入 CI）
# 用法: powershell -NoProfile -ExecutionPolicy Bypass -File scripts/test-xll.ps1 [-BaseDir <编译产物目录>]
# review-2026-08-30：产物名随 v2.2.0 命名变更更新——旧 `Analytics-AddIn64-packed.xll` 已不存在，
# 现为 `<Module>-AddIn-<tfm>[-64]-packed.xll`（net48/net8.0 各出 32/64 两变体）。本脚本测试 64 位变体。
param(
    [string]$BaseDir = 'D:\Workspace\zgrwo\VBA\DeepSeek\ClaudeCode\已编译文件'
)

$xlls = @(
    @{Name='A-net48'; Path="$BaseDir\net48\Analytics-AddIn-net48-64-packed.xll"; F='=STATS.MEAN({1,2,3,4,5})'; E=3.0},
    @{Name='D-net48'; Path="$BaseDir\net48\DataToolkit-AddIn-net48-64-packed.xll"; F='=STR.REVERSE("hello")'; E='olleh'},
    @{Name='A-net8';  Path="$BaseDir\net8.0-windows\Analytics-AddIn-net8.0-64-packed.xll"; F='=STATS.MEAN({1,2,3,4,5})'; E=3.0},
    @{Name='D-net8';  Path="$BaseDir\net8.0-windows\DataToolkit-AddIn-net8.0-64-packed.xll"; F='=STR.REVERSE("hello")'; E='olleh'}
)

$ROUNDS = 4
$pass = 0; $fail = 0

for ($r = 1; $r -le $ROUNDS; $r++) {
    Get-Process Excel -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
    Start-Sleep 1
    Write-Output "=== Round $r/$ROUNDS ==="
    $xl = New-Object -ComObject Excel.Application
    $xl.Visible = $false; $xl.DisplayAlerts = $false
    foreach ($x in $xlls) {
        if (-not (Test-Path $x.Path)) { Write-Output "  SKIP $($x.Name): missing"; $fail++; continue }
        try {
            $xl.RegisterXLL($x.Path) | Out-Null
            $val = $xl.Evaluate($x.F)
            if ($val -eq $x.E) { Write-Output "  PASS $($x.Name)"; $pass++ }
            else { Write-Output "  FAIL $($x.Name): expected=$($x.E) actual=$val"; $fail++ }
        } catch { Write-Output "  FAIL $($x.Name): load error"; $fail++ }
    }
    try {
        $wb = $xl.Workbooks.Add(); $ws = $wb.Worksheets(1)
        $ws.Cells(1,1).Formula = '=STATS.MEAN({1,2,3,4,5,6,7,8,9,10})'
        Start-Sleep 0.5
        if ($ws.Cells(1,1).Value -eq 5.5) { Write-Output "  PASS cell"; $pass++ }
        else { Write-Output "  FAIL cell: value=$($ws.Cells(1,1).Value)"; $fail++ }
        $wb.Close($false)
    } catch { Write-Output "  FAIL cell"; $fail++ }
    $xl.Quit()
    [System.Runtime.InteropServices.Marshal]::ReleaseComObject($xl) | Out-Null
    Start-Sleep 1
}

Get-Process Excel -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
Write-Output ""
Write-Output "========================================"
Write-Output "RESULT: $pass / $($pass+$fail) passed ($ROUNDS rounds x 5 tests)"
Write-Output "========================================"
