#Requires -Version 5.1
<#
.SYNOPSIS
    ExcelFormulaLabs pre-commit check script
.DESCRIPTION
    Blocks commits with common violations:
    1. Bare catch {} - red line
    2. Self-validation check(name, X, X) - false negative（全文括号平衡解析，支持跨行/嵌套调用/数组参数/短别名）
    3. IntelliSense code in net8.0 - framework isolation
    4. Core layer referencing ExcelDna - architecture violation
    5. NaN/Inf guard missing in Core files with division
    6. hasHeaders parameter missing for object[,] Core methods
.NOTES
    Usage: .\scripts\pre-commit-check.ps1 [-RepoRoot <path>]
    自测：tests/scripts/test_precommit_check.ps1（回归守卫，CI 强制执行）
#>
param(
    [string]$RepoRoot = (Split-Path -Parent $PSScriptRoot)
)

$ErrorActionPreference = "Stop"
$violations = @()
$script:skipped = 0

function Read-Utf8Text {
    param([string]$Path)
    if (-not (Test-Path $Path)) { return $null }
    return [System.IO.File]::ReadAllText($Path, [System.Text.Encoding]::UTF8)
}

# 从文本中提取 check( 调用的顶层参数列表（括号/引号平衡，支持嵌套调用与跨行参数）
# 传入全文并逐字符深度计数跨行提取（仅按单行传入会漏跨行参数）。
# 深度计数须含 [ ] { }——否则 f([1,2]) 这类数组参数内的逗号被误当参数分隔符，
# 自校验 arg2==arg3 对比会被切碎而漏检（test_precommit 场景 3 第二行覆盖此情形）。
function Split-TopLevelArgs {
    param([string]$Line, [int]$StartIndex)
    $args = New-Object System.Collections.Generic.List[string]
    $depth = 1          # 已处于 check( 括号内：深度 1 为顶层参数层
    $inStr = $null      # 当前引号字符（' 或 "）
    $buf = New-Object System.Text.StringBuilder
    $i = $StartIndex
    while ($i -lt $Line.Length) {
        $ch = $Line[$i]
        if ($inStr) {
            if ($ch -eq $inStr) {
                if ($i + 1 -lt $Line.Length -and $Line[$i+1] -eq $inStr) { [void]$buf.Append($ch); $i++ }
                else { $inStr = $null }
            } elseif ($ch -eq '\' -and $inStr -eq '"') {
                [void]$buf.Append($ch)
                if ($i + 1 -lt $Line.Length) { [void]$buf.Append($Line[$i+1]); $i++ }
            } else { [void]$buf.Append($ch) }
        } else {
            if ($ch -eq '"') { $inStr = '"'; [void]$buf.Append($ch) }
            elseif ($ch -eq "'") { $inStr = "'"; [void]$buf.Append($ch) }
            elseif ($ch -eq '(' -or $ch -eq '[' -or $ch -eq '{') { $depth++; [void]$buf.Append($ch) }
            elseif ($ch -eq ')' -or $ch -eq ']' -or $ch -eq '}') {
                $depth--
                if ($depth -eq 0) { break }   # 最外层 check( 闭合，结束
                [void]$buf.Append($ch)
            }
            elseif ($ch -eq ',' -and $depth -eq 1) { $args.Add($buf.ToString()); [void]$buf.Clear() }
            else { [void]$buf.Append($ch) }
        }
        $i++
    }
    if ($buf.Length -gt 0) { $args.Add($buf.ToString()) }
    return $args
}

# R3-18：参数归一化——两侧文本不同但语义同源的变体须判等：
#   a 与 a[:] / a[0] / a[1:2]（切片）、f(x) 与 f( x )（空白）、(x) 与 x（外括号）、
#   f(x,) 与 f(x)（尾逗号）。归一化顺序：剥外括号 → 去尾部下标 → 去全部空白 → 去尾逗号。
function Normalize-CheckArg {
    param([string]$A)
    $A = $A.Trim()
    while ($A.Length -ge 2 -and $A.StartsWith('(') -and $A.EndsWith(')')) { $A = $A.Substring(1, $A.Length - 2).Trim() }
    while ($A -match '\[[^\]]*\]$') { $A = $A.Substring(0, $A.LastIndexOf('[')).Trim() }
    $A = ($A -replace '\s+', '')
    $A = $A -replace ',\)', ')'   # f(x,) 与 f(x)
    $A = $A -replace ',$', ''     # (a,) 与 (a)
    return $A
}

# -- Check 1: Bare catch {} --
Write-Host ""
Write-Host "[1/6] Checking bare catch {} ..."

# R5-P3-39 (review 2026-09-06)：原 Select-String 行级匹配对跨行写法盲（`catch // 注释` 换行 `{`
# 为合法 C#）——改读全文用单行模式正则（\s* 跨行匹配 {），行号由全文偏移计算。
$bareCatch = Get-ChildItem -Path "$RepoRoot/src" -Recurse -Filter "*.cs" -ErrorAction SilentlyContinue |
    Where-Object { $_.FullName -notmatch "[/\\](obj|bin)[/\\]" } | ForEach-Object {
        $text = Read-Utf8Text $_.FullName
        if (-not $text) { return }
        foreach ($rx in [regex]::Matches($text, "catch(?:\s*//[^\r\n]*)*\s*{")) {
            [PSCustomObject]@{ Path = $_.FullName; LineNumber = ($text.Substring(0, $rx.Index) -split "`n").Count }
        }
    }

if ($bareCatch) {
    foreach ($m in $bareCatch) {
        $violations += "BARE_CATCH: $($m.Path):$($m.LineNumber)"
    }
    Write-Host "  [FAIL] Found $(@($bareCatch).Count) bare catch" -ForegroundColor Red
} else {
    Write-Host "  [OK] No bare catch" -ForegroundColor Green
}

# -- Check 2: Self-validation pattern (check(name, X, X)) --
Write-Host ""
Write-Host "[2/6] Checking self-validation pattern ..."

$verifyScript = Join-Path $RepoRoot "scripts\verify-manual.py"
if (Test-Path $verifyScript) {
    # R15 (review-2026-09-05)：改为全文扫描——原按单行解析，两类输入曾绕过（均实测）：
    #   ① 跨行 check(（参数分布多行，单行 IndexOf 永不命中完整参数列表）；
    #   ② 短别名 check("m", x, x)（$a2.Length -gt 3 豁免放行，"x" 长度 1）。
    # 现全文逐字符括号平衡提取参数列表；并移除长度豁免：顶层参数 ≥3 且去空白后
    # arg2==arg3 即自校验（期望值硬编码铁律下，同源对照无论长短都是假阴性）。
    $verifyText = [System.IO.File]::ReadAllText($verifyScript, [System.Text.Encoding]::UTF8)
    $selfHits = @()
    # 词边界定位（cross_check( 等尾缀不被误提取）；
    # 行首 # 注释跳过（注释里的 check(name, X, X) 示例不会假阳）。
    $lineStarts = @()  # 每行起始偏移，用于定位 idx 所在行
    $pos = 0
    foreach ($ln in ($verifyText -split "`n")) { $lineStarts += $pos; $pos += $ln.Length + 1 }
    $idx = $verifyText.IndexOf("check(")
    while ($idx -ge 0) {
        $lineIdx = 0
        while ($lineIdx + 1 -lt $lineStarts.Count -and $lineStarts[$lineIdx + 1] -le $idx) { $lineIdx++ }
        $lineText = ($verifyText -split "`n")[$lineIdx]
        if ($lineText -match '^\s*#') { $idx = $verifyText.IndexOf("check(", $idx + 1); continue }
        $before = if ($idx -gt 0) { $verifyText[$idx - 1] } else { ' ' }
        if ($before -match '[A-Za-z0-9_]') { $idx = $verifyText.IndexOf("check(", $idx + 1); continue }
        $args = Split-TopLevelArgs $verifyText ($idx + 6)
        # check(name, X, X) 及其归一化变体（R3-18）：切片/空白/外括号/尾逗号差异
        # 归一化后判等（`check(name, a, a[:])`、`check(name, f(x), f( x ))` 旧实现漏检）。
        if ($args.Count -ge 3) {
            $a2 = Normalize-CheckArg $args[1]
            $a3 = Normalize-CheckArg $args[2]
            if ($a2.Length -gt 0 -and $a2 -eq $a3) {
                $lineNo = ($verifyText.Substring(0, $idx) -split "`n").Count
                $selfHits += "verify-manual.py:$lineNo ($a2)"
            }
        }
        $idx = $verifyText.IndexOf("check(", $idx + 1)
    }
    if ($selfHits.Count -gt 0) {
        foreach ($h in $selfHits) { $violations += "SELF_CHECK: $h" }
        Write-Host "  [FAIL] Found $($selfHits.Count) self-validation" -ForegroundColor Red
    } else {
        Write-Host "  [OK] No self-validation" -ForegroundColor Green
    }
} else {
    Write-Host "  [SKIP] verify-manual.py not found" -ForegroundColor DarkYellow
}

# -- Check 3: IntelliSense in net8.0 --
Write-Host ""
Write-Host "[3/6] Checking IntelliSense isolation ..."

$allCs = Get-ChildItem -Path "$RepoRoot/src" -Recurse -Filter "*.cs" -ErrorAction SilentlyContinue |
    Where-Object { $_.FullName -notmatch "[/\\](obj|bin)[/\\]" }
$intelliHits = $allCs | Select-String -Pattern "ExcelDna\.IntelliSense"
$leaked = @()

# R5-P3-18 (review 2026-09-06)：原回看 10 行 + 不处理 #else——NET48 块超 10 行误报、
    # `#if NET48 ... #else ... IntelliSense ... #endif` 漏报。改全文件条件编译状态机：
    # 栈式跟踪 #if/#elif/#else/#endif，条件含 NET48 即视为 net48 启用区。
foreach ($hit in $intelliHits) {
    $content = @(Get-Content $hit.Path)
    $stack = New-Object System.Collections.Generic.Stack[bool]
    $inNet48 = $false
    for ($i = 0; $i -lt $content.Count; $i++) {
        $ln = $content[$i]
        if ($ln -match "^\s*#if\s+(.*)$") {
            $stack.Push(($Matches[1] -match "NET48"))
        } elseif ($ln -match "^\s*#elif\s+(.*)$" -and $stack.Count -gt 0) {
            $stack.Pop(); $stack.Push(($Matches[1] -match "NET48"))
        } elseif ($ln -match "^\s*#else" -and $stack.Count -gt 0) {
            $stack.Push(-not $stack.Pop())
        } elseif ($ln -match "^\s*#endif" -and $stack.Count -gt 0) {
            $stack.Pop()
        }
        if ($i -eq $hit.LineNumber - 1) {
            $inNet48 = ($stack.Count -gt 0) -and (-not $stack.Contains($false))
            break
        }
    }
    if (-not $inNet48) {
        $leaked += $hit
    }
}

if ($leaked.Count -gt 0) {
    foreach ($m in $leaked) {
        $violations += "INTELLISENSE_LEAK: $($m.Path):$($m.LineNumber)"
    }
    Write-Host "  [FAIL] Found $($leaked.Count) IntelliSense outside NET48" -ForegroundColor Red
} else {
    Write-Host "  [OK] IntelliSense isolation correct" -ForegroundColor Green
}

# -- Check 4: Core layer ExcelDna reference --
Write-Host ""
Write-Host "[4/6] Checking Core layer isolation ..."

# bin/obj 排除（与检查 5 口径一致）。名字通配 *Core.cs 的
# 局限（Core 逻辑放非 *Core.cs 文件会漏网）为已知边界。
$coreFiles = Get-ChildItem -Path "$RepoRoot/src" -Recurse -Filter "*Core.cs" -ErrorAction SilentlyContinue |
    Where-Object { $_.FullName -notmatch "[/\\](obj|bin)[/\\]" }
$coreHits = $coreFiles | Select-String -Pattern "ExcelDna"

if ($coreHits) {
    foreach ($m in $coreHits) {
        $violations += "CORE_EXCEL_REF: $($m.Path):$($m.LineNumber)"
    }
    Write-Host "  [FAIL] Found $($coreHits.Count) ExcelDna refs in Core" -ForegroundColor Red
} else {
    Write-Host "  [OK] Core layer has zero Excel dependency" -ForegroundColor Green
}

# -- Check 5: NaN/Inf guard in Core files --
Write-Host ""
Write-Host "[5/6] Checking NaN/Inf guards in Core files ..."

# 动态发现全部 *Core.cs，名单漂移自愈——硬编码 $coreModules 名单会漏掉新增 Core
#（如 DoeCore/DoeAnalysisCore）。
$coreFiles = Get-ChildItem -Path "$RepoRoot/src" -Recurse -Filter "*Core.cs" -ErrorAction SilentlyContinue |
    Where-Object { $_.FullName -notmatch "[/\\](obj|bin)[/\\]" }
# P2-1 (review-2026-08-31)：显式 int-除法豁免清单（替代已被移除的"任一 ArgumentException"过宽豁免）——
# 下列文件经人工核实：除法均为整数除法（常量除数，不可能产生 NaN/Inf）。
# DateTimeCore：（Month+2)/3、(Month+5)/6、Easter 的 b/100 等；DoeCore：(r / div)、cc /= 3（数组索引）。
$intDivOnlyFiles = @('DateTimeCore.cs', 'DoeCore.cs')
$nanInfMissing = @()

foreach ($f in $coreFiles) {
    $content = Read-Utf8Text $f.FullName
    # 读文件失败须 SKIP 计数输出而非静默 continue，
    # 防止文件不可读时守卫检查静默空转（对齐 verify-docs Check-Skip 语义）。
    if (-not $content) { $script:skipped++; Write-Host "  [SKIP] $($f.Name) unreadable, guard check skipped" -ForegroundColor DarkYellow; continue }
    # 剥离 // 与 /* */ 注释后再检测除法表达式：原正则会把 `/// <summary>` 等 XML 注释
    # 误判为除法，导致所有文件 hasDivision=true，守卫检查退化为「任一 ArgumentException 即豁免”。
    $code = [regex]::Replace($content, '/\*.*?\*/', '', [System.Text.RegularExpressions.RegexOptions]::Singleline)
    $code = [regex]::Replace($code, '(?m)//.*$', '')
    # 剥离字符串字面量——SqlCore 的 "DDL/DML" 等字符串里的斜杠会被误判为除法。
    $code = [regex]::Replace($code, '"[^"]*"', '""')
    # F-20 (review 2026-09-06)：补 '/=' 复合赋值——原正则 '/' 后必须跟标识符，
    # 仅用 x /= y 的 Core 文件曾可绕过 NaN/Inf 守卫检查（当前全库 0 现症，防患）。
    # R5-P3-19 (review 2026-09-06)：原 (?!0\b) 负向先行豁免 `/ 0` 与 `/ 0.5`——除以
    # 常量零/小常量恰恰必产或放大 Inf/NaN，更需守卫。当前全库无该形态（已 grep 实测），
    # 移除豁免属防患（引入 `/ 0.5` 类除法的 Core 文件现在会正确要求守卫）。
    $hasDivision = ($code -match '/\s*\w+') -or ($code -match '/=')
    # int 除法豁免（显式名单，非"任一 ArgumentException"）
    if ($intDivOnlyFiles -contains $f.Name) { continue }
    if ($hasDivision) {
        # P2-1 (review-2026-08-31)：守卫判定改用已剥离注释的 $code——原用 $content，文件头写
        # 一句 `// ArgumentException 用于参数校验` 注释即可豁免整个文件的 NaN/Inf 检查；
        # 并移除 ArgumentException 豁免：参数校验异常与"除法结果的 NaN/Inf 显式守卫"无因果关系
        # （IEEE 除零不抛异常，静默产生 Inf）。
        $hasGuard = ($code -match 'double\.IsNaN') -or
                    ($code -match 'double\.IsInfinity') -or
                    ($code -match 'double\.NaN')
        if (-not $hasGuard) {
            $nanInfMissing += $f.FullName
            $violations += "NAN_INF_GUARD: $($f.FullName)"
        }
    }
}

if ($nanInfMissing.Count -gt 0) {
    Write-Host "  [FAIL] Found $($nanInfMissing.Count) Core file(s) without NaN/Inf guard" -ForegroundColor Red
} else {
    Write-Host "  [OK] All Core files with division have NaN/Inf guards" -ForegroundColor Green
}

# -- Check 6: hasHeaders parameter for object[,] Core methods --
Write-Host ""
Write-Host "[6/6] Checking hasHeaders contract ..."

# 扫排除 bin/obj 的全部 .cs，而非 `-Filter "*Core.cs"`——只扫 *Core.cs 会漏掉
# AnalyticsHelpers.cs（含 ToDoubleMatrix(object[,])）等 Helper。Udf 层方法接收
# object 单参（非 object[,] 直接参数），不会误匹配。
$allCoreCs = Get-ChildItem -Path "$RepoRoot/src" -Recurse -Filter "*.cs" -ErrorAction SilentlyContinue |
    Where-Object { $_.FullName -notmatch "[/\\](obj|bin)[/\\]" }
$hasHeaderViolations = @()
# Structural transformation exemptions (don't interpret header semantics)
# 共 9 项：AGENTS.md §4 契约核心，与 ai-review-prompt §3.4 一致。
# DictSetCore.Keys/Values 为列提取（与 SelectColumns 同类结构变换）→ 豁免。
# AnalyticsHelpers.ToDoubleMatrix 是纯类型转换（无表头语义）→ 豁免。
$structuralExempt = @('Transpose','SelectColumns','SelectRows','CrossJoin','Flatten2D','Count','Keys','Values','ToDoubleMatrix')

foreach ($f in $allCoreCs) {
    $content = Read-Utf8Text $f.FullName
    # 不可读文件 → SKIP 计数，不静默空转（对齐检查 5 的读失败 SKIP 语义）。
    if (-not $content) { $script:skipped++; Write-Host "  [SKIP] $($f.Name) unreadable, hasHeaders check skipped" -ForegroundColor DarkYellow; continue }
    # Match method signatures with object[,] as PARAMETER (not return type)
    # R18 (review-2026-09-05)：原 `\([^)]*object...[^)]*\)` 对参数段含嵌套括号的签名漏报
    #（如元组参数 `(int,int) key, object[,] data`——首个 `)` 提前终结 [^)]*，实测漏报）。
    # 改为允许一层嵌套括号的参数段提取（分支两选择首字符不相交，无回溯风险）；
    # 泛型 Func<object[,],bool> 形态保持命中（test_precommit 场景 10/11）。
    # F-04 (review 2026-09-06，fixture 实测)：一层嵌套仍漏二层元组 `((int,(int,string)) t, object[,] d)`
    # ——嵌套扩为两层（内层同构递归一层）；并补：修饰符链（override/virtual/sealed/async/extern）、
    # protected、NRT `object?[,]`。
    $paramMatches = [regex]::Matches($content, '(private|protected internal|protected|internal|public)\s+(?:(?:static|override|virtual|sealed|async|extern|new)\s+)*(?:[\w<>.,\[\]?]+\s+)?(\w+)\s*\((?:[^()]|\((?:[^()]|\([^()]*\))*\))*object\s*\??\s*\[,+\s*\][^)]*\)')
    foreach ($pm in $paramMatches) {
        $sig = $pm.Value
        $accessMod = $pm.Groups[1].Value
        $methodName = $pm.Groups[2].Value
        # Skip private helpers (not part of hasHeaders contract)
        if ($accessMod -eq 'private') { continue }
        # Skip structural transformation exemptions
        if ($structuralExempt -contains $methodName) { continue }
        # Skip if method already has hasHeaders
        if ($sig -match 'hasHeaders') { continue }
        $hasHeaderViolations += "$($f.FullName):$methodName"
        $violations += "HAS_HEADERS: $($f.FullName):$methodName"
    }
}

if ($hasHeaderViolations.Count -gt 0) {
    Write-Host "  [FAIL] Found $($hasHeaderViolations.Count) Core file(s) with object[,] but no hasHeaders" -ForegroundColor Red
} else {
    Write-Host "  [OK] All Core files with object[,] have hasHeaders parameter" -ForegroundColor Green
}

# -- Summary --
Write-Host ""
Write-Host "============================================================"
# SKIP 计数入账展示（不可读文件跳过不静默）
if ($script:skipped -gt 0) { Write-Host "  [INFO] $($script:skipped) file(s) skipped (unreadable)" -ForegroundColor DarkYellow }
if ($violations.Count -gt 0) {
    Write-Host "  [BLOCKED] $($violations.Count) violation(s) found:" -ForegroundColor Red
    Write-Host ""
    foreach ($v in $violations) {
        Write-Host "    - $v" -ForegroundColor Red
    }
    Write-Host ""
    Write-Host "  Fix violations before committing." -ForegroundColor Red
    Write-Host "============================================================"
    exit 1
} else {
    Write-Host "  [PASS] All checks passed. Safe to commit." -ForegroundColor Green
    Write-Host "============================================================"
    exit 0
}