# verify-docs.ps1 - 文档一致性验证（唯一实现；verify-docs.sh 为包装器）
# ============================================================================
# 用法：.\scripts\verify-docs.ps1 [-RepoRoot <path>]
# 18 项检查：
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
#  14.  project-structure.md 目录树声明的条目全部真实存在
#  15.  AGENTS.md 与 project-structure.md 顶层目录集合一致（双目录树防漂移）
#  16.  散文式 UDF 计数（AGENTS/CONTRIBUTING/CHANGELOG/注释/Total 表）== 推导值
#  17.  [ExcelArgument] 名称 ↔ api-reference 参数列（自动比对，剥离可选标记）
#  18.  src/ 实际文件必须被目录树声明（反向检查：存在→声明）
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
$ErrorActionPreference = "Continue"
$script:pass = 0; $script:fail = 0

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

# SKIP 分支（如无 git 环境）：不计数失败，仅提示
function Check-Skip {
    param([string]$Label, [string]$Reason)
    Write-Host "  [SKIP] $Label ($Reason)"
    $script:pass++
}

# ---------- 1. UDF 数量 ----------
$docUdfs = (Select-String -Path (Join-Path $RepoRoot "rules\api-reference.md") -Pattern '^\| `[A-Z]+\.[A-Z]' | Measure-Object).Count
$codeUdfs = (Get-ChildItem -Path (Join-Path $RepoRoot "src") -Recurse -Filter "*.cs" |
    Where-Object { $_.FullName -notmatch "\\obj\\" -and $_.FullName -notmatch "\\bin\\" } |
    Select-String -Pattern 'ExcelFunction\(Name\s*=\s*"([^"]*)"' -AllMatches |
    ForEach-Object { $_.Matches } | ForEach-Object { $_.Groups[1].Value } |
    Sort-Object -Unique | Measure-Object).Count
if ($docUdfs -eq $codeUdfs) { Check "UDF count ($docUdfs)" "OK" }
else { Check "UDF count" "doc=$docUdfs code=$codeUdfs" }

# ---------- 2. UDF 全覆盖 ----------
$apiContent = Read-Utf8 (Join-Path $RepoRoot "rules\api-reference.md")
$missing = @()
Get-ChildItem -Path (Join-Path $RepoRoot "src") -Recurse -Filter "*.cs" |
    Where-Object { $_.FullName -notmatch "\\obj\\" -and $_.FullName -notmatch "\\bin\\" } |
    Select-String -Pattern 'ExcelFunction\(Name\s*=\s*"([^"]*)"' -AllMatches |
    ForEach-Object { $_.Matches } | ForEach-Object { $_.Groups[1].Value } |
    Sort-Object -Unique | ForEach-Object {
        if ($apiContent -notmatch [regex]::Escape($_)) { $missing += $_ }
    }
if ($missing.Count -eq 0) { Check "UDF full coverage" "OK" }
else { Check "UDF full coverage" "missing: $($missing -join ', ')" }

# ---------- 3. skill.md 含 RangeExport ----------
$skillContent = Read-Utf8 (Join-Path $RepoRoot "skills\excel-dna-project.md")
if ($skillContent -match 'RangeExport') { Check "skill.md RangeExport" "OK" }
else { Check "skill.md RangeExport" "missing" }

# ---------- 4. 架构术语 ----------
if ($skillContent -match 'MapOver') { Check "skill.md MapOver term" "OK" }
else { Check "skill.md MapOver term" "missing" }
$readmeContent = Read-Utf8 (Join-Path $RepoRoot "README.md")
if ($readmeContent -match 'ElementWiseMapper') { Check "README no internal class names" "should use MapOver not internal class" }
else { Check "README no internal impl details" "OK" }

# ---------- 5. MathNet 版本匹配 ----------
$docVer = if ((Read-Utf8 (Join-Path $RepoRoot "rules\context.md")) -match 'MathNet\.Numerics\s+([0-9.]+)') { $Matches[1] } else { "?" }
$csprojVer = if ((Read-Utf8 (Join-Path $RepoRoot "src\Analytics\Analytics.csproj")) -match 'MathNet\.Numerics.*Version="([0-9.]+)"') { $Matches[1] } else { "?" }
if ($docVer -eq $csprojVer) { Check "MathNet version ($docVer)" "OK" }
else { Check "MathNet version" "doc=$docVer csproj=$csprojVer" }

# ---------- 6. 无裸 catch ----------
$bareCatches = Get-ChildItem -Path (Join-Path $RepoRoot "src") -Recurse -Filter "*.cs" |
    Where-Object { $_.FullName -notmatch "\\obj\\" -and $_.FullName -notmatch "\\bin\\" } |
    Select-String -Pattern 'catch\s*\{'
if ($bareCatches.Count -eq 0) { Check "No bare catch" "OK" }
else { Check "No bare catch" "$($bareCatches.Count) found" }

# ---------- 7. .dna 模板完整 ----------
if (Test-Path (Join-Path $RepoRoot "src\DataToolkit\DataToolkit-AddIn-net8.dna.tpl")) { Check "net8 .dna template" "OK" }
else { Check "net8 .dna template" "missing" }
if (Test-Path (Join-Path $RepoRoot "src\DataToolkit\DataToolkit-AddIn-net48.dna.tpl")) { Check "net48 .dna template" "OK" }
else { Check "net48 .dna template" "missing" }

# ---------- 8. 无残留生成 .dna ----------
# P2 (review): generated .dna files carry TFM suffixes (*-net48.dna / *-net8.0.dna);
# the old no-suffix pattern missed stale files from interrupted builds.
$residual = Get-ChildItem -Path (Join-Path $RepoRoot "src\DataToolkit") -Filter "*.dna" -File -ErrorAction SilentlyContinue |
    Where-Object { $_.Name -notlike "*.tpl" }
if (-not $residual) { Check "No residual .dna" "OK" }
else { Check "No residual .dna" "found residual" }

# ---------- 9. README 无硬编码数量徽章 ----------
$badgeHits = @()
if ($readmeContent -match 'badge/tests-') { $badgeHits += "tests-" }
if ($readmeContent -match 'badge/UDFs-') { $badgeHits += "UDFs-" }
if ($badgeHits.Count -eq 0) { Check "README no hardcoded count badges" "OK" }
else { Check "README no hardcoded count badges" "found $($badgeHits -join ', ')（数量只能见 api-reference.md）" }

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
        if ($changelog -notmatch [regex]::Escape("## [$ver]")) { $untracked += $t }
    }
    if ($untracked.Count -eq 0) { Check "CHANGELOG covers all tags ($($semverTags.Count) tags)" "OK" }
    else { Check "CHANGELOG covers all tags" "missing entries: $($untracked -join ', ')" }

    # H3 (review-2026-08-29)：字符串排序会将 v2.9.0 排在 v2.10.0 之前，误选最新 tag。
    # 改为语义化版本（major/minor/patch）比较。
    $latestTag = $semverTags |
        Sort-Object -Property @{ Expression = {
                $v = $_ -replace '^v', ''
                $parts = $v -split '\.'
                [long]$parts[0] * 1000000 + [long]$parts[1] * 1000 + [long]$parts[2]
            } } -Descending | Select-Object -First 1
    $props = Read-Utf8 (Join-Path $RepoRoot "src\Directory.Build.props")
    $propsVer = if ($props -match '<Version>([0-9.]+)</Version>') { $Matches[1] } else { "?" }
    $latestVer = $latestTag -replace '^v', ''
    if ($propsVer -eq $latestVer) { Check "Directory.Build.props version == latest tag ($latestVer)" "OK" }
    else { Check "Directory.Build.props version" "props=$propsVer latest-tag=$latestVer" }

    # G1 (review-2026-08-29)：AssemblyVersion / FileVersion 必须与 <Version> 一致
    #（X.Y.Z → X.Y.Z.0）。v2.2.1 曾漏改 AV/FV 漂移到 2.2.0.0。
    if ($propsVer -match '^\d+\.\d+\.\d+$') {
        $propsAv = if ($props -match '<AssemblyVersion>([0-9.]+)</AssemblyVersion>') { $Matches[1] } else { "?" }
        $propsFv = if ($props -match '<FileVersion>([0-9.]+)</FileVersion>') { $Matches[1] } else { "?" }
        $expect = "$propsVer.0"
        if ($propsAv -eq $expect -and $propsFv -eq $expect) {
            Check "AssemblyVersion/FileVersion == Version" "OK"
        } else {
            Check "AssemblyVersion/FileVersion == Version" "expect=$expect av=$propsAv fv=$propsFv"
        }
    }
}

# ---------- 11. 模块 csproj 描述数量 == [ExcelFunction] 计数 ----------
foreach ($module in @("Analytics", "DataToolkit")) {
    $count = (Select-String -Path (Join-Path $RepoRoot "src\$module\*.cs") -Pattern '\[ExcelFunction' -AllMatches | Measure-Object).Count
    $csprojText = Read-Utf8 (Join-Path $RepoRoot "src\$module\$module.csproj")
    $descNum = if ($csprojText -match '(\d+)\s*个') { [int]$Matches[1] } else { -1 }
    if ($descNum -eq $count) { Check "$module csproj description count ($count)" "OK" }
    else { Check "$module csproj description count" "desc=$descNum code=$count" }
}

# ---------- 12. Markdown 相对链接断链扫描 ----------
$mdFiles = Get-ChildItem -Path $RepoRoot -Recurse -Filter "*.md" |
    Where-Object { $_.FullName -notmatch '\\\.git\\|\\bin\\|\\obj\\|\\\.qoder\\|\\TestResults\\|\\logs\\' }
$broken = @()
foreach ($f in $mdFiles) {
    $text = Read-Utf8 $f.FullName
    if (-not $text) { continue }
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
if (Test-Path (Join-Path $RepoRoot ".qoder\skills")) {
    $psExe = if ($PSVersionTable.PSEdition -eq 'Core') { 'pwsh' } else { 'powershell' }
    & $psExe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $RepoRoot "scripts\sync-qoder-skills.ps1") -CheckOnly 2>&1 | ForEach-Object { Write-Host "      $_" }
    if ($LASTEXITCODE -eq 0) { Check ".qoder skills mirror" "OK" }
    else { Check ".qoder skills mirror" "drifted (run scripts/sync-qoder-skills.ps1)" }
} else {
    Check-Skip ".qoder skills mirror" "not present (local-only tool mirror)"
}

# ---------- 14. project-structure.md 目录树条目存在性 ----------
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

$structText = Read-Utf8 (Join-Path $RepoRoot "rules\project-structure.md")
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
        $local = $e.Path -replace '/', '\'
        if (-not (Test-Path (Join-Path $RepoRoot $local))) { $missingEntries += $e.Path }
    }
    if ($missingEntries.Count -eq 0) { Check "project-structure.md tree entries ($($structEntries.Count) entries)" "OK" }
    else { Check "project-structure.md tree entries" "missing: $($missingEntries -join ', ')" }
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
#   豁免：cross-validation.md（模块级计数，仅验 Total 行）与 CHANGELOG.md（历史表述，仅验 X→Y 终值）。
$proseMdFiles = Get-ChildItem -Path $RepoRoot -Recurse -Filter "*.md" |
    Where-Object { ($_.FullName -replace '\\', '/') -notmatch '/(\.git|bin|obj|\.qoder|TestResults|logs)/' }
# 相对路径统一归一化为正斜杠 + 去掉前导分隔符（Windows 为 \，Linux/macOS 为 /，pwsh 双平台兼容）
$proseFiles = @("src/Foundation/ElementWiseMapper.cs") +
    @($proseMdFiles | ForEach-Object { ($_.FullName.Substring($RepoRoot.Length) -replace '\\', '/').TrimStart('/') })
$proseMismatches = @()
foreach ($rel in $proseFiles) {
    $text = Read-Utf8 (Join-Path $RepoRoot $rel)
    if (-not $text) { continue }
    $isModuleLevel = ($rel -eq "docs/cross-validation.md")
    $isHistorical = ($rel -eq "CHANGELOG.md")
    # 模式 1：`N UDF`（如 "236 UDF"）——除模块级/历史文件外强制执行 == codeUdfs
    if (-not $isModuleLevel -and -not $isHistorical) {
        foreach ($m in [regex]::Matches($text, '(\d+)\s+UDF')) {
            if ([int]$m.Groups[1].Value -ne $codeUdfs) { $proseMismatches += "${rel}: '$($m.Value)'" }
        }
    }
    # 模式 2：`UDF 总数 X→Y`（CHANGELOG 历史记录，断言终值）
    foreach ($m in [regex]::Matches($text, 'UDF\s*总数\s*\d+\s*→\s*(\d+)')) {
        if ([int]$m.Groups[1].Value -ne $codeUdfs) { $proseMismatches += "${rel}: '$($m.Value)'" }
    }
    # 模式 3：cross-validation.md Total 行 `| **Total** | **236** |`
    if ($isModuleLevel) {
        foreach ($m in [regex]::Matches($text, '\|\s*\*\*Total\*\*\s*\|\s*\*\*(\d+)\*\*')) {
            if ([int]$m.Groups[1].Value -ne $codeUdfs) { $proseMismatches += "${rel}: Total '$($m.Groups[1].Value)'" }
        }
    }
}
if ($proseMismatches.Count -eq 0) { Check "Prose UDF counts ($codeUdfs)" "OK" }
else { Check "Prose UDF counts" ($proseMismatches -join ' | ') }

# ---------- 17. [ExcelArgument] 名称 ↔ api-reference 参数列 ----------
# review-2026-08-29 P2：api-reference 参数列与源码 [ExcelArgument(Name=...)] 自动比对，
# 防文档参数名/顺序与实现漂移（H4 曾手工修正 STATS.SUMMARY/MODE 的参数名）。
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
    Where-Object { $_.FullName -notmatch "\\obj\\" -and $_.FullName -notmatch "\\bin\\" } | ForEach-Object {
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
# review-2026-08-29 P2：新增源码文件（如 NativeDllStore.cs）若忘记登记到 project-structure.md
# 目录树，前向检查（声明→存在，检查 14）无法发现。本检查反向扫描 src/ 下实际文件。
$declaredSrcFiles = @()
foreach ($e in $structEntries) {
    if (-not $e.IsDir -and $e.Path -like 'src/*') { $declaredSrcFiles += $e.Path }
}
$srcFiles = Get-ChildItem -Path (Join-Path $RepoRoot "src") -Recurse -File |
    Where-Object { $_.FullName -notmatch "\\obj\\" -and $_.FullName -notmatch "\\bin\\" }
$undeclaredFiles = @()
foreach ($f in $srcFiles) {
    # 相对路径统一归一化为正斜杠 + 去前导分隔符（Windows \ / Linux /，pwsh 双平台兼容）
    $rel = ($f.FullName.Substring($RepoRoot.Length) -replace '\\', '/').TrimStart('/')
    if ($rel -notin $declaredSrcFiles) { $undeclaredFiles += $rel }
}
if ($undeclaredFiles.Count -eq 0) { Check "src files declared in tree ($($srcFiles.Count))" "OK" }
else { Check "src files declared in tree" "undeclared: $($undeclaredFiles -join ', ')" }

# ---------- 汇总 ----------
Write-Host ""
Write-Host "=== Pass: $($script:pass)  Fail: $($script:fail) ==="
if ($script:fail -gt 0) { exit 1 } else { exit 0 }