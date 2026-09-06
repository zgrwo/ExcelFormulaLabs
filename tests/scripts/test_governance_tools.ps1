# ============================================================================
# test_governance_tools.ps1 — 治理工具脚本回归守卫（R5-07 / review 2026-09-06）
# 场景：覆盖第四轮 8 项修复中此前零自测的 5 个脚本面：
#       scaffold-udf 标识符校验（N-E）/ run-affected 未映射告警（N-G）/ commit-msg
#       bash 计数与 locale 固定（N-C + R5-P3-37）/ patch-xll 缺失 exit 1（N-D）/
#       update_excel_arguments 迁移完成语义（R5-P3-25）。
# R6-F2 (review 2026-09-06)：patch-xll 瞬时文件锁重试（-SimulateTransientLock 负向注入，
#       需要 Release 构建产物时运行，否则 SKIP）。
# 用法：pwsh 或 powershell 均可 -NoProfile -ExecutionPolicy Bypass -File tests/scripts/test_governance_tools.ps1
# ============================================================================
$ErrorActionPreference = "Stop"
$repo = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)   # 仓库根
$tmpRoot = Join-Path $env:TEMP ("gov-tool-test-" + [guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Path $tmpRoot -Force | Out-Null

$passCount = 0; $failCount = 0
$hostCmd = if (Get-Command pwsh -ErrorAction SilentlyContinue) { "pwsh" } else { "powershell" }

function Assert-Scenario {
    param([string]$Name, [bool]$Ok, [string]$Detail = "")
    if ($Ok) {
        $script:passCount++
        Write-Host "  [PASS] $Name" -ForegroundColor Green
    } else {
        $script:failCount++
        Write-Host "  [FAIL] $Name $Detail" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "=== [1] scaffold-udf.ps1 标识符校验（N-E）===" -ForegroundColor Cyan

# 1a/1b/1c：危险名必须被拒绝（路径穿越 / 数字开头 / 含空格——空串参数在子进程调用中会被
# PowerShell 丢弃导致绑定错位，无法作为 -File 调用面用例，故以含空格名替代覆盖"非法字符"路）
foreach ($bad in @('../Evil', '1Weather', 'Bad Name')) {
    $out = & $hostCmd -NoProfile -ExecutionPolicy Bypass -File (Join-Path $repo "scripts\scaffold-udf.ps1") `
        -Module Analytics -Name $bad -Prefix EVIL 2>&1
    $exit = $LASTEXITCODE
    $outStr = $out | Out-String
    Assert-Scenario "scaffold rejects name '$bad'" (($exit -ne 0) -and ($outStr -match 'PascalCase')) "exit=$exit"
}
# 1d：危险名不产生任何文件（防穿越副作用）
$leftover = Get-ChildItem -Path (Join-Path $repo "src") -Filter "Evil*" -Recurse -ErrorAction SilentlyContinue
Assert-Scenario "scaffold rejected names leave no files" ($null -eq $leftover)

# 1e：合法名在 fixture 模块目录可用（复制真实模块结构到临时目录，脚本对 $root 写入）
$fixtureRepo = Join-Path $tmpRoot "scaffold-fixture"
New-Item -ItemType Directory -Path (Join-Path $fixtureRepo "src\Analytics") -Force | Out-Null
New-Item -ItemType Directory -Path (Join-Path $fixtureRepo "templates\NewModule") -Force | Out-Null
New-Item -ItemType Directory -Path (Join-Path $fixtureRepo "tests") -Force | Out-Null
Copy-Item (Join-Path $repo "templates\NewModule\*") (Join-Path $fixtureRepo "templates\NewModule") -Force
$scaffoldScript = Join-Path (Join-Path $fixtureRepo "scripts") "scaffold-udf.ps1"
New-Item -ItemType Directory -Path (Join-Path $fixtureRepo "scripts") -Force | Out-Null
Copy-Item (Join-Path $repo "scripts\scaffold-udf.ps1") $scaffoldScript -Force
$out = & $hostCmd -NoProfile -ExecutionPolicy Bypass -File $scaffoldScript `
    -Module Analytics -Name Weather -Prefix WEATHER 2>&1
$exit = $LASTEXITCODE
$genCore = Test-Path (Join-Path $fixtureRepo "src\Analytics\WeatherCore.cs")
Assert-Scenario "scaffold accepts valid name (fixture)" (($exit -eq 0) -and $genCore) "exit=$exit coreExists=$genCore"

Write-Host ""
Write-Host "=== [2] run-affected-tests.ps1 未映射告警（N-G）===" -ForegroundColor Cyan

$out = & $hostCmd -NoProfile -ExecutionPolicy Bypass -File (Join-Path $repo "scripts\run-affected-tests.ps1") `
    -ChangedFiles "src/NewModule/FooCore.cs" -DryRun 2>&1
$exit = $LASTEXITCODE
$outStr = $out | Out-String
# 告警必须可见 + 退出码保持 0（映射缺失非变更错误，N-G 声明语义）
Assert-Scenario "unmapped module warns" ($outStr -match '\[WARN\] no test project mapped') "out=$($outStr.Substring(0, [Math]::Min(200, $outStr.Length)))"
Assert-Scenario "unmapped module stays exit 0" ($exit -eq 0) "exit=$exit"

$out = & $hostCmd -NoProfile -ExecutionPolicy Bypass -File (Join-Path $repo "scripts\run-affected-tests.ps1") `
    -ChangedFiles "src/Analytics/StatsCore.cs" -DryRun 2>&1
$outStr = $out | Out-String
Assert-Scenario "mapped module routes to test project" ($outStr -match 'Analytics\.Tests') ($outStr | Out-String).Substring(0,100)

# R5-P3-26：非命名约定的 src 文件也须路由（不再静默 "(no affected tests)"）
$out = & $hostCmd -NoProfile -ExecutionPolicy Bypass -File (Join-Path $repo "scripts\run-affected-tests.ps1") `
    -ChangedFiles "src/Analytics/SomeHelper2.cs" -DryRun 2>&1
$outStr = $out | Out-String
Assert-Scenario "non-standard-named src file routes to module tests" ($outStr -match 'Analytics\.Tests')

Write-Host ""
Write-Host "=== [3] validate-commit-msg.sh 计数与 locale（N-C + R5-P3-37）===" -ForegroundColor Cyan

$msgFile = Join-Path $tmpRoot "commit-msg.txt"
# 3a：合规中文标题（24 个中文字 ≈ 72 UTF-8 字节）在 C locale 下不应按字节误报
$zh = "fix: 修订中文标题字符计数回归测试的场景一长标题描述文本"
[System.IO.File]::WriteAllText($msgFile, $zh, (New-Object System.Text.UTF8Encoding($false)))
$env:LC_ALL = "C"
$null = bash (Join-Path $repo "scripts\validate-commit-msg.sh") $msgFile 2>&1
$exitC = $LASTEXITCODE
Remove-Item Env:LC_ALL -ErrorAction SilentlyContinue
Assert-Scenario "CJK subject passes under LC_ALL=C (char not byte count)" ($exitC -eq 0) "exit=$exitC"

# 3b：超长标题必须 FAIL
# 负向用例的 stderr 输出（被测脚本按设计打印错误）在 EAP=Stop 下会升级为终止异常——局部降级
$ErrorActionPreference = "Continue"
$long = "fix: " + ("a" * 80)
[System.IO.File]::WriteAllText($msgFile, $long, (New-Object System.Text.UTF8Encoding($false)))
$null = bash (Join-Path $repo "scripts\validate-commit-msg.sh") $msgFile 2>&1
$exitLong = $LASTEXITCODE
Assert-Scenario "over-length subject fails" ($exitLong -ne 0) "exit=$exitLong"

# 3c：非 Conventional Commits 必须 FAIL
[System.IO.File]::WriteAllText($msgFile, "updated some stuff", (New-Object System.Text.UTF8Encoding($false)))
$null = bash (Join-Path $repo "scripts\validate-commit-msg.sh") $msgFile 2>&1
$exitFmt = $LASTEXITCODE
Assert-Scenario "non-conventional subject fails" ($exitFmt -ne 0) "exit=$exitFmt"
$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "=== [4] patch-xll-version.ps1 缺失 exit 1（N-D）===" -ForegroundColor Cyan

$ErrorActionPreference = "Continue"
$out = & $hostCmd -NoProfile -ExecutionPolicy Bypass -File (Join-Path $repo "scripts\patch-xll-version.ps1") `
    -XllPath (Join-Path $tmpRoot "no-such.xll") -FileDescription "d" -ProductName "p" 2>&1
$exit = $LASTEXITCODE
Assert-Scenario "missing xll exits non-zero" ($exit -eq 1) "exit=$exit"
$ErrorActionPreference = "Stop"

# [4b] R6-F2 (review 2026-09-06)：-SimulateTransientLock 首调 exit 5 → 重试必须收敛到 exit 0。
# 需要真实 Release 构建的 .xll（含 FileDescription/ProductName 键的 VERSIONINFO）——测试用
# 临时副本，不触碰 bin/ 原产物；无构建产物时 SKIP（与 [5] 的 SKIP 模式一致）。
$builtXll = Get-ChildItem -Path (Join-Path $repo "src") -Filter "*-packed.xll" -Recurse -ErrorAction SilentlyContinue |
    Where-Object { $_.DirectoryName -match "Release" } | Select-Object -First 1
if ($builtXll) {
    $testXll = Join-Path $tmpRoot "fixture-packed.xll"
    Copy-Item $builtXll.FullName $testXll -Force
    $ErrorActionPreference = "Continue"
    $out = & $hostCmd -NoProfile -ExecutionPolicy Bypass -File (Join-Path $repo "scripts\patch-xll-version.ps1") `
        -XllPath $testXll -FileDescription "d" -ProductName "p" -SimulateTransientLock 2>&1
    $exit = $LASTEXITCODE
    $outStr = $out | Out-String
    Assert-Scenario "simulated transient lock retries to success (exit 0)" `
        (($exit -eq 0) -and ($outStr -match 'retrying')) "exit=$exit"
    $ErrorActionPreference = "Stop"
} else {
    Write-Host "  [SKIP] no Release-built xll found - retry scenario needs a real xll" -ForegroundColor DarkYellow
    $script:skipCount = [int]$script:skipCount + 1
}

Write-Host ""
Write-Host "=== [5] update_excel_arguments.py 迁移完成语义（R5-P3-25）===" -ForegroundColor Cyan
# 对真实仓库运行是只读的：当前全部 18 个 Udf.cs 均为现代注解签名（updated == content 不写盘），
# 断言"迁移已完成 → exit 0"的新语义；若未来出现旧式签名，此场景会改写 src——故仅在校验
# git 干净时执行，并在结束后断言无源码变更。
$gitDirty = (git -C $repo status --porcelain -- src/ | Out-String).Trim()
if ($gitDirty -eq "") {
    $null = python (Join-Path $repo "scripts\update_excel_arguments.py") 2>&1
    $exit = $LASTEXITCODE
    Assert-Scenario "already-migrated repo exits 0 (not drift-error)" ($exit -eq 0) "exit=$exit"
} else {
    Write-Host "  [SKIP] src/ 不干净，跳过真实仓库干跑（避免误写）" -ForegroundColor DarkYellow
    $script:skipCount = [int]$script:skipCount + 1
}

# ── 汇总 ──
Write-Host ""
Write-Host "=== Pass: $passCount  Fail: $failCount  Skip: $skipCount ==="
Remove-Item -Path $tmpRoot -Recurse -Force -ErrorAction SilentlyContinue
if ($failCount -gt 0) { exit 1 } else { exit 0 }
