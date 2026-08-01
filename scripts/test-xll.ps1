$BUILT = 'D:\Workspace\zgrwo\VBA\DeepSeek\ClaudeCode\已编译文件'
$xlls = @(
    @{Name='A-net48'; Path="$BUILT\net48\Analytics-AddIn64-packed.xll"; F='=STATS.MEAN({1,2,3,4,5})'; E=3.0},
    @{Name='D-net48'; Path="$BUILT\net48\DataToolkit-AddIn64-packed.xll"; F='=STR.REVERSE("hello")'; E='olleh'},
    @{Name='A-net8';  Path="$BUILT\net8.0-windows\Analytics-AddIn64-packed.xll"; F='=STATS.MEAN({1,2,3,4,5})'; E=3.0},
    @{Name='D-net8';  Path="$BUILT\net8.0-windows\DataToolkit-AddIn64-packed.xll"; F='=STR.REVERSE("hello")'; E='olleh'}
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
