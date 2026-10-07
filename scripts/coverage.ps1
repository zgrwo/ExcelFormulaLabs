#Requires -Version 5.1
<#
.SYNOPSIS
    ExcelFormulaLabs 覆盖率门禁（net8.0，行 + 分支）。
.DESCRIPTION
    **阈值的单一实现**：ci.yml 的 coverage job 直接调用本脚本，不再各自硬编码
    （此前 ci.yml 与 coverage.ps1 两处各写一份 92/86/86，需人工同步）。

    每个模块同时卡**行覆盖率**与**分支覆盖率**——只卡行会让"补测试刷行数"与真实
    分支质量脱钩（2026-10-07 实测：Analytics 行 92.68% 而分支仅 80.80%）。

    阈值按实测值留余量以吸收抖动，但**余量不是固定承诺**——它随实测漂移，
    引用时以本脚本输出为准（2026-10-07 复测，net8.0 + Include 过滤 + SandboxStatus
    文件 I/O 测试补齐、**Foundation 分支用例补齐**后）：
        模块          line    阈值  余量      branch  阈值  余量
        Foundation    96.18   92    4.18      90.25   84    6.25
        Analytics     92.79   86    6.79      81.02   76    5.02
        DataToolkit   89.34   86    3.34      85.41   82    3.41
    **当前最小余量 3.34 点（DataToolkit line）**。此前的最小余量是 Foundation branch 2.56 点：
    Foundation 分支只覆盖了守卫的"命中"侧（3 参 MapOverMulti 的逐格哨兵、DictOperations 的
    NaN/±Inf 键、整秒 DateTime 键、SafeKey 的 Boolean:False / 空 2D 数组、降序排序的三路分区），
    补齐后 86.56 → **90.25**。此前的注释曾写"≥4 个点余量"且基线记为 DataToolkit 90.21/86.04，
    两项均与实测不符（P2-2：同一脚本实测 DataToolkit 行 87.50/分支 84.85，行余量仅 1.5 点）；
    阈值本身自始未调整。

    已知本机坑（不改变门禁语义，仅影响本机复现）：裸跑本脚本时 DataToolkit 步的
    `PackExcelAddIn` 可能报 `Win32Exception (110)`「系统无法打开指定的设备或文件」
    （`ResourceResolverWin.End` → `EndUpdateResource` 失败）——前置的 Foundation/Analytics
    两次 `dotnet test` 留下的 MSBuild 工作节点复用会持有文件句柄。规避（二选一，均已实测）：
    运行前设 `$env:MSBUILDDISABLENODEREUSE='1'`，或给本脚本内的 `dotnet test` 加 `-m:1`。

    为何需要本脚本：tests/*/coverage-local/ 下的历史报告**未加 Include 过滤**，
    Foundation 类在 Analytics 报告里显示 ~0%，数字误导（Analytics 53.5% vs 实际 90.2%）。
    比对门禁请一律走本脚本或 CI。
.NOTES
    用法：powershell -File scripts/coverage.ps1 [-AnalyticsBranch 76] ...
    调低阈值仅用于本地排查，不得提交降低后的阈值。
#>
param(
    [string]$RepoRoot = (Split-Path -Parent $PSScriptRoot),
    [int]$Foundation = 92,           [int]$FoundationBranch = 84,
    [int]$Analytics = 86,            [int]$AnalyticsBranch = 76,
    [int]$DataToolkit = 86,          [int]$DataToolkitBranch = 82,
    [switch]$NoRestore
)

$ErrorActionPreference = "Stop"
$RepoRoot = [System.IO.Path]::GetFullPath($RepoRoot)

$modules = @(
    [PSCustomObject]@{ Name = 'Foundation';  Project = 'tests/Foundation.Tests/Foundation.Tests.csproj';   Tfm = 'net8.0';         Include = '[Foundation]*';  Threshold = $Foundation;  BranchThreshold = $FoundationBranch },
    [PSCustomObject]@{ Name = 'Analytics';   Project = 'tests/Analytics.Tests/Analytics.Tests.csproj';     Tfm = 'net8.0-windows'; Include = '[Analytics]*';   Threshold = $Analytics;   BranchThreshold = $AnalyticsBranch },
    [PSCustomObject]@{ Name = 'DataToolkit'; Project = 'tests/DataToolkit.Tests/DataToolkit.Tests.csproj'; Tfm = 'net8.0-windows'; Include = '[DataToolkit]*'; Threshold = $DataToolkit; BranchThreshold = $DataToolkitBranch }
)

$failures = @()
Push-Location $RepoRoot
try {
    foreach ($m in $modules) {
        Write-Host ""
        Write-Host "===== Coverage: $($m.Name) (line >= $($m.Threshold)%, branch >= $($m.BranchThreshold)%) =====" -ForegroundColor Cyan

        # R1-07 假绿修复：运行前清理旧报告。coverlet 在测试失败/构建失败时**不重写**报告，
        # 残留的旧报告会被下方"按最新文件读取"逻辑当成本轮结果 → 失败被静默读成 PASS。
        $projDir = Join-Path $RepoRoot (Split-Path $m.Project -Parent)
        $coverageDir = Join-Path $projDir 'coverage'
        if (Test-Path $coverageDir) {
            Get-ChildItem -Path $coverageDir -Filter '*.cobertura.xml' -File -ErrorAction SilentlyContinue |
                Remove-Item -Force -ErrorAction SilentlyContinue
        }

        # MSBuild 的 -p: 以逗号作属性分隔符：多阈值必须转义为 %2C，否则报 MSB1006。
        $args = @(
            'test', $m.Project, '-m:1', '-f', $m.Tfm,
            '-p:CollectCoverage=true', '-p:CoverletOutputFormat=cobertura',
            "-p:CoverletOutput=coverage/$($m.Name.ToLower())",
            "-p:Threshold=$($m.Threshold)%2C$($m.BranchThreshold)",
            '-p:ThresholdType=line%2Cbranch', '-p:ThresholdStat=total',
            "-p:Include=$($m.Include)", '--nologo'
        )
        if ($NoRestore) { $args += '--no-restore' }
        & dotnet @args
        $dotnetExit = $LASTEXITCODE

        # 解析报告做汇总（可读输出）；退出码始终是权威判据（报告存在也不豁免）。
        $report = Get-ChildItem -Path $coverageDir -Filter '*.cobertura.xml' -ErrorAction SilentlyContinue |
            Sort-Object LastWriteTime -Descending | Select-Object -First 1
        $ratesOk = $false
        $lPct = 0.0; $bPct = 0.0
        if ($report) {
            $doc = New-Object System.Xml.XmlDocument
            $doc.Load($report.FullName)
            $lPct = [Math]::Round([double]$doc.DocumentElement.GetAttribute('line-rate') * 100, 2)
            $bPct = [Math]::Round([double]$doc.DocumentElement.GetAttribute('branch-rate') * 100, 2)
            $ratesOk = ($lPct -ge $m.Threshold) -and ($bPct -ge $m.BranchThreshold)
            if ($ratesOk -and $dotnetExit -eq 0) {
                Write-Host "  [PASS] $($m.Name): line $lPct%  branch $bPct%" -ForegroundColor Green
            } else {
                Write-Host "  [FAIL] $($m.Name): line $lPct% (>= $($m.Threshold)) / branch $bPct% (>= $($m.BranchThreshold)) (exit $dotnetExit)" -ForegroundColor Red
            }
        }
        if ($dotnetExit -ne 0) {
            $failures += "$($m.Name) dotnet test exit $dotnetExit"
        } elseif (-not $report) {
            $failures += "$($m.Name) cobertura report not found (test/build did not complete)"
        } elseif (-not $ratesOk) {
            $failures += "$($m.Name) line $lPct% / branch $bPct% below gate"
        }
    }
} finally {
    Pop-Location
}

Write-Host ""
Write-Host "============================================================"
if ($failures.Count -eq 0) {
    Write-Host "  [PASS] All coverage gates passed (line + branch)." -ForegroundColor Green
    Write-Host "============================================================"
    exit 0
} else {
    Write-Host "  [BLOCKED] $($failures.Count) coverage gate failure(s):" -ForegroundColor Red
    foreach ($f in $failures) { Write-Host "    - $f" -ForegroundColor Red }
    Write-Host "============================================================"
    exit 1
}
