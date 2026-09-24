# ============================================================================
# test_verify_docs.ps1 — verify-docs.ps1 回归守卫（场景 A–V + 子用例 G2–G8 / H2 / I1·I2 / K1–K3 / L2）
# 场景 N–V（F-09）：检查 2/3/4/6/7/14/15/17/18 的 FAIL 路径负向注入（此前无自测）
# 场景 A：真实仓库副本 → 全部检查通过（基线，防门禁自身回归）
# 场景 B：README 硬编码徽章 → 检查 9 FAIL
# 场景 C：README 断链 → 检查 12 FAIL
# 场景 D：.qoder 镜像漂移 → 检查 13 FAIL
# 场景 E：api-reference UDF 计数漂移 → 检查 1 FAIL
# 场景 F：csproj 描述函数计数漂移 → 检查 11 FAIL
# 场景 G：散文式 UDF 计数漂移（AGENTS.md）→ 检查 16 FAIL
#   G2/G3/G4：中文变体负向注入：
#     G2 `N 项 UDF`（模式 1a 量词扩 项）/ G3 `UDF 数量 N`（模式 1b 倒装）/
#     G4 `N 个函数（UDF）`（模式 1c）——注入后检查 16 必须 FAIL
# 场景 H：CHANGELOG 幽灵条目（无 tag）→ 检查 10 反向 FAIL
# 场景 H2：版本标题悬空（内联/引用链接皆无）→ 检查 10 正向 FAIL（release-please
#   内联链接 `## [X](url)` 须被接受，见场景 A 基线；悬空保护不因兼容而丢失）
# 场景 I：残留 .dna 扫描域——I1 Analytics 根残留 → FAIL；I2 bin/ 下 → PASS
# 场景 J：MathNet 版本双解析失败 → 检查 5 FAIL（双 "?" 恒真 PASS，须注入版本使其失败）
# 用法：pwsh 或 powershell 均可 -NoProfile -ExecutionPolicy Bypass -File tests/scripts/test_verify_docs.ps1
# 被测门禁经 $hostCmd 优先 pwsh7 调用（恒用 powershell 会使 pwsh7 语义差异永不暴露）。
# 注意：本测试复制仓库（排除 bin/obj/.git 等），耗时数秒，仅在 CI windows job 与本地运行。
# ============================================================================
$ErrorActionPreference = "Stop"
$repo = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)   # 仓库根
$verifier = Join-Path $repo "scripts\verify-docs.ps1"
$tmpRoot = Join-Path $env:TEMP ("vd-test-" + [guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Path $tmpRoot -Force | Out-Null

$passCount = 0; $failCount = 0; $skipCount = 0
$hostCmd = if (Get-Command pwsh -ErrorAction SilentlyContinue) { "pwsh" } else { "powershell" }

$script:fixtureSeq = 0
function Copy-RepoFixture {
    # 复制仓库（排除生成目录），返回 fixture 路径。
    # 按序号隔离：固定目录会让所有场景共享同一目录，注入状态跨场景累积
    #（场景 B 的徽章文本残留进 C/D/…；要求"干净全绿"的 I2 被 I1 的残留污染而假失败）。
    $script:fixtureSeq++
    $dst = Join-Path $tmpRoot ("fixture-" + $script:fixtureSeq)
    robocopy $repo $dst /E /XD bin obj .git BenchmarkDotNet.Artifacts logs better-harness __pycache__ /XF *.pyc /NFL /NDL /NJH /NJS /NP | Out-Null
    if ($LASTEXITCODE -ge 8) { throw "robocopy 失败: $LASTEXITCODE" }
    # 目录树契约要求 logs/ 存在（内容不入库），fixture 补建空目录
    New-Item -ItemType Directory -Path (Join-Path $dst "logs") -Force | Out-Null
    return $dst
}

function Run-VerifyDocs {
    param([string]$Dir, [string]$ExpectMsg, [bool]$ExpectFail)
    $output = & $hostCmd -NoProfile -ExecutionPolicy Bypass -File $verifier -RepoRoot $Dir 2>&1
    $exit = $LASTEXITCODE
    $out = ($output | Out-String)
    $ok = $false
    if ($ExpectFail) {
        $ok = ($exit -ne 0) -and ($out -match [regex]::Escape($ExpectMsg))
    } else {
        $ok = ($exit -eq 0)
    }
    if ($ok) {
        $script:passCount++
        Write-Host "  [PASS] $ExpectMsg (exit=$exit)" -ForegroundColor Green
    } else {
        $script:failCount++
        Write-Host "  [FAIL] $ExpectMsg (exit=$exit)" -ForegroundColor Red
        Write-Host ($out | Select-Object -Last 6 | ForEach-Object { "      $_" })
    }
}

# --- 场景 A：基线（真实仓库副本全绿）---
Write-Host "[A] 基线：仓库副本全部检查通过"
$fixture = Copy-RepoFixture
Run-VerifyDocs $fixture "全部通过" $false

# --- 场景 B：README 硬编码数量徽章 ---
Write-Host "[B] README 硬编码徽章应 FAIL（检查 9）"
$readme = Join-Path $fixture "README.md"
[System.IO.File]::AppendAllText($readme, "`n[![Tests](https://img.shields.io/badge/tests-999%20passed-brightgreen)](x)`n", (New-Object System.Text.UTF8Encoding($false)))
Run-VerifyDocs $fixture "hardcoded count badges" $true

# --- 场景 C：README 断链 ---
Write-Host "[C] README 断链应 FAIL（检查 12）"
$fixture2 = Copy-RepoFixture
$readme2 = Join-Path $fixture2 "README.md"
[System.IO.File]::AppendAllText($readme2, "`n[断链示例](./missing-target-xyz.md)`n", (New-Object System.Text.UTF8Encoding($false)))
Run-VerifyDocs $fixture2 "broken links" $true

# --- 场景 D：.qoder 镜像漂移（仅当源仓库含 .qoder 本地镜像时）---
if (Test-Path (Join-Path $repo ".qoder\skills")) {
    Write-Host "[D] .qoder 镜像漂移应 FAIL（检查 13）"
    $fixture3 = Copy-RepoFixture
    $mirror = Join-Path $fixture3 ".qoder\skills\excel-dna-project\SKILL.md"
    [System.IO.File]::AppendAllText($mirror, "`n<!-- drift -->`n", (New-Object System.Text.UTF8Encoding($false)))
    Run-VerifyDocs $fixture3 "mirror" $true
} else {
    Write-Host "[D] .qoder 本地镜像不存在，场景跳过（CI 环境）"
    # SKIP 分账记录，不计入 pass：计入 passCount 与 "SKIP 不计入 pass" 语义相悖。
    $script:skipCount++
}

# --- 场景 E：api-reference UDF 计数漂移（检查 1）---
Write-Host "[E] api-reference UDF 计数漂移应 FAIL（检查 1）"
$fixtureE = Copy-RepoFixture
$apiRef = Join-Path $fixtureE "docs\specification\api-reference.md"
[System.IO.File]::AppendAllText($apiRef, "`r`n" + '| `TEST.FAKE` | (x) | `double` | 测试条目（不应存在） |' + "`r`n", (New-Object System.Text.UTF8Encoding($false)))
Run-VerifyDocs $fixtureE "UDF count" $true

# --- 场景 F：csproj 描述函数计数漂移（检查 11）---
Write-Host "[F] csproj 描述函数计数漂移应 FAIL（检查 11）"
$fixtureF = Copy-RepoFixture
$csproj = Join-Path $fixtureF "src\DataToolkit\DataToolkit.csproj"
$content = [System.IO.File]::ReadAllText($csproj, (New-Object System.Text.UTF8Encoding($false)))
$content = $content -replace '144 个数据处理函数', '143 个数据处理函数'
[System.IO.File]::WriteAllText($csproj, $content, (New-Object System.Text.UTF8Encoding($false)))
Run-VerifyDocs $fixtureF "DataToolkit csproj description count" $true

# --- 场景 G：散文式 UDF 计数漂移（检查 16）---
# 注入必须追加而非替换：`-replace '236 UDF','999 UDF'` 在计数变化后替换目标不存在
# → 注入变 no-op → 场景恒 PASS（回归守卫失效）；追加注入与 G2/G3/G4 一致，不绑定当前计数。
Write-Host "[G] 散文式 UDF 计数漂移应 FAIL（检查 16）"
$fixtureG = Copy-RepoFixture
[System.IO.File]::AppendAllText((Join-Path $fixtureG "AGENTS.md"), "`n999 UDF`n", (New-Object System.Text.UTF8Encoding($false)))
Run-VerifyDocs $fixtureG "Prose UDF counts" $true

# --- 场景 G2/G3/G4：中文变体负向注入 ---
# 注入文本用码点构造：规避本测试脚本在 PS5.1 无 BOM 环境下的编码歧义（与注入目标无关）。
$cXiang  = [char]0x9879                                     # 项
$cShu    = [string][char]0x6570 + [char]0x91CF              # 数量
$cGeFn   = [string][char]0x4E2A + [char]0x51FD + [char]0x6570  # 个函数
$cHanShu = [string][char]0x51FD + [char]0x6570                  # 函数
$cLp     = [char]0xFF08                                     # （
$cRp     = [char]0xFF09                                     # ）

Write-Host "[G2] 中文变体「N 项 UDF」漂移应 FAIL（检查 16 模式 1a）"
$fixtureG2 = Copy-RepoFixture
[System.IO.File]::AppendAllText((Join-Path $fixtureG2 "AGENTS.md"), "`n999 $cXiang UDF`n", (New-Object System.Text.UTF8Encoding($false)))
Run-VerifyDocs $fixtureG2 "Prose UDF counts" $true

Write-Host "[G3] 中文倒装「UDF 数量 N」漂移应 FAIL（检查 16 模式 1b）"
$fixtureG3 = Copy-RepoFixture
[System.IO.File]::AppendAllText((Join-Path $fixtureG3 "AGENTS.md"), "`nUDF $cShu 999`n", (New-Object System.Text.UTF8Encoding($false)))
Run-VerifyDocs $fixtureG3 "Prose UDF counts" $true

Write-Host "[G4] 中文变体「N 个函数（UDF）」漂移应 FAIL（检查 16 模式 1c）"
$fixtureG4 = Copy-RepoFixture
[System.IO.File]::AppendAllText((Join-Path $fixtureG4 "AGENTS.md"), "`n999 $cGeFn$cLp UDF $cRp`n", (New-Object System.Text.UTF8Encoding($false)))
Run-VerifyDocs $fixtureG4 "Prose UDF counts" $true

# G5/G6/G7：R2-15 大小写与同义词盲区 + R2-9 CrossVal 计数算术
Write-Host "[G5] 小写「N udf」漂移应 FAIL（检查 16 IgnoreCase）"
$fixtureG5 = Copy-RepoFixture
[System.IO.File]::AppendAllText((Join-Path $fixtureG5 "AGENTS.md"), "`n999 udf`n", (New-Object System.Text.UTF8Encoding($false)))
Run-VerifyDocs $fixtureG5 "Prose UDF counts" $true

$cGe       = [char]0x4E2A                                     # 个
$cZiDingYi = [string][char]0x81EA + [char]0x5B9A + [char]0x4E49  # 自定义
Write-Host "[G6] 同义词「N 个自定义函数」漂移应 FAIL（检查 16 模式 1d）"
$fixtureG6 = Copy-RepoFixture
[System.IO.File]::AppendAllText((Join-Path $fixtureG6 "AGENTS.md"), "`n999 $cGe$cZiDingYi$cHanShu`n", (New-Object System.Text.UTF8Encoding($false)))
Run-VerifyDocs $fixtureG6 "Prose UDF counts" $true

Write-Host "[G7] CrossVal 计数算术失配（N+M != K）应 FAIL（检查 16 新增断言）"
$fixtureG7 = Copy-RepoFixture
$readmeG7 = Join-Path $fixtureG7 "README.md"
$g7 = [System.IO.File]::ReadAllText($readmeG7)
# 通配注入：旧实现硬编码 '合计 432'，README 计数更新（432→461）后注入静默失效、
# 负向场景假通过——改为匹配任意合计数，测试不再随计数漂移而腐化。
$g7 = [regex]::Replace($g7, '合计\s*\d+', '合计 999')
[System.IO.File]::WriteAllText($readmeG7, $g7, (New-Object System.Text.UTF8Encoding($false)))
Run-VerifyDocs $fixtureG7 "CrossVal" $true

Write-Host "[G8] 源码注释散文计数漂移（.cs）应 FAIL（检查 16 扫描域扩展 P3-6）"
$fixtureG8 = Copy-RepoFixture
[System.IO.File]::AppendAllText((Join-Path $fixtureG8 "src\Foundation\NumericGuard.cs"), "`n// 999 UDF`n", (New-Object System.Text.UTF8Encoding($false)))
Run-VerifyDocs $fixtureG8 "Prose UDF counts" $true

# --- 场景 H：CHANGELOG 幽灵条目（检查 10 反向对账的负向回归守卫）---
# 检查 10 依赖 git tag——fixture 无 .git 时整体 SKIP（无从验证反向）。此处初始化
# git 仓库并为既有语义化版本条目打 tag（注入的 9.9.9 除外），使反向对账真正生效。
Write-Host "[H] CHANGELOG 幽灵条目应 FAIL（检查 10 反向）"
$fixtureH = Copy-RepoFixture
[System.IO.File]::AppendAllText((Join-Path $fixtureH "CHANGELOG.md"), "`n## [9.9.9] - 2026-09-06`n`n### Injected`n`n- ghost entry`n", (New-Object System.Text.UTF8Encoding($false)))
# PS5.1 + EAP=Stop 会把 git 的 stderr 警告（CRLF 提示等，即使 2>&1）升级为异常——
# git 块内临时降级 EAP=Continue 并禁 autocrlf。
$prevEap = $ErrorActionPreference
$ErrorActionPreference = "Continue"
try {
    git -C $fixtureH -c core.autocrlf=false init 2>$null | Out-Null
    git -C $fixtureH config user.email "test@example.com" 2>$null | Out-Null
    git -C $fixtureH config user.name "fixture" 2>$null | Out-Null
    git -C $fixtureH -c core.autocrlf=false add -A 2>$null | Out-Null
    git -C $fixtureH -c core.autocrlf=false commit -m "init" 2>$null | Out-Null
    $changelogH = [System.IO.File]::ReadAllText((Join-Path $fixtureH "CHANGELOG.md"))
    foreach ($m in [regex]::Matches($changelogH, '(?m)^##\s*\[(\d+\.\d+\.\d+)\]')) {
        $v = $m.Groups[1].Value
        if ($v -ne "9.9.9") { git -C $fixtureH tag ("v" + $v) 2>$null | Out-Null }
    }
} finally { $ErrorActionPreference = $prevEap }
Run-VerifyDocs $fixtureH "no tag for: 9.9.9" $true

# --- 场景 H2：CHANGELOG 版本标题悬空（检查 10 正向；inline/ref 链接兼容不得放宽为只查标题）---
Write-Host "[H2] CHANGELOG 版本标题悬空应 FAIL（检查 10 正向）"
$fixtureH2 = Copy-RepoFixture
$clH2 = Join-Path $fixtureH2 "CHANGELOG.md"
$textH2 = [System.IO.File]::ReadAllText($clH2, (New-Object System.Text.UTF8Encoding($false)))
# 剥掉 2.4.0 标题的内联链接（`## [2.4.0](url) (date)` → `## [2.4.0] (date)`）；
# CHANGELOG 底部无 [2.4.0]: 引用定义 → 两种链接形式皆无 = 悬空。
$textH2 = [regex]::Replace($textH2, '## \[2\.4\.0\]\([^)]*\)', '## [2.4.0]')
if ($textH2 -notmatch '## \[2\.4\.0\]\s') { throw "H2 注入失败：未找到 2.4.0 内联标题（CHANGELOG 格式漂移）" }
[System.IO.File]::WriteAllText($clH2, $textH2, (New-Object System.Text.UTF8Encoding($false)))
# 与 H 同法初始化 git 并打全量 tag：仅让「标题悬空」一项失败，隔离于反向幽灵检查。
$prevEap = $ErrorActionPreference
$ErrorActionPreference = "Continue"
try {
    git -C $fixtureH2 -c core.autocrlf=false init 2>$null | Out-Null
    git -C $fixtureH2 config user.email "test@example.com" 2>$null | Out-Null
    git -C $fixtureH2 config user.name "fixture" 2>$null | Out-Null
    git -C $fixtureH2 -c core.autocrlf=false add -A 2>$null | Out-Null
    git -C $fixtureH2 -c core.autocrlf=false commit -m "init" 2>$null | Out-Null
    $changelogH2 = [System.IO.File]::ReadAllText($clH2)
    foreach ($m in [regex]::Matches($changelogH2, '(?m)^##\s*\[(\d+\.\d+\.\d+)\]')) {
        git -C $fixtureH2 tag ("v" + $m.Groups[1].Value) 2>$null | Out-Null
    }
} finally { $ErrorActionPreference = $prevEap }
Run-VerifyDocs $fixtureH2 "missing entries: v2.4.0" $true

# --- 场景 I：残留 .dna 扫描域（检查 8 覆盖 src 全模块；bin/obj 生成物排除）---
Write-Host "[I1] Analytics 根残留 .dna 应 FAIL（检查 8 域扩展）"
$fixtureI1 = Copy-RepoFixture
[System.IO.File]::WriteAllText((Join-Path $fixtureI1 "src\Analytics\Analytics-AddIn-net8.0.dna"), "<stale/>", (New-Object System.Text.UTF8Encoding($false)))
Run-VerifyDocs $fixtureI1 "No residual .dna" $true

Write-Host "[I2] bin/ 下构建产物 .dna 不应误报（生成物排除）"
$fixtureI2 = Copy-RepoFixture
New-Item -ItemType Directory -Path (Join-Path $fixtureI2 "src\Analytics\bin\Debug") -Force | Out-Null
[System.IO.File]::WriteAllText((Join-Path $fixtureI2 "src\Analytics\bin\Debug\Analytics-AddIn-net8.0.dna"), "<transient/>", (New-Object System.Text.UTF8Encoding($false)))
Run-VerifyDocs $fixtureI2 "全部通过" $false

# --- 场景 J：MathNet 版本双解析失败（双 "?" 恒真 PASS，须注入版本使其失败）---
Write-Host "[J] MathNet 版本双解析失败应 FAIL（检查 5）"
$fixtureJ = Copy-RepoFixture
$ctxJ = Join-Path $fixtureJ "docs\governance\context.md"
$contentJ = [System.IO.File]::ReadAllText($ctxJ, (New-Object System.Text.UTF8Encoding($false)))
$contentJ = $contentJ -replace 'MathNet\.Numerics\s+[0-9.]+', 'MathNet.Numerics vX'
[System.IO.File]::WriteAllText($ctxJ, $contentJ, (New-Object System.Text.UTF8Encoding($false)))
Run-VerifyDocs $fixtureJ "unparseable" $true

# --- 场景 K：CHANGELOG「UDF 总数 X→Y」区间链（检查 16 模式 2）---
# 区间终点 ≤ 当前计数且相邻区间首尾相接（按起点升序排序后校验）；历史区间终点不要求 == 当前计数。
Write-Host "[K1] CHANGELOG 区间终点超过当前 UDF 数应 FAIL（检查 16 模式 2）"
$fixtureK1 = Copy-RepoFixture
[System.IO.File]::AppendAllText((Join-Path $fixtureK1 "CHANGELOG.md"), "`n- 注入：UDF 总数 236→999`n", (New-Object System.Text.UTF8Encoding($false)))
Run-VerifyDocs $fixtureK1 "Prose UDF counts" $true

Write-Host "[K2] CHANGELOG 区间链不连续应 FAIL（检查 16 模式 2；终点合法但与前段不接续）"
$fixtureK2 = Copy-RepoFixture
[System.IO.File]::AppendAllText((Join-Path $fixtureK2 "CHANGELOG.md"), "`n- 注入：UDF 总数 237→240`n", (New-Object System.Text.UTF8Encoding($false)))
Run-VerifyDocs $fixtureK2 "Prose UDF counts" $true

# K3：新版本区间天然位于 CHANGELOG 顶部（新→旧文档顺序）。
# 按文档顺序比较相邻区间，对合法的新版本条目必然误报（实测注入 `236→240` 假 FAIL）。
# 回归守卫：移除现有区间后，注入逆序（高区间在前）的连续链 220→240 / 200→220，应全绿。
Write-Host "[K3] CHANGELOG 顶部新版本区间（新→旧顺序）应 PASS（R7-1 方向修复）"
$fixtureK3 = Copy-RepoFixture
$clK3 = Join-Path $fixtureK3 "CHANGELOG.md"
$k3Text = [System.IO.File]::ReadAllText($clK3, (New-Object System.Text.UTF8Encoding($false)))
$k3Text = [regex]::Replace($k3Text, 'UDF\s*总数\s*\d+\s*→\s*\d+', 'UDF 总数（K3 移除）')
[System.IO.File]::WriteAllText($clK3, ($k3Text + "`n- K3：UDF 总数 220→240`n- K3：UDF 总数 200→220`n"), (New-Object System.Text.UTF8Encoding($false)))
Run-VerifyDocs $fixtureK3 "全部通过" $false

Write-Host "[L] [Fact] 计数声明漂移应 FAIL（检查 20）"
$fixtureL = Copy-RepoFixture
[System.IO.File]::AppendAllText((Join-Path $fixtureL "AGENTS.md"), "`n- 999 个 [Fact]（注入）`n", (New-Object System.Text.UTF8Encoding($false)))
Run-VerifyDocs $fixtureL "Fact count claims" $true

# L2：按 [Fact]/[Theory] 分型统计——只统计 [Fact] 时 [Theory] 声明即使错误也无门禁。
Write-Host "[L2] [Theory] 计数声明漂移应 FAIL（检查 20 分型统计）"
$fixtureL2 = Copy-RepoFixture
[System.IO.File]::AppendAllText((Join-Path $fixtureL2 "AGENTS.md"), "`n- 999 个 [Theory]（注入）`n", (New-Object System.Text.UTF8Encoding($false)))
Run-VerifyDocs $fixtureL2 "Fact count claims" $true

# --- 场景 M：version.txt 与 props <Version> 漂移（检查 10b，release-please 版本锚点）---
Write-Host "[M] version.txt 漂移应 FAIL（检查 10b）"
$fixtureM = Copy-RepoFixture
[System.IO.File]::WriteAllText((Join-Path $fixtureM "version.txt"), "9.9.9`n", (New-Object System.Text.UTF8Encoding($false)))
Run-VerifyDocs $fixtureM "version.txt" $true

# --- 场景 N：检查 2/3/4/6/7/14/15/17/18 的 FAIL 路径（F-09：此前约 9 项无负向自测）---
Write-Host "[N] api-reference 缺源码 UDF 条目应 FAIL（检查 2 全覆盖）"
$fixtureN = Copy-RepoFixture
$apiN = Join-Path $fixtureN "docs\specification\api-reference.md"
$textN = [System.IO.File]::ReadAllText($apiN, (New-Object System.Text.UTF8Encoding($false)))
# 改名而非删行：保持 UDF 计数不变，隔离检查 2（否则检查 1 同时 FAIL）。
$textN = $textN.Replace('STR.REVERSE', 'STR.REVERSX')
[System.IO.File]::WriteAllText($apiN, $textN, (New-Object System.Text.UTF8Encoding($false)))
Run-VerifyDocs $fixtureN "UDF full coverage" $true

Write-Host "[O] skill 缺 RangeExport 应 FAIL（检查 3）"
$fixtureO = Copy-RepoFixture
$skillO = Join-Path $fixtureO "skills\excel-dna-project.md"
$textO = [System.IO.File]::ReadAllText($skillO, (New-Object System.Text.UTF8Encoding($false)))
# 替换目标须完全消除 'RangeExport' 子串（'XRangeExport' 仍含之，会假通过）。
[System.IO.File]::WriteAllText($skillO, $textO.Replace('RangeExport', 'RangeExp0rt'), (New-Object System.Text.UTF8Encoding($false)))
Run-VerifyDocs $fixtureO "skill.md RangeExport" $true

Write-Host "[P] README 含内部类名应 FAIL（检查 4）"
$fixtureP = Copy-RepoFixture
[System.IO.File]::AppendAllText((Join-Path $fixtureP "README.md"), "`nElementWiseMapper`n", (New-Object System.Text.UTF8Encoding($false)))
Run-VerifyDocs $fixtureP "README no internal class names" $true

Write-Host "[Q] src 注入裸 catch 应 FAIL（检查 6 红线）"
$fixtureQ = Copy-RepoFixture
[System.IO.File]::AppendAllText((Join-Path $fixtureQ "src\Foundation\NumericGuard.cs"), "`n// injected`ncatch { }`n", (New-Object System.Text.UTF8Encoding($false)))
Run-VerifyDocs $fixtureQ "No bare catch" $true

Write-Host "[R] 删除 .dna 模板应 FAIL（检查 7）"
$fixtureR = Copy-RepoFixture
Remove-Item (Join-Path $fixtureR "src\Analytics\Analytics-AddIn-net8.dna.tpl") -Force
Run-VerifyDocs $fixtureR "net8 tpl missing" $true

Write-Host "[S] project-structure 树声明不存在条目应 FAIL（检查 14）"
$fixtureS = Copy-RepoFixture
$psS = Join-Path $fixtureS "docs\governance\project-structure.md"
$textS = [System.IO.File]::ReadAllText($psS, (New-Object System.Text.UTF8Encoding($false)))
# 在 governance/ 子块末条（└──）后追加一条不存在的声明（保持缩进层级；目录栈不误变）。
$textS = $textS.Replace("│   │   └── ai-review-prompt.md     #     AI 深度审查 Prompt（变更审查模板）",
                        "│   │   ├── ai-review-prompt.md     #     AI 深度审查 Prompt（变更审查模板）`r`n│   │   └── fake-governance.md       #     注入（不存在）")
[System.IO.File]::WriteAllText($psS, $textS, (New-Object System.Text.UTF8Encoding($false)))
Run-VerifyDocs $fixtureS "tree entries" $true

Write-Host "[T] AGENTS 树顶层目录与 structure 不一致应 FAIL（检查 15）"
$fixtureT = Copy-RepoFixture
$agentsT = Join-Path $fixtureT "AGENTS.md"
$textT = [System.IO.File]::ReadAllText($agentsT, (New-Object System.Text.UTF8Encoding($false)))
$textT = $textT.Replace("├── build/                        # 构建配置说明",
                        "├── build/                        # 构建配置说明`r`n├── fake-top/                     # 注入（不存在）")
[System.IO.File]::WriteAllText($agentsT, $textT, (New-Object System.Text.UTF8Encoding($false)))
Run-VerifyDocs $fixtureT "top dirs" $true

Write-Host "[U] api-reference 参数名与源码漂移应 FAIL（检查 17）"
$fixtureU = Copy-RepoFixture
$apiU = Join-Path $fixtureU "docs\specification\api-reference.md"
$textU = [System.IO.File]::ReadAllText($apiU, (New-Object System.Text.UTF8Encoding($false)))
$textU = [regex]::Replace($textU, '(?m)^(\| `STR\.REVERSE` \| \()text(\))', '${1}textx${2}')
[System.IO.File]::WriteAllText($apiU, $textU, (New-Object System.Text.UTF8Encoding($false)))
Run-VerifyDocs $fixtureU "ExcelArgument" $true

Write-Host "[V] src 新增未声明文件应 FAIL（检查 18 反向）"
$fixtureV = Copy-RepoFixture
[System.IO.File]::WriteAllText((Join-Path $fixtureV "src\Foundation\UndeclaredProbe.cs"), "namespace ExcelFormulaLabs.Foundation { internal static class UndeclaredProbe { } }`n", (New-Object System.Text.UTF8Encoding($false)))
Run-VerifyDocs $fixtureV "undeclared" $true

# --- 汇总 ---
Remove-Item -Recurse -Force $tmpRoot
Write-Host ""
Write-Host "=== Pass: $passCount  Fail: $failCount  Skip: $skipCount ==="
if ($failCount -gt 0) { exit 1 } else { exit 0 }