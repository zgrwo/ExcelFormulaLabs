# ============================================================================
# test_governance_tools.ps1 — 治理工具脚本回归守卫
# 场景：覆盖 5 个脚本面：scaffold-udf 标识符校验 / run-affected 未映射告警 /
#       commit-msg bash 计数与 locale 固定 / patch-xll 缺失 exit 1 /
#       update_excel_arguments 迁移完成语义；
#       [4c] 另覆盖 MSBuild Exec stderr 语义（R1-01：IgnoreStandardErrorWarningFormat
#       静态断言 + Exec 写 stderr/exit 0 的 MSBuild 级功能复现）。
# patch-xll 瞬时文件锁重试（-SimulateTransientLock 负向注入，
#       需要 Release 构建产物时运行，否则 SKIP）。
# 用法：pwsh 或 powershell 均可 -NoProfile -ExecutionPolicy Bypass -File tests/scripts/test_governance_tools.ps1
# ============================================================================
$ErrorActionPreference = "Stop"
$repo = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)   # 仓库根
$tmpRoot = Join-Path $env:TEMP ("gov-tool-test-" + [guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Path $tmpRoot -Force | Out-Null

$passCount = 0; $failCount = 0; $skipCount = 0
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

# SKIP 单列计数、不计入 pass（与 verify-docs 的 Check-Skip 同语义）：
# 把环境缺失计为 pass 会掩盖"该场景根本没跑"。
function Skip-Scenario {
    param([string]$Name)
    $script:skipCount++
    Write-Host "  [SKIP] $Name" -ForegroundColor Yellow
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

# 1f：夹具含元数据模板时必须写出 udf-metadata/<Name>Udf.json（ADR-0011 的单一真源），
#     且缺 tools/udfgen.py 时**只告警不失败**（本夹具正是无 tools/ 的情形）。
$genMeta = Test-Path (Join-Path $fixtureRepo "udf-metadata\WeatherUdf.json")
Assert-Scenario "scaffold emits metadata source of truth" $genMeta "metaExists=$genMeta"
Assert-Scenario "scaffold skips missing udfgen without failing" (($out | Out-String) -match '\[SKIP\] 未找到 tools/udfgen.py') "exit=$exit"

# 1g：端到端——夹具补齐 tools/udfgen.py 后，应生成 src/<Module>/<Name>Udf.g.cs。
#     这是新流程（模板 → 元数据 → 生成）真正的回归守卫；无 python 环境时跳过。
if (Get-Command python -ErrorAction SilentlyContinue) {
    New-Item -ItemType Directory -Path (Join-Path $fixtureRepo "tools") -Force | Out-Null
    Copy-Item (Join-Path $repo "tools\udfgen.py") (Join-Path $fixtureRepo "tools\udfgen.py") -Force
    Remove-Item (Join-Path $fixtureRepo "udf-metadata\WeatherUdf.json") -Force -ErrorAction SilentlyContinue
    Remove-Item (Join-Path $fixtureRepo "src\Analytics\WeatherCore.cs") -Force -ErrorAction SilentlyContinue
    $out2 = & $hostCmd -NoProfile -ExecutionPolicy Bypass -File $scaffoldScript `
        -Module Analytics -Name Rain -Prefix RAIN 2>&1
    $exit2 = $LASTEXITCODE
    $genCs = Test-Path (Join-Path $fixtureRepo "src\Analytics\RainUdf.g.cs")
    Assert-Scenario "scaffold generates .g.cs end-to-end" (($exit2 -eq 0) -and $genCs) "exit=$exit2 gcsExists=$genCs"
} else {
    Write-Host "  [SKIP] python 不可用，跳过 scaffold 端到端生成场景"
}

# 1h：P3-6——工具**存在**但生成失败（坏 JSON）必须非零退出。
#     旧实现只打印 [WARN] 后 exit 0：CI/调用方把"模板已落盘但没有 .g.cs"的半成品当成功。
#     与 1f 的"缺工具（[SKIP]，exit 0）"语义不同——后者是环境不适用，前者是流程失败。
if (Get-Command python -ErrorAction SilentlyContinue) {
    $jsonTpl = Join-Path $fixtureRepo "templates\NewModule\{Name}Udf.json.template"
    # 保留 {Name} 占位符但破坏 JSON 结构 → udfgen.py json.loads 抛错（真实失败路径，非桩）
    [System.IO.File]::WriteAllText($jsonTpl,
        "{ `"name`": `"{Name}Udf`", BROKEN",
        (New-Object System.Text.UTF8Encoding($false)))
    # 负向用例的 stderr（udfgen 的 Python traceback）在 EAP=Stop 下会升级为终止异常——局部降级
    #（与下方 3b/3c、[4] 的负向场景同法）。
    $ErrorActionPreference = "Continue"
    $out3 = & $hostCmd -NoProfile -ExecutionPolicy Bypass -File $scaffoldScript `
        -Module Analytics -Name Broken -Prefix BROKEN 2>&1
    $exit3 = $LASTEXITCODE
    $ErrorActionPreference = "Stop"
    $outStr3 = $out3 | Out-String
    $tplOnDisk = (Test-Path (Join-Path $fixtureRepo "src\Analytics\BrokenCore.cs")) -and
                 (Test-Path (Join-Path $fixtureRepo "udf-metadata\BrokenUdf.json"))
    $gcsAbsent = -not (Test-Path (Join-Path $fixtureRepo "src\Analytics\BrokenUdf.g.cs"))
    Assert-Scenario "scaffold fails (exit != 0) when udfgen generation fails" `
        (($exit3 -ne 0) -and $tplOnDisk -and $gcsAbsent -and ($outStr3 -match '\[FAIL\] UDF 声明生成失败')) `
        "exit=$exit3 templates=$tplOnDisk noGcs=$gcsAbsent"
    # 恢复正常 JSON 模板：后续场景/重复运行不得受污染
    Copy-Item (Join-Path $repo "templates\NewModule\{Name}Udf.json.template") $jsonTpl -Force
} else {
    Write-Host "  [SKIP] python 不可用，跳过 scaffold 生成失败退出码场景"
}

Write-Host ""
Write-Host "=== [2] run-affected-tests.ps1 未映射告警（N-G）===" -ForegroundColor Cyan

$out = & $hostCmd -NoProfile -ExecutionPolicy Bypass -File (Join-Path $repo "scripts\run-affected-tests.ps1") `
    -ChangedFiles "src/NewModule/FooCore.cs" -DryRun 2>&1
$exit = $LASTEXITCODE
$outStr = $out | Out-String
# 告警必须可见 + 退出码保持 0（映射缺失非变更错误语义）
Assert-Scenario "unmapped module warns" ($outStr -match '\[WARN\] no test project mapped') "out=$($outStr.Substring(0, [Math]::Min(200, $outStr.Length)))"
Assert-Scenario "unmapped module stays exit 0" ($exit -eq 0) "exit=$exit"

$out = & $hostCmd -NoProfile -ExecutionPolicy Bypass -File (Join-Path $repo "scripts\run-affected-tests.ps1") `
    -ChangedFiles "src/Analytics/StatsCore.cs" -DryRun 2>&1
$outStr = $out | Out-String
Assert-Scenario "mapped module routes to test project" ($outStr -match 'Analytics\.Tests') ($outStr | Out-String).Substring(0,100)

# 非命名约定的 src 文件也须路由（不得静默 "(no affected tests)"）
$out = & $hostCmd -NoProfile -ExecutionPolicy Bypass -File (Join-Path $repo "scripts\run-affected-tests.ps1") `
    -ChangedFiles "src/Analytics/SomeHelper2.cs" -DryRun 2>&1
$outStr = $out | Out-String
Assert-Scenario "non-standard-named src file routes to module tests" ($outStr -match 'Analytics\.Tests')

Write-Host ""
Write-Host "=== [3] validate-commit-msg.sh 计数与 locale（N-C + R5-P3-37）===" -ForegroundColor Cyan

# 本场景需要 bash（validate-commit-msg.sh 是 POSIX shell 脚本）。无 bash 的机器上
# **必须 SKIP 而非崩溃**：旧实现直接调 bash，在 $ErrorActionPreference="Stop" 下
# "command not found" 升级为终止异常 → 脚本连汇总行都不打印、exit 1，
# 看起来像"测试失败"而实为环境缺失（2026-10-07 本机复现）。
if (-not (Get-Command bash -ErrorAction SilentlyContinue)) {
    Skip-Scenario "validate-commit-msg.sh（本机无 bash）"
} else {

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

}

Write-Host ""
Write-Host "=== [4] patch-xll-version.ps1 缺失 exit 1（N-D）===" -ForegroundColor Cyan

$ErrorActionPreference = "Continue"
$out = & $hostCmd -NoProfile -ExecutionPolicy Bypass -File (Join-Path $repo "scripts\patch-xll-version.ps1") `
    -XllPath (Join-Path $tmpRoot "no-such.xll") -FileDescription "d" -ProductName "p" 2>&1
$exit = $LASTEXITCODE
Assert-Scenario "missing xll exits non-zero" ($exit -eq 1) "exit=$exit"
$ErrorActionPreference = "Stop"

# [4b] -SimulateTransientLock 首调 exit 5 → 重试必须收敛到 exit 0。
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
Write-Host "=== [4c] R1-01：MSBuild Exec stderr 语义（IgnoreStandardErrorWarningFormat）===" -ForegroundColor Cyan

# (a) 两个 csproj 的 8 个 patch-xll Exec 必须带 IgnoreStandardErrorWarningFormat="true"
#（否则 patch-xll 首败 stderr 会让"重试后成功"的构建仍报 MSB3073 -1）。
foreach ($csprojRel in @("src\DataToolkit\DataToolkit.csproj", "src\Analytics\Analytics.csproj")) {
    $cp = Join-Path $repo $csprojRel
    $cpText = [System.IO.File]::ReadAllText($cp, [System.Text.Encoding]::UTF8)
    # 仅统计 patch-xll-version.ps1 的 Exec（VerifyPackOutput 的 verify-pack Exec 不在此列）。
    $patchExecs = [regex]::Matches($cpText, '<Exec [^>]*patch-xll-version\.ps1[^>]*>')
    $execIgnored = @($patchExecs | Where-Object { $_.Value -match 'IgnoreStandardErrorWarningFormat="true"' }).Count
    Assert-Scenario "patch-xll Execs ignore stderr format ($csprojRel)" `
        ($patchExecs.Count -gt 0 -and $patchExecs.Count -eq $execIgnored) `
        "patchExec=$($patchExecs.Count) ignored=$execIgnored"
}

# (b) 功能复现：Exec 子进程写 stderr 且 exit 0——带属性必须构建成功；
# 不带属性必须 MSB3073 失败（证明该属性正是 R1-01 根因的开关）。
$projWith = @'
<Project>
  <Target Name="StderrExit0">
    <Exec Command="powershell -NoProfile -Command &quot;[Console]::Error.WriteLine('ERROR: boom'); exit 0&quot;" IgnoreStandardErrorWarningFormat="true" />
  </Target>
</Project>
'@
$projPlain = @'
<Project>
  <Target Name="StderrExit0">
    <Exec Command="powershell -NoProfile -Command &quot;[Console]::Error.WriteLine('ERROR: boom'); exit 0&quot;" />
  </Target>
</Project>
'@
$projWithPath = Join-Path $tmpRoot "stderr-ignored.proj"
$projPlainPath = Join-Path $tmpRoot "stderr-plain.proj"
[System.IO.File]::WriteAllText($projWithPath, $projWith, (New-Object System.Text.UTF8Encoding($false)))
[System.IO.File]::WriteAllText($projPlainPath, $projPlain, (New-Object System.Text.UTF8Encoding($false)))
$ErrorActionPreference = "Continue"
$null = dotnet msbuild $projWithPath -t:StderrExit0 -nologo 2>&1
$exitIgnored = $LASTEXITCODE
$null = dotnet msbuild $projPlainPath -t:StderrExit0 -nologo 2>&1
$exitPlain = $LASTEXITCODE
$ErrorActionPreference = "Stop"
Assert-Scenario "Exec + IgnoreStandardErrorWarningFormat tolerates stderr/exit0" `
    ($exitIgnored -eq 0) "exit=$exitIgnored"
Assert-Scenario "Exec without attribute fails on stderr/exit0 (root cause reproduced)" `
    ($exitPlain -ne 0) "exit=$exitPlain"

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

Write-Host ""
Write-Host "=== [6] 编码不变量：.ps1 内容必须是合法 UTF-8（含非 ASCII 的还须带 BOM）===" -ForegroundColor Cyan

# 为什么需要这条：Windows PowerShell 5.1 在**非 UTF-8 ACP** 的机器（如 GitHub Actions 的英文
# runner，ACP=cp1252）上会把无 BOM 的 .ps1 按 ANSI 解码——文件里的中文变乱码，轻则输出错乱、
# 重则解析失败退出 1。本仓已踩过两次：一次是"8 个 PS1 脚本补 UTF-8 BOM"；一次是 2026-10-07
# scripts/verify-pack.ps1（原本无 BOM、内容以英文为主）被加入中文注释后，Benchmarks 与 CI 的
# Release 构建双双因 verify-pack 退出 1 而失败。另注：编辑工具会**静默剥掉 BOM**，必须由门禁兜住。
#
# P3-5 补洞（review-2026-10-08 两处漏检均已实测复现）：
#   ① 旧实现 `if ($hasBom) { continue }` 在**读内容之前**早退——BOM 只声明"我是 UTF-8"，
#      并不保证字节真是 UTF-8。BOM + GBK 内容的文件整类放行（实测 [PASS]，Pass: 20 Fail: 0，exit 0），
#      而 PS 5.1 解析它同样乱码/失败，正是本检查要拦的形态。
#   ② 旧实现只扫 `git ls-files '*.ps1'`（已跟踪）——未跟踪的新脚本不在扫描域。实测对照：
#      同一份无 BOM 中文 .ps1 未跟踪时全绿，`git add -N` 后立刻 FAIL，证明洞在"跟踪状态"。
# 现口径：扫描域 = 已跟踪 ∪ 未跟踪（--others --exclude-standard，尊重 .gitignore）；
#        每个文件的**内容**都必须能被严格 UTF-8 解码（throwOnInvalidBytes），与有无 BOM 无关；
#        含非 ASCII 的还必须带 BOM（无 BOM 时 PS 5.1 按 ACP 解码，与内容是否合法 UTF-8 无关）。
$tracked = @(git -C $repo ls-files '*.ps1')
$untracked = @(git -C $repo ls-files --others --exclude-standard '*.ps1')
$ps1Files = @($tracked + $untracked | Sort-Object -Unique)
# 扫描域非空守卫：git 不可用/paths 拼错时枚举得 0 个文件，本检查会静默空转成 PASS。
Assert-Scenario "ps1 scan domain non-empty (tracked + untracked)" `
    ($ps1Files.Count -gt 0) "scanned=$($ps1Files.Count) tracked=$($tracked.Count) untracked=$($untracked.Count)"

$strictUtf8 = New-Object System.Text.UTF8Encoding($false, $true)
$noBomWithCjk = @()
$invalidUtf8 = @()
foreach ($rel in $ps1Files) {
    $full = Join-Path $repo $rel
    if (-not (Test-Path $full)) { continue }
    $bytes = [System.IO.File]::ReadAllBytes($full)
    $hasBom = ($bytes.Length -ge 3 -and $bytes[0] -eq 239 -and $bytes[1] -eq 187 -and $bytes[2] -eq 191)
    $offset = if ($hasBom) { 3 } else { 0 }
    $text = $null
    try {
        $text = $strictUtf8.GetString($bytes, $offset, $bytes.Length - $offset)
    } catch {
        # DecoderFallbackException：字节序列不是合法 UTF-8（BOM + GBK 内容即此形态）
        $invalidUtf8 += $rel
        continue
    }
    if (-not $hasBom -and $text -match '[^\x00-\x7F]') { $noBomWithCjk += $rel }
}
Assert-Scenario "every .ps1 (tracked + untracked) decodes as strict UTF-8" `
    ($invalidUtf8.Count -eq 0) "非法 UTF-8: $($invalidUtf8 -join ', ')"
Assert-Scenario "every .ps1 with non-ASCII has a UTF-8 BOM" ($noBomWithCjk.Count -eq 0) "缺 BOM: $($noBomWithCjk -join ', ')"

# ── [7] 测试宿主 culture 固定（2026-10-10 审查 E-2）──
# 背景：3 个 xUnit 工程都有 TestCultureSetup.cs 以 [ModuleInitializer] 固定 InvariantCulture，
# 但 Python↔C# 通道的 C# 半边（tests/CrossValRunner）没有任何固定 → tr-TR/de-DE 机器上
# 字符串型 UDF（STR.FORMAT / DT.* 格式化 / RANGE.TOJSON）与 ResultSerializer 输出会偏离，
# 产生"环境归因"的间歇性失败。此场景把 4 个宿主全部纳入守卫。
Write-Host ""
Write-Host "=== [7] 测试宿主 culture 固定（E-2）==="
$cultureHosts = @(
    @{ Name = 'Foundation.Tests';  Rel = 'tests/Foundation.Tests/TestCultureSetup.cs' },
    @{ Name = 'Analytics.Tests';   Rel = 'tests/Analytics.Tests/TestCultureSetup.cs' },
    @{ Name = 'DataToolkit.Tests'; Rel = 'tests/DataToolkit.Tests/TestCultureSetup.cs' },
    @{ Name = 'CrossValRunner';    Rel = 'tests/CrossValRunner/Program.cs' }
)
$cultureBad = @()
foreach ($h in $cultureHosts) {
    $p = Join-Path $repo $h.Rel
    if (-not (Test-Path $p)) { $cultureBad += "$($h.Name): 文件缺失 $($h.Rel)"; continue }
    $txt = [System.IO.File]::ReadAllText($p, [System.Text.Encoding]::UTF8)
    if ($txt -notmatch 'DefaultThreadCurrentCulture\s*=\s*CultureInfo\.InvariantCulture') {
        $cultureBad += "$($h.Name): 未固定 DefaultThreadCurrentCulture"
    }
}
Assert-Scenario "all 4 test hosts pin InvariantCulture" ($cultureBad.Count -eq 0) "未固定: $($cultureBad -join '; ')"

# ── [8] verify-pack 必须检出**截断**产物（2026-10-10）──
# 失败的 ExcelDnaPack 会留下部分写入的 *-packed.xll（实测 1,168,896 与 658,944 字节，
# 正常为 3,730,432 / 1,196,544），此后每次 pack 都失败在 EndUpdateResource 且错误信息
# 完全指不到真因。旧判据 `$minSize = 100*1024` 对 1.17 MB 的截断产物**照常放行**。
# 用 Tfm=net8.0-windows + Module=Analytics：该组合会跳过第 2 节（SQLite 原生，仅 DataToolkit）
# 与第 4 节（net48 依赖闭包），使本场景**只**检验第 1 节，避免其它节的 FAIL 造成假通过。
Write-Host ""
Write-Host "=== [8] verify-pack 截断检测 ==="
$fxPack = Join-Path $env:TEMP ("packfx-" + [guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Path $fxPack -Force | Out-Null
function New-FakePe([int]$Size) {
    $b = New-Object byte[] $Size
    $b[0] = 0x4D; $b[1] = 0x5A                      # MZ
    $peOff = 0x80
    [BitConverter]::GetBytes([int]$peOff).CopyTo($b, 0x3C)
    $b[$peOff] = 0x50; $b[$peOff + 1] = 0x45; $b[$peOff + 2] = 0; $b[$peOff + 3] = 0   # PE\0\0
    return $b
}
[System.IO.File]::WriteAllBytes((Join-Path $fxPack "Analytics-AddIn-net8.0-packed.xll"),    (New-FakePe 1657856))
# 用**真实事故**的比值（1,168,896 / 1,657,856 = 0.705）而非极端值——极端比值（如 0.18）连旧的
# 宽松判据 [0.6, 1.7] 都能拦住，锁不住收窄后的 [0.85, 1.18]。
[System.IO.File]::WriteAllBytes((Join-Path $fxPack "Analytics-AddIn-net8.0-64-packed.xll"), (New-FakePe 1168896))
$outPack = (& powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $repo "scripts/verify-pack.ps1") -PublishDir $fxPack -Module Analytics -Tfm net8.0-windows 2>&1 | Out-String)
$packExit = $LASTEXITCODE
Assert-Scenario "truncated packed xll detected (bitness size mismatch)" ($packExit -ne 0 -and $outPack -match "size mismatch") "exit=$packExit"
Remove-Item $fxPack -Recurse -Force -ErrorAction SilentlyContinue

# ── 汇总 ──
Write-Host ""
Write-Host "=== Pass: $passCount  Fail: $failCount  Skip: $skipCount ==="
Remove-Item -Path $tmpRoot -Recurse -Force -ErrorAction SilentlyContinue
if ($failCount -gt 0) { exit 1 } else { exit 0 }
