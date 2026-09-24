# verify-docs.ps1 - 文档一致性验证（唯一实现；verify-docs.sh 为包装器）
# ============================================================================
# 用法：.\scripts\verify-docs.ps1 [-RepoRoot <path>]
# 20 个编号项（每个编号项含多条断言，运行时逐条输出：基线 27 条 = 26 PASS + 1 SKIP；
# R08 review 2026-09-05：检查 19 于 2026-08-31 加入后头注计数漏更 → 2026-09-15 起采用
# 「编号项 + 运行时断言数」双口径，引用者请以脚本尾部 Pass/Fail/Skip 输出为准）：
#   1.  UDF 数量：api-reference.md 为准，与源码 [ExcelFunction] 一致
#   2.  UDF 全覆盖：每个源码 UDF 在 api-reference.md 有条目
#   3.  skill.md 含 RangeExport（数据工具模块技能覆盖）
#   4.  架构术语：skill.md 含 MapOver；README 无内部类名（ElementWiseMapper）
#   5.  版本匹配：context.md 与 Analytics.csproj 的 MathNet.Numerics 版本一致
#   6.  无裸 catch {}（红线）
#   7.  .dna 模板完整（net48 / net8）
#   8.  无残留生成 .dna
#   9.  README 无硬编码数量徽章（tests-/UDFs-，数字只能来自 api-reference）
#  10.  CHANGELOG 覆盖全部 v* git tag；Directory.Build.props 版本 == 最新 tag
#  11.  模块 csproj Description 函数数量 == 该模块 [ExcelFunction] 计数
#  12.  Markdown 相对链接无断链（排除 http/https/mailto/#/Windows 绝对路径）
#  13.  .qoder skills 镜像与 skills/ 一致（变换后字节比对，见 sync-qoder-skills.ps1）
#  14.  project-structure.md 目录树声明的条目全部真实存在（含 docs/plans 反向：实际文件必须被声明）
#  15.  AGENTS.md 与 project-structure.md 顶层目录集合一致（双目录树防漂移）
#  16.  散文式 UDF 计数（AGENTS/CONTRIBUTING/CHANGELOG/注释/Total 表）== 推导值
#  17.  [ExcelArgument] 名称 ↔ api-reference 参数列（自动比对，剥离可选标记）
#  18.  src/ 实际文件必须被目录树声明（反向检查：存在→声明）
#  19.  文档版本头 == Directory.Build.props <Version>（specification / user-manual）
#  20.  [Fact] 计数声明（specification 等 md）== tests/**/*.cs 实测计数（review-2026-09-13）
#
# 注意：文件一律用显式 UTF-8 读取（本脚本兼容 Windows PowerShell 5.1 与 pwsh 7）。
# ============================================================================
param(
    [string]$RepoRoot = (Split-Path -Parent $PSScriptRoot)
)
# review-2026-08-29：统一为真实长路径——GitHub Actions 的 $env:TEMP 是 8.3 短名
# （C:\Users\RUNNER~1\...），而 Get-ChildItem 返回长名（runneradmin），两者长度差
# 3 字符，Substring($RepoRoot.Length) 前缀错位会让检查 16/18 的相对路径变成
# "ure/src/..." 而全部失配（test_verify_docs 场景 A 在 CI 上复现）。
$RepoRoot = [System.IO.Path]::GetFullPath($RepoRoot)
# 修剪尾分隔符：否则 Substring($RepoRoot.Length) 多剥一字符（检查 16/18 相对路径错位）；
# 盘根（如 D:\，长度 3）不动。
if ($RepoRoot.Length -gt 3) { $RepoRoot = $RepoRoot.TrimEnd('\', '/') }
$ErrorActionPreference = "Continue"
$script:pass = 0; $script:fail = 0; $script:skip = 0

function Read-Utf8 {
    param([string]$Path)
    if (-not (Test-Path $Path)) { return $null }
    return [System.IO.File]::ReadAllText($Path, [System.Text.Encoding]::UTF8)
}

function Check {
    param([string]$Label, [string]$Result)
    if ($Result -eq "OK") { Write-Host "  [PASS] $Label"; $script:pass++ }
    else { Write-Host "  [FAIL] ${Label}: ${Result}"; $script:fail++ }
}

# SKIP 分支（如无 git 环境）单列 skip 计数、不计入 pass：把 SKIP 计为 pass
# 会让 "Pass: 23" 含跳过项，掩盖未执行的检查。
function Check-Skip {
    param([string]$Label, [string]$Reason)
    Write-Host "  [SKIP] $Label ($Reason)"
    $script:skip++
}

# ---------- 1. UDF 数量 ----------
$docUdfs = (Select-String -Path (Join-Path $RepoRoot "docs/specification/api-reference.md") -Pattern '^\| `[A-Z]+\.[A-Z]' | Measure-Object).Count
$codeUdfs = (Get-ChildItem -Path (Join-Path $RepoRoot "src") -Recurse -Filter "*.cs" |
    Where-Object { $_.FullName -notmatch "[/\\](obj|bin)[/\\]" } |
    Select-String -Pattern 'ExcelFunction\(Name\s*=\s*"([^"]*)"' -AllMatches |
    ForEach-Object { $_.Matches } | ForEach-Object { $_.Groups[1].Value } |
    Sort-Object -Unique | Measure-Object).Count
if ($docUdfs -eq $codeUdfs) { Check "UDF count ($docUdfs)" "OK" }
else { Check "UDF count" "doc=$docUdfs code=$codeUdfs" }

# ---------- 2. UDF 全覆盖 ----------
$apiContent = Read-Utf8 (Join-Path $RepoRoot "docs/specification/api-reference.md")
$missing = @()
Get-ChildItem -Path (Join-Path $RepoRoot "src") -Recurse -Filter "*.cs" |
    Where-Object { $_.FullName -notmatch "[/\\](obj|bin)[/\\]" } |
    Select-String -Pattern 'ExcelFunction\(Name\s*=\s*"([^"]*)"' -AllMatches |
    ForEach-Object { $_.Matches } | ForEach-Object { $_.Groups[1].Value } |
    Sort-Object -Unique | ForEach-Object {
        if ($apiContent -notmatch [regex]::Escape($_)) { $missing += $_ }
    }
if ($missing.Count -eq 0) { Check "UDF full coverage" "OK" }
else { Check "UDF full coverage" "missing: $($missing -join ', ')" }

# ---------- 3. skill.md 含 RangeExport ----------
$skillContent = Read-Utf8 (Join-Path $RepoRoot "skills/excel-dna-project.md")
if ($skillContent -match 'RangeExport') { Check "skill.md RangeExport" "OK" }
else { Check "skill.md RangeExport" "missing" }

# ---------- 4. 架构术语 ----------
if ($skillContent -match 'MapOver') { Check "skill.md MapOver term" "OK" }
else { Check "skill.md MapOver term" "missing" }
$readmeContent = Read-Utf8 (Join-Path $RepoRoot "README.md")
# README 缺失须显式 FAIL：否则本检查与检查 9 静默 PASS。
if ($null -eq $readmeContent) { Check "README.md present" "missing" }
elseif ($readmeContent -match 'ElementWiseMapper') { Check "README no internal class names" "should use MapOver not internal class" }
else { Check "README no internal impl details" "OK" }

# ---------- 5. MathNet 版本匹配 ----------
$docVer = if ((Read-Utf8 (Join-Path $RepoRoot "docs/governance/context.md")) -match 'MathNet\.Numerics\s+([0-9.]+)') { $Matches[1] } else { "?" }
$csprojVer = if ((Read-Utf8 (Join-Path $RepoRoot "src/Analytics/Analytics.csproj")) -match 'MathNet\.Numerics.*Version="([0-9.]+)"') { $Matches[1] } else { "?" }
# 任一侧解析失败（"?"）→ FAIL：两侧同为 "?" 会恒真 PASS，
# 版本一致性门禁整体空转。
if ($docVer -eq "?" -or $csprojVer -eq "?") { Check "MathNet version" "unparseable (doc=$docVer csproj=$csprojVer)" }
elseif ($docVer -eq $csprojVer) { Check "MathNet version ($docVer)" "OK" }
else { Check "MathNet version" "doc=$docVer csproj=$csprojVer" }

# ---------- 6. 无裸 catch ----------
# 须读全文正则（允许 catch 与 { 之间的行注释），行号由偏移计算：Select-String 行级匹配
# 对跨行写法盲（`catch // 注释` 换行 `{` 为合法 C# 且注释文本阻断 \s*）。
$bareCatches = Get-ChildItem -Path (Join-Path $RepoRoot "src") -Recurse -Filter "*.cs" |
    Where-Object { $_.FullName -notmatch "[/\\](obj|bin)[/\\]" } | ForEach-Object {
        $t = Read-Utf8 $_.FullName
        if (-not $t) { return }
        foreach ($rx in [regex]::Matches($t, 'catch(?:\s*//[^\r\n]*)*\s*\{')) {
            [PSCustomObject]@{ Path = $_.FullName; LineNumber = ($t.Substring(0, $rx.Index) -split "`n").Count }
        }
    }
$bcArr = @($bareCatches)
if ($bcArr.Count -eq 0) { Check "No bare catch" "OK" }
else { Check "No bare catch" "$($bcArr.Count) found: $($bcArr | ForEach-Object { "$($_.Path):$($_.LineNumber)" })" }

# ---------- 7. .dna 模板完整 ----------
# .dna 模板由 csproj 推导：凡 csproj 引用 .dna 的 src 模块目录必须齐 net48+net8 模板——
# 硬编码 DataToolkit 两个 tpl 路径会让其余模块模板零门禁（删除无拦截）。
$dnaModules = Get-ChildItem -Path (Join-Path $RepoRoot "src") -Directory | Where-Object {
    (Get-ChildItem $_.FullName -Filter "*.csproj" -File -ErrorAction SilentlyContinue |
        ForEach-Object { [System.IO.File]::ReadAllText($_.FullName, [System.Text.Encoding]::UTF8) }) -match '\.dna'
}
$tplMissing = @()
foreach ($md in $dnaModules) {
    if (-not (Get-ChildItem $md.FullName -Filter "*-net48.dna.tpl" -File -ErrorAction SilentlyContinue)) { $tplMissing += "$($md.Name): net48 tpl missing" }
    if (-not (Get-ChildItem $md.FullName -Filter "*-net8.dna.tpl" -File -ErrorAction SilentlyContinue)) { $tplMissing += "$($md.Name): net8 tpl missing" }
}
if ($dnaModules.Count -eq 0) { Check ".dna templates" "no add-in module found (csproj referencing .dna)" }
elseif ($tplMissing.Count -eq 0) { Check ".dna templates ($($dnaModules.Count) modules)" "OK" }
else { Check ".dna templates" "$($tplMissing -join '; ')" }

# ---------- 8. 无残留生成 .dna ----------
# generated .dna files carry TFM suffixes (*-net48.dna / *-net8.0.dna);
# a no-suffix pattern misses stale files from interrupted builds.
# 扫描域须为 src 全模块：单扫 DataToolkit 时 Analytics 等模块的残留不设防。
$residual = Get-ChildItem -Path (Join-Path $RepoRoot "src") -Recurse -Filter "*.dna" -File -ErrorAction SilentlyContinue |
    Where-Object { $_.Name -notlike "*.tpl" -and $_.FullName -notmatch "[/\\](obj|bin)[/\\]" }
if (-not $residual) { Check "No residual .dna" "OK" }
else {
    $rel = $residual | ForEach-Object { $_.FullName.Substring($RepoRoot.Length) -replace '\\', '/' }
    Check "No residual .dna" "found residual: $($rel -join ', ')"
}

# ---------- 9. README 无硬编码数量徽章 ----------
# ① README.md 与 README.en.md 均纳入扫描；② README 缺失 → FAIL。
$badgeFail = @()
foreach ($rf in @("README.md", "README.en.md")) {
    $rc = Read-Utf8 (Join-Path $RepoRoot $rf)
    if ($null -eq $rc) { $badgeFail += "$rf missing"; continue }
    if ($rc -match 'badge/tests-') { $badgeFail += "${rf}: tests-" }
    if ($rc -match 'badge/UDFs-') { $badgeFail += "${rf}: UDFs-" }
}
if ($badgeFail.Count -eq 0) { Check "README no hardcoded count badges" "OK" }
else { Check "README no hardcoded count badges" "found $($badgeFail -join ', ')（数量只能见 api-reference.md）" }

# ---------- 10. CHANGELOG 覆盖全部 v* tag + props 版本 == 最新 tag ----------
$changelog = Read-Utf8 (Join-Path $RepoRoot "CHANGELOG.md")
$tags = & git -C $RepoRoot tag --list "v*" 2>$null
if ($LASTEXITCODE -ne 0) {
    Check-Skip "CHANGELOG covers all tags" "no git"
} elseif (-not $tags) {
    Check-Skip "CHANGELOG covers all tags" "no v* tags"
} else {
    # 只检查语义化版本 tag（vX.Y.Z），跳过 v1.0.0-net8.0 这类历史命名
    $semverTags = @($tags | Where-Object { $_ -match '^v\d+\.\d+\.\d+$' })
    $untracked = @()
    foreach ($t in $semverTags) {
        $ver = $t -replace '^v', ''
        if ($changelog -notmatch [regex]::Escape("## [$ver]")) { $untracked += $t; continue }
        # 章节头与版本链接必须成对：只查 `## [X]` 时链接丢失会门禁放过（[Unreleased] 悬空案例）。
        # 链接两种形式均合法：① 内联 `## [X](url) (date)`（release-please 生成，2026-09-24
        # v2.4.0 实证）；② 引用定义 `[X]: url`（发版自动化前的手工条目）。两者皆无 = 悬空标题。
        $hasInlineLink = $changelog -match [regex]::Escape("## [$ver](")
        $hasRefLink = $changelog -match ("(?m)^\s*" + [regex]::Escape("[$ver]:"))
        if (-not $hasInlineLink -and -not $hasRefLink) { $untracked += $t }
    }
    if ($untracked.Count -eq 0) { Check "CHANGELOG covers all tags ($($semverTags.Count) tags)" "OK" }
    else { Check "CHANGELOG covers all tags" "missing entries: $($untracked -join ', ')" }

    # N11 (review-2026-09-05)：反向对账——原检查只做 tag→CHANGELOG 单向校验，CHANGELOG 新增
    # `## [X.Y.Z]` 条目而忘记打 tag（或 tag 名打错）时门禁放过。补反向：CHANGELOG 每个语义化
    # 版本条目必须有对应 v* tag。已知例外 1.0.8：历史幽灵条目（无 v1.0.8 tag，发版制度建立前
    # 遗留；文档面处置归文档代理，此处显式豁免）。
    $ghostAllow = @("1.0.8")
    $ghosts = @()
    foreach ($m in [regex]::Matches($changelog, '(?m)^##\s*\[(\d+\.\d+\.\d+)\]')) {
        $v = $m.Groups[1].Value
        if ($ghostAllow -notcontains $v -and ($semverTags -notcontains "v$v")) { $ghosts += $v }
    }
    if ($ghosts.Count -eq 0) { Check "CHANGELOG entries all tagged" "OK" }
    else { Check "CHANGELOG entries all tagged" "no tag for: $($ghosts -join ', ')" }

    # H3 (review-2026-08-29)：字符串排序会将 v2.9.0 排在 v2.10.0 之前，误选最新 tag。
    # 改为语义化版本（major/minor/patch）比较。
    $latestTag = $semverTags |
        Sort-Object -Property @{ Expression = {
                $v = $_ -replace '^v', ''
                $parts = $v -split '\.'
                [long]$parts[0] * 1000000 + [long]$parts[1] * 1000 + [long]$parts[2]
            } } -Descending | Select-Object -First 1
    $props = Read-Utf8 (Join-Path $RepoRoot "src/Directory.Build.props")
    $propsVer = if ($props -match '<Version>([0-9.]+)</Version>') { $Matches[1] } else { "?" }
    $latestVer = $latestTag -replace '^v', ''
    if ($propsVer -eq $latestVer) { Check "Directory.Build.props version == latest tag ($latestVer)" "OK" }
    else { Check "Directory.Build.props version" "props=$propsVer latest-tag=$latestVer" }

    # G1 (review-2026-08-29)：AssemblyVersion / FileVersion 必须与 <Version> 一致
    #（X.Y.Z → X.Y.Z.0）。v2.2.1 曾漏改 AV/FV 漂移到 2.2.0.0。
    # R5-P3-22 (review 2026-09-06)：支持 4 段 Version（AV/FV 须与之一致）；其余形态
    # （prerelease 等）显式 SKIP 计数——原实现 4 段/非 3 段静默跳过且无 SKIP，检查可能空转。
    # 2026-09-23：AV/FV 改为 $(Version).0 派生形式（release-please 只 bump <Version>）——
    # 正则放宽为 [^<]+ 以捕获派生表达式，并优先判定派生形式。
    $propsAv = if ($props -match '<AssemblyVersion>([^<]+)</AssemblyVersion>') { $Matches[1] } else { "?" }
    $propsFv = if ($props -match '<FileVersion>([^<]+)</FileVersion>') { $Matches[1] } else { "?" }
    if ($propsAv -eq '$(Version).0' -and $propsFv -eq '$(Version).0') {
        Check "AssemblyVersion/FileVersion == Version (derived)" "OK"
    } elseif ($propsVer -match '^\d+\.\d+\.\d+$') {
        $expect = "$propsVer.0"
        if ($propsAv -eq $expect -and $propsFv -eq $expect) {
            Check "AssemblyVersion/FileVersion == Version" "OK"
        } else {
            Check "AssemblyVersion/FileVersion == Version" "expect=$expect av=$propsAv fv=$propsFv"
        }
    } elseif ($propsVer -match '^\d+\.\d+\.\d+\.\d+$') {
        if ($propsAv -eq $propsVer -and $propsFv -eq $propsVer) {
            Check "AssemblyVersion/FileVersion == Version" "OK"
        } else {
            Check "AssemblyVersion/FileVersion == Version" "expect=$propsVer av=$propsAv fv=$propsFv"
        }
    } else {
        Check-Skip "AssemblyVersion/FileVersion == Version" "Version '$propsVer' not in comparable X.Y.Z[.W] form"
    }
}

# ---------- 10b. version.txt == Directory.Build.props <Version>（release-please 版本锚点）----------
# release-please simple 策略以 version.txt 为版本文件；漂移会让下一个版本从错误基线递增。
# 与 git tag 无关，无条件执行（fixture 无 git 也可负向验证）。
$propsForVt = Read-Utf8 (Join-Path $RepoRoot "src/Directory.Build.props")
$propsVerVt = if ($propsForVt -match '<Version>([0-9.]+)</Version>') { $Matches[1] } else { "?" }
$versionTxtRaw = Read-Utf8 (Join-Path $RepoRoot "version.txt")
if (-not $versionTxtRaw) {
    Check "version.txt == Version" "version.txt missing/unreadable"
} else {
    $versionTxt = $versionTxtRaw.Trim()
    if ($versionTxt -eq $propsVerVt) { Check "version.txt == Version ($propsVerVt)" "OK" }
    else { Check "version.txt == Version" "version.txt=$versionTxt props=$propsVerVt" }
}

# ---------- 11. 模块 csproj 描述数量 == [ExcelFunction] 计数 ----------
# ① 计命中数而非行数（同行双 [ExcelFunction 会少计）；
# ② 描述数量锚定 <Description> 标签（防 csproj 前部注释里的"N 个"被首匹配吞掉）；
# ③ 反向守卫——src/ 下出现含 [ExcelFunction] 而不在名单的模块目录即指名 FAIL（防新模块漏对账）。
foreach ($module in @("Analytics", "DataToolkit")) {
    $count = (Select-String -Path (Get-ChildItem (Join-Path $RepoRoot "src/$module") -Recurse -Filter "*.cs" -File | Where-Object { $_.FullName -notmatch "[/\\](obj|bin)[/\\]" } | ForEach-Object { $_.FullName }) -Pattern '\[ExcelFunction' -AllMatches |
        ForEach-Object { $_.Matches } | Measure-Object).Count
    $csprojText = Read-Utf8 (Join-Path $RepoRoot "src/$module/$module.csproj")
    $descNum = if ($csprojText -match '<Description>[^<]*?(\d+)\s*个') { [int]$Matches[1] } else { -1 }
    if ($descNum -eq $count) { Check "$module csproj description count ($count)" "OK" }
    else { Check "$module csproj description count" "desc=$descNum code=$count" }
}
$knownModules = @("Analytics", "DataToolkit")
$unlistedModules = Get-ChildItem -Path (Join-Path $RepoRoot "src") -Directory | Where-Object {
    $knownModules -notcontains $_.Name -and
    (Get-ChildItem $_.FullName -Recurse -Filter "*.cs" -File -ErrorAction SilentlyContinue |
        Where-Object { $_.FullName -notmatch "[/\\](obj|bin)[/\\]" } |
        Select-String -Pattern '\[ExcelFunction' | Measure-Object).Count -gt 0
}
if ($unlistedModules.Count -eq 0) { Check "csproj count covers all UDF modules" "OK" }
else { Check "csproj count covers all UDF modules" "unlisted module(s) with UDFs: $($unlistedModules.Name -join ', ')" }

# ---------- 12. Markdown 相对链接断链扫描 ----------
# 归一化为 '/' 后再匹配排除正则：排除正则若用反斜杠形态，Linux 下 FullName 用 '/' 时失效
# （.git/TestResults/logs 内 .md 会被误扫）。
$mdFiles = Get-ChildItem -Path $RepoRoot -Recurse -Filter "*.md" |
    Where-Object { ($_.FullName -replace '\\', '/') -notmatch '\.git/|/bin/|/obj/|\.qoder/|TestResults/|/logs/|BenchmarkDotNet\.Artifacts/' }
$broken = @()
foreach ($f in $mdFiles) {
    $text = Read-Utf8 $f.FullName
    # 读文件失败须 SKIP 计数输出（与检查 19 同法），对齐"SKIP 不计入 pass"语义，
    # 防止文件不可读时检查 12 静默空转。
    if (-not $text) { Check-Skip "Markdown broken links" "unreadable: $($f.Name)"; continue }
    foreach ($m in [regex]::Matches($text, '\]\(([^)]+)\)')) {
        $target = $m.Groups[1].Value.Trim()
        if ($target -match '^(https?://|mailto:|#|ftp://|file://)') { continue }
        if ($target -match '^[A-Za-z]:[\\/]') { continue }  # Windows 绝对路径不检查
        $pathPart = ($target -split '#')[0].Trim()
        if ($pathPart -eq '') { continue }
        $candidate = Join-Path $f.DirectoryName $pathPart
        try { $resolved = [System.IO.Path]::GetFullPath($candidate) } catch { continue }
        if (-not (Test-Path $resolved)) {
            $broken += "$($f.Name) -> $target"
        }
    }
}
if ($broken.Count -eq 0) { Check "Markdown broken links" "OK" }
else { Check "Markdown broken links" "$($broken.Count): $($broken -join ' | ')" }

# ---------- 13. .qoder skills 镜像一致性（本地工具镜像，不入库；缺失则跳过）----------
if (Test-Path (Join-Path $RepoRoot ".qoder/skills")) {
    $psExe = if ($PSVersionTable.PSEdition -eq 'Core') { 'pwsh' } else { 'powershell' }
    & $psExe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $RepoRoot "scripts/sync-qoder-skills.ps1") -CheckOnly 2>&1 | ForEach-Object { Write-Host "      $_" }
    if ($LASTEXITCODE -eq 0) { Check ".qoder skills mirror" "OK" }
    else { Check ".qoder skills mirror" "drifted (run scripts/sync-qoder-skills.ps1)" }
} else {
    Check-Skip ".qoder skills mirror" "not present (local-only tool mirror)"
}

# ---------- 14. project-structure.md 目录树条目存在性 ----------
# 范围注记（F-21，review 2026-09-06）：检查 14 前向（声明→存在）覆盖全树；检查 18 反向
# （存在→声明）有意仅覆盖 src/——tests/docs/skills 等处新增文件不强制逐个登记。
# 若需扩展反向范围，须先确认现有树已 100% 登记目标目录，否则门禁立即全量误报。
function Get-TreeEntries {
    param([string]$TreeText)
    $entries = @()
    $stack = New-Object System.Collections.Stack
    foreach ($line in ($TreeText -split "`n")) {
        if ($line -notmatch '[├└]──') { continue }
        $parts = $line -split '[├└]──'
        $indent = $parts[0].Length
        $name = ($parts[1] -split '#')[0].Trim().TrimEnd()
        if ($name -eq '' -or $name -eq '...' -or $name.StartsWith('(')) { continue }
        while ($stack.Count -gt 0 -and $stack.Peek().Indent -ge $indent) { [void]$stack.Pop() }
        $parent = if ($stack.Count -gt 0) { $stack.Peek().Path } else { '' }
        if ($name.EndsWith('/')) {
            $dirName = $name.TrimEnd('/')
            $path = if ($parent) { "$parent/$dirName" } else { $dirName }
            $stack.Push([pscustomobject]@{ Indent = $indent; Path = $path })
            $entries += [pscustomobject]@{ Path = $path; IsDir = $true }
        } else {
            foreach ($part in ($name -split ' / ')) {
                $path = if ($parent) { "$parent/$part" } else { $part }
                $entries += [pscustomobject]@{ Path = $path; IsDir = $false }
            }
        }
    }
    return $entries
}

function Get-TreeBlock {
    param([string]$Text)
    $m = [regex]::Match($Text, '```\s*\r?\n(ExcelFormulaLabs/.*?)```', [System.Text.RegularExpressions.RegexOptions]::Singleline)
    if (-not $m.Success) { return $null }
    return $m.Groups[1].Value
}

$structText = Read-Utf8 (Join-Path $RepoRoot "docs/governance/project-structure.md")
$structBlock = Get-TreeBlock $structText
$structEntries = if ($structBlock) { Get-TreeEntries $structBlock } else { @() }
# 「不入库」目录（.gitignore 覆盖，如 logs/）：干净 checkout 下不存在，豁免存在性检查
$ignoredDirs = @("logs")
if (-not $structEntries) {
    Check "project-structure.md tree" "unparseable (no tree block)"
} else {
    $missingEntries = @()
    foreach ($e in $structEntries) {
        $top = ($e.Path -split '/')[0]
        if ($top -in $ignoredDirs) { continue }
        # 按平台选分隔符：Linux 下 Test-Path 收到 '\' 路径恒 false 会全树误报 missing；
        # Windows 用 '\'，其余平台保留 '/'。
        $local = if ($IsWindows -or $env:OS -eq 'Windows_NT') { $e.Path -replace '/', '\' } else { $e.Path }
        if (-not (Test-Path (Join-Path $RepoRoot $local))) { $missingEntries += $e.Path }
    }
    if ($missingEntries.Count -eq 0) { Check "project-structure.md tree entries ($($structEntries.Count) entries)" "OK" }
    else { Check "project-structure.md tree entries" "missing: $($missingEntries -join ', ')" }

    # 检查 14 反向（R1-13）：docs/plans/ 下实际文件必须被目录树声明——
    # 2026-09-23-excellence-roadmap.md 曾漏登记而无门禁发现（仅 src/ 有检查 18 反向）。
    $plansDir = Join-Path $RepoRoot "docs/plans"
    if (Test-Path $plansDir) {
        $declaredPlans = @($structEntries | Where-Object { -not $_.IsDir -and $_.Path -like 'docs/plans/*' } | ForEach-Object { $_.Path })
        $actualPlans = @(Get-ChildItem -Path $plansDir -File -Filter '*.md' | ForEach-Object { "docs/plans/$($_.Name)" })
        $undeclaredPlans = @($actualPlans | Where-Object { $_ -notin $declaredPlans })
        if ($undeclaredPlans.Count -eq 0) { Check "docs/plans declared in tree ($($actualPlans.Count))" "OK" }
        else { Check "docs/plans declared in tree" "undeclared: $($undeclaredPlans -join ', ')" }
    }
}

# ---------- 15. AGENTS.md 与 project-structure.md 顶层目录一致 ----------
$agentsText = Read-Utf8 (Join-Path $RepoRoot "AGENTS.md")
$agentsBlock = Get-TreeBlock $agentsText
$agentsDirs = @()
$structDirs = @()
if ($agentsBlock) { $agentsDirs = @(Get-TreeEntries $agentsBlock | Where-Object { $_.IsDir -and $_.Path -notmatch '/' } | ForEach-Object { $_.Path }) }
if ($structBlock) { $structDirs = @(Get-TreeEntries $structBlock | Where-Object { $_.IsDir -and $_.Path -notmatch '/' } | ForEach-Object { $_.Path }) }
if (-not $agentsBlock -or -not $structBlock) {
    Check "AGENTS/project-structure top dirs" "unparseable tree"
} else {
    $missingInAgents = @($structDirs | Where-Object { $_ -notin $agentsDirs })
    $missingInStruct = @($agentsDirs | Where-Object { $_ -notin $structDirs })
    if ($missingInAgents.Count -eq 0 -and $missingInStruct.Count -eq 0) {
        Check "AGENTS/project-structure top dirs ($($structDirs.Count) dirs)" "OK"
    } else {
        $detail = @()
        if ($missingInAgents) { $detail += "AGENTS 缺: $($missingInAgents -join ',')" }
        if ($missingInStruct) { $detail += "structure 缺: $($missingInStruct -join ',')" }
        Check "AGENTS/project-structure top dirs" ($detail -join '; ')
    }
}

# ---------- 16. 散文式 UDF 计数一致性 ----------
# review-2026-08-29 P1-3/P1-4：此前只校验 api-reference↔源码（检查 1/11），从不校验
# AGENTS/CONTRIBUTING/CHANGELOG/注释中的散文 `\d+ UDF` 计数，导致 232 陈旧漂移全绿通过。
# review-2026-08-30：扫描范围从 5 个指定文件扩展为全仓 *.md（+ 源码注释文件）。
#   豁免：CHANGELOG.md（历史表述，仅验 X→Y 终值）。
#   2026-09-05：docs/cross-validation.md 已归档至 logs/reports/（审查报告唯一存放处，全仓扫描自动豁免），
#   其模块级 Total 计数检查随归档移除。
$proseMdFiles = Get-ChildItem -Path $RepoRoot -Recurse -Filter "*.md" |
    Where-Object { ($_.FullName -replace '\\', '/') -notmatch '/(\.git|bin|obj|\.qoder|TestResults|logs)/' -and ($_.FullName -replace '\\', '/') -notmatch 'BenchmarkDotNet\.Artifacts/' }
# 相对路径统一归一化为正斜杠 + 去掉前导分隔符（Windows 为 \，Linux/macOS 为 /，pwsh 双平台兼容）
$proseCsFiles = Get-ChildItem -Path (Join-Path $RepoRoot "src") -Recurse -Filter "*.cs" -ErrorAction SilentlyContinue |
    # F-08：排除正则须双平台（旧式仅匹配反斜杠，Linux CI 上 bin/obj 生成物会被扫描）。
    Where-Object { $_.FullName -notmatch '[/\\](obj|bin)[/\\]' } |
    ForEach-Object { ($_.FullName.Substring($RepoRoot.Length) -replace '\\', '/').TrimStart('/') }
# P3-6：扫描域从"*.md + 单文件 ElementWiseMapper.cs"扩展为全部 src/**/*.cs（含注释中的
# 散文计数；此前注释漂移可全绿通过）。
$proseFiles = @($proseCsFiles) +
    @($proseMdFiles | ForEach-Object { ($_.FullName.Substring($RepoRoot.Length) -replace '\\', '/').TrimStart('/') })
$proseMismatches = @()
foreach ($rel in $proseFiles) {
    $text = Read-Utf8 (Join-Path $RepoRoot $rel)
    # 读文件失败须 SKIP 计数输出（同检查 19），不得静默 continue。
    if (-not $text) { Check-Skip "Prose UDF counts" "unreadable: $rel"; continue }
    $isHistorical = ($rel -eq "CHANGELOG.md")
    # 模式 1：`N UDF`（如 "236 UDF"）与中文 `N 个 UDF`——除历史文件外强制执行 == codeUdfs。
    # 正则须覆盖中文变体：`(\d+)\s+UDF` 匹配不上「236 个 UDF」（中间隔着「个」），
    # 中文 README 计数漂移会全绿通过。变体词表：量词 个|项（`236 项 UDF`）、倒装形式
    # （`UDF 数量 236` / `UDF 总数 236` / `UDF 共 236` / `UDF: 236`）、`236 个函数（UDF）`。
    # 各变体经负向注入实测（test_verify_docs 场景 G2/G3/G4）。倒装模式仅对非历史文件
    # 生效：CHANGELOG 的「UDF 总数 X→Y」由下方模式 2 单独按区间链校验。
    if (-not $isHistorical) {
        # 模式 1a：`N UDF` / `N 个 UDF` / `N 项 UDF`
        # R2-15：全部模式加 IgnoreCase（`240 udf` 等小写变体此前 0 命中）。
        foreach ($m in [regex]::Matches($text, '(\d+)\s*(?:个|项)?\s*UDF', [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)) {
            if ([int]$m.Groups[1].Value -ne $codeUdfs) { $proseMismatches += "${rel}: '$($m.Value)'" }
        }
        # 模式 1b：倒装形式 `UDF (数量|总数|共)?[:：=]? N`
        foreach ($m in [regex]::Matches($text, 'UDF\s*(?:数量|总数|共)?\s*[:：=]?\s*(\d+)', [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)) {
            if ([int]$m.Groups[1].Value -ne $codeUdfs) { $proseMismatches += "${rel}: 倒装 '$($m.Value)'" }
        }
        # 模式 1c：`N 个函数（UDF）`（全角/半角括号均收）
        foreach ($m in [regex]::Matches($text, '(\d+)\s*个函数\s*[（(]\s*UDF\s*[）)]')) {
            if ([int]$m.Groups[1].Value -ne $codeUdfs) { $proseMismatches += "${rel}: '$($m.Value)'" }
        }
        # 模式 1d：`N 个自定义函数`（R2-15 词表补全）
        foreach ($m in [regex]::Matches($text, '(\d+)\s*个自定义函数', [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)) {
            if ([int]$m.Groups[1].Value -ne $codeUdfs) { $proseMismatches += "${rel}: '$($m.Value)'" }
        }
        # CrossVal 双通道计数自洽（R2-9）：manual-only N / cross-validated M（合计 K）
        # 必须 N+M==K 且为正数；与实测值的精确对账由 verify-manual.py 的 README
        # reconciliation 承担（CI cross-val job 执行，漂移即 FAIL，R1-09）。
        # 2026-09-23（Phase 4）：N/M 是**检查项数**而非 UDF 数——单个 UDF 可有多项检查，
        # 检查数合法超过 UDF 总数（实测 cross=263 > 240），故移除“各 ≤ UDF 总数”断言；
        # UDF 级覆盖声明（X/Y UDF）由下方分数形式单独约束（分子 ≤ 分母 == codeUdfs）。
        foreach ($m in [regex]::Matches($text, 'manual-only\s+(\d+)\s*/\s*cross-validated\s+(\d+)\s*[（(]\s*合计\s*(\d+)')) {
            $man = [int]$m.Groups[1].Value; $cross = [int]$m.Groups[2].Value; $tot = [int]$m.Groups[3].Value
            if ($man + $cross -ne $tot) { $proseMismatches += "${rel}: CrossVal 计数 '$($m.Value)' ($man+$cross != $tot)" }
            if ($man -le 0 -or $cross -le 0) { $proseMismatches += "${rel}: CrossVal 计数 '$($m.Value)' 必须为正数" }
        }
        # 分数形式 `X/Y UDF`（README "224/236 个 UDF"）：分母是总数声明必须 == codeUdfs，
        # 分子是覆盖数只要求 ≤ codeUdfs（两者都验，防分子分母任一侧漂移）。
        foreach ($m in [regex]::Matches($text, '(\d+)/(\d+)\s*(?:个)?\s*UDF', [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)) {
            $num = [int]$m.Groups[1].Value; $den = [int]$m.Groups[2].Value
            if ($den -ne $codeUdfs) { $proseMismatches += "${rel}: 分母 '$($m.Value)' ($den != $codeUdfs)" }
            if ($num -gt $codeUdfs) { $proseMismatches += "${rel}: 分子 '$($m.Value)' ($num > $codeUdfs)" }
        }
        # 分数形式 `X/Y（P%）`（README "216/240（90.0%）"，R1-09：旧正则要求 UDF 后缀，
        # 此形态不在扫描域 → 真 C# 对照宣称漂移可全绿通过）。同样验分母/分子上界；
        # 与实测值的精确对账由 verify-manual.py 的 README reconciliation 承担。
        foreach ($m in [regex]::Matches($text, '(\d+)/(\d+)\s*[（(]\s*\d+(?:\.\d+)?\s*%\s*[）)]')) {
            $num = [int]$m.Groups[1].Value; $den = [int]$m.Groups[2].Value
            if ($den -ne $codeUdfs) { $proseMismatches += "${rel}: 分母 '$($m.Value)' ($den != $codeUdfs)" }
            if ($num -gt $den) { $proseMismatches += "${rel}: 分子 '$($m.Value)' ($num > $den)" }
        }
    }
    # 模式 2：`UDF 总数 X→Y`（CHANGELOG 历史区间）——链式校验，不强制终值 == 当前计数。
    # R7 (review-2026-09-12, F8)：旧规则要求每个历史区间终点 == 当前 UDF 数；新增模块后
    # （如 SOLVE：236→240）旧的 `232→236` 必然变红，倒逼改写历史记录（本次变更曾把
    # 「UDF 总数 232→236」改写成「由 232 增至 236」以规避——门禁设计缺陷）。
    # 新规则：① 每个区间 0 < X < Y；② 终点 ≤ 当前计数（不得声称超出源码现实）；
    # ③ 相邻区间首尾相接（防中间区间改动后链断裂/漏记）。当前计数的强制一致由
    # 非历史文件的模式 1a/1b/1c/分数形式承担；发版时 CHANGELOG 必须新增递增区间
    # （由检查 10 的 tag↔CHANGELOG 与版本一致性间接保障）。
    # R7-1 (review-2026-09-13, max-level 发行前审查)：区间链必须**按起点升序排序后**校验。
    # Keep a Changelog 为新→旧排序，新版本区间在文档顶部（如 `236→240` 在 `232→236` 之前）；
    # 旧实现按文档顺序比较 `[$ri][0] == [$ri-1][1]`，对正确的新版本条目必然误报
    # （注入实测 `区间链断裂 '236→240' vs '232→236'`，K3 场景为回归守卫）。
    $ranges = @()
    foreach ($m in [regex]::Matches($text, 'UDF\s*总数\s*(\d+)\s*→\s*(\d+)')) {
        $x = [int]$m.Groups[1].Value; $y = [int]$m.Groups[2].Value
        if ($x -le 0 -or $x -ge $y) { $proseMismatches += "${rel}: '$($m.Value)'（区间须 0<X<Y）" }
        elseif ($y -gt $codeUdfs) { $proseMismatches += "${rel}: '$($m.Value)'（终点 $y > 当前 $codeUdfs）" }
        $ranges += ,@($x, $y)
    }
    $sortedRanges = @($ranges | Sort-Object { $_[0] })
    for ($ri = 1; $ri -lt $sortedRanges.Count; $ri++) {
        if ($sortedRanges[$ri][0] -ne $sortedRanges[$ri - 1][1]) {
            $proseMismatches += "${rel}: 区间链断裂 '$($sortedRanges[$ri-1][0])→$($sortedRanges[$ri-1][1])' vs '$($sortedRanges[$ri][0])→$($sortedRanges[$ri][1])'"
        }
    }
}
if ($proseMismatches.Count -eq 0) { Check "Prose UDF counts ($codeUdfs)" "OK" }
else { Check "Prose UDF counts" ($proseMismatches -join ' | ') }

# ---------- 17. [ExcelArgument] 名称 ↔ api-reference 参数列 ----------
# api-reference 参数列与源码 [ExcelArgument(Name=...)] 自动比对，
# 防文档参数名/顺序与实现漂移。
# 归一化：两端都剥离可选参数方括号——源码 [ExcelArgument(Name="[x]")] ↔ 文档 (x)。
$apiParams = @{}
foreach ($row in [regex]::Matches($apiContent, '^\|\s*`([A-Za-z0-9_.]+)`\s*\|\s*\(([^)]*)\)\s*\|', [System.Text.RegularExpressions.RegexOptions]::Multiline)) {
    $names = @()
    foreach ($p in ($row.Groups[2].Value -split ',')) {
        $p = $p.Trim().Trim('[', ']')
        if ($p -ne '') { $names += $p }
    }
    $apiParams[$row.Groups[1].Value] = $names
}
$srcParams = @{}
Get-ChildItem -Path (Join-Path $RepoRoot "src") -Recurse -Filter "*.cs" |
    Where-Object { $_.FullName -notmatch "[/\\](obj|bin)[/\\]" } | ForEach-Object {
    $text = Read-Utf8 $_.FullName
    $currentFn = $null
    foreach ($am in [regex]::Matches($text, '\[Excel(Function|Argument)\(Name\s*=\s*"([^"]+)"')) {
        if ($am.Groups[1].Value -eq 'Function') { $currentFn = $am.Groups[2].Value; $srcParams[$currentFn] = @() }
        elseif ($currentFn) { $srcParams[$currentFn] += $am.Groups[2].Value.Trim().Trim('[', ']') }
    }
}
$paramMismatches = @()
foreach ($name in $apiParams.Keys) {
    if (-not $srcParams.ContainsKey($name)) { $paramMismatches += "$name 无源码 UDF"; continue }
    if (($apiParams[$name] -join ',') -ne ($srcParams[$name] -join ',')) {
        $paramMismatches += "$name 文档=($($apiParams[$name] -join ',')) 源码=($($srcParams[$name] -join ','))"
    }
}
if ($paramMismatches.Count -eq 0) { Check "[ExcelArgument] vs api-reference params ($($apiParams.Count))" "OK" }
else { Check "[ExcelArgument] vs api-reference params" ($paramMismatches -join ' | ') }

# ---------- 18. 反向检查：src/ 实际文件必须被目录树声明 ----------
# 反向扫描 src/ 下实际文件：新增源码文件（如 NativeDllStore.cs）若忘记登记到 project-structure.md
# 目录树，前向检查（声明→存在，检查 14）无法发现。
$declaredSrcFiles = @()
foreach ($e in $structEntries) {
    if (-not $e.IsDir -and $e.Path -like 'src/*') { $declaredSrcFiles += $e.Path }
}
$srcFiles = Get-ChildItem -Path (Join-Path $RepoRoot "src") -Recurse -File |
    Where-Object { $_.FullName -notmatch "[/\\](obj|bin)[/\\]" -and $_.Extension -ne ".dna" -and $_.FullName -notmatch "BenchmarkDotNet\.Artifacts" }
# 排除 .dna 生成物：构建并发时 GenerateDna 产物可能瞬时落盘
# （.gitignore:12 已声明 src/**/*.dna 为生成物，不入库），否则检查 18 会把瞬时 .dna 当未登记
# 文件假 FAIL。
$undeclaredFiles = @()
foreach ($f in $srcFiles) {
    # 相对路径统一归一化为正斜杠 + 去前导分隔符（Windows \ / Linux /，pwsh 双平台兼容）
    $rel = ($f.FullName.Substring($RepoRoot.Length) -replace '\\', '/').TrimStart('/')
    if ($rel -notin $declaredSrcFiles) { $undeclaredFiles += $rel }
}
if ($undeclaredFiles.Count -eq 0) { Check "src files declared in tree ($($srcFiles.Count))" "OK" }
else { Check "src files declared in tree" "undeclared: $($undeclaredFiles -join ', ')" }

# ---------- 19. 文档版本头 == Directory.Build.props <Version> ----------
# specification/user-manual/api-reference 的版本头须与 Directory.Build.props <Version> 一致：
# CHANGELOG 声称"已同步"不能作数——版本头漂移时检查 5 只查 MathNet 版本、检查 10 只查
# CHANGELOG/tag，均不覆盖。
# docs/cross-validation.md 归档于 logs/reports/（审查报告唯一存放处），不参与版本头校验。
$propsVersion = [regex]::Match((Read-Utf8 (Join-Path $RepoRoot "src/Directory.Build.props")), '<Version>([^<]+)</Version>').Groups[1].Value
$verMismatches = @()
foreach ($vf in @("docs/specification/specification.md", "docs/user-manual/user-manual.md", "docs/specification/api-reference.md")) {
    $vt = Read-Utf8 (Join-Path $RepoRoot $vf)
    # 文件缺失/不可读须 SKIP 计数输出（不得静默 continue），
    # 对齐 Check-Skip"不计入 pass"语义（防止两文件全丢时检查 19 静默空转成 PASS）。
    if (-not $vt) { Check-Skip "Doc version header ($vf)" "file missing/unreadable"; continue }
    # specification「版本：v2.2.5」/ user-manual「**版本**：v2.2.5」
    # 行首锚定：无锚正则会命中正文任意位置的「版本：X.Y.Z」（如变更记录、示例），
    # 与真正的文档版本头混淆。锚定后需保证 spec/user-manual 的版本头行（行首 + Markdown
    # 前缀 > * #）仍命中（正向已实测）。
    $m = [regex]::Match($vt, '(?m)^\s*[>*#\s]*(?:版本|Version|v)\s*[:：*]*\s*(v?\d+\.\d+\.\d+)')
    if (-not $m.Success -or $m.Groups[1].Value.TrimStart('v') -ne $propsVersion) {
        $verMismatches += "${vf}: '$($m.Groups[1].Value)' (props=$propsVersion)"
    }
}
if ($verMismatches.Count -eq 0) { Check "Doc version headers == $propsVersion" "OK" }
else { Check "Doc version headers" ($verMismatches -join ' | ') }

# ---------- 20. [Fact] 计数声明一致性（md 声明 ↔ tests/**/*.cs 实测）----------
# [Fact] 计数声明须与源码实测一致：spec 声称数与 tests/**/*.cs 实测数漂移即 FAIL。
# [Fact]/[Theory] 分型统计、量词可选——仅统计 [Fact] 且量词"个"必填会静默漏过
# [Theory] 用例或英文计数写法。
$factFiles = @(Get-ChildItem -Path (Join-Path $RepoRoot "tests") -Recurse -Filter "*.cs" -ErrorAction SilentlyContinue |
    Where-Object { ($_.FullName -replace '\\', '/') -notmatch '/(bin|obj)/' })
if ($factFiles.Count -gt 0) {
    $codeFacts = 0; $codeTheories = 0
    foreach ($ff in $factFiles) {
        $ft = Read-Utf8 $ff.FullName
        if ($ft) {
            # F-08：[Fact(Skip="...")] / [FactAttribute] 变体此前不计入——声明计数应含全部
            # 事实型测试（Skip 仅在运行时跳过，不改变源码事实数）。
            $codeFacts += ([regex]::Matches($ft, '\[Fact(?:Attribute)?(?=[\]\(])')).Count
            $codeTheories += ([regex]::Matches($ft, '\[Theory(?:Attribute)?(?=[\]\(])')).Count
        }
    }
    $factMismatches = @()
    foreach ($rel in $proseFiles) {
        if ($rel -notmatch '\.md$') { continue }
        $text = Read-Utf8 (Join-Path $RepoRoot $rel)
        # F-08：不可读文件须 SKIP 计数输出，不得静默 continue（检查可能空转）。
        if (-not $text) { Check-Skip "Fact count claims" "unreadable: $rel"; continue }
        foreach ($m in [regex]::Matches($text, '([\d,]+)\s*个?\s*\[(Fact|Theory)\]')) {
            $claim = [int]($m.Groups[1].Value -replace ',', '')
            $kind = $m.Groups[2].Value
            $actual = if ($kind -eq 'Theory') { $codeTheories } else { $codeFacts }
            if ($claim -ne $actual) { $factMismatches += "${rel}: '$($m.Value)' ($claim != $actual)" }
        }
    }
    if ($factMismatches.Count -eq 0) { Check "Fact count claims ($codeFacts Fact / $codeTheories Theory)" "OK" }
    else { Check "Fact count claims" ($factMismatches -join ' | ') }
} else {
    Check-Skip "Fact count claims" "no test sources found"
}

# ---------- 汇总 ----------
Write-Host ""
Write-Host "=== Pass: $($script:pass)  Fail: $($script:fail)  Skip: $($script:skip) ==="
if ($script:fail -gt 0) { exit 1 } else { exit 0 }
