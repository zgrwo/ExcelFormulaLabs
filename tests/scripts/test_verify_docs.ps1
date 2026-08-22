# ============================================================================
# test_verify_docs.ps1 — verify-docs.ps1 回归守卫（4 场景）
# 场景 A：真实仓库副本 → 15 项检查全过（基线，防门禁自身回归）
# 场景 B：README 硬编码徽章 → 检查 9 FAIL
# 场景 C：README 断链 → 检查 12 FAIL
# 场景 D：.qoder 镜像漂移 → 检查 13 FAIL
# 用法：powershell -NoProfile -ExecutionPolicy Bypass -File tests/scripts/test_verify_docs.ps1
# 注意：本测试复制仓库（排除 bin/obj/.git 等），耗时数秒，仅在 CI windows job 与本地运行。
# ============================================================================
$ErrorActionPreference = "Stop"
$repo = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)   # 仓库根
$verifier = Join-Path $repo "scripts\verify-docs.ps1"
$tmpRoot = Join-Path $env:TEMP ("vd-test-" + [guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Path $tmpRoot -Force | Out-Null

$passCount = 0; $failCount = 0

function Copy-RepoFixture {
    # 复制仓库（排除生成目录），返回 fixture 路径
    $dst = Join-Path $tmpRoot "fixture"
    robocopy $repo $dst /E /XD bin obj .git BenchmarkDotNet.Artifacts logs better-harness __pycache__ /XF *.pyc /NFL /NDL /NJH /NJS /NP | Out-Null
    if ($LASTEXITCODE -ge 8) { throw "robocopy 失败: $LASTEXITCODE" }
    # 目录树契约要求 logs/ 存在（内容不入库），fixture 补建空目录
    New-Item -ItemType Directory -Path (Join-Path $dst "logs") -Force | Out-Null
    return $dst
}

function Run-VerifyDocs {
    param([string]$Dir, [string]$ExpectMsg, [bool]$ExpectFail)
    $output = & powershell -NoProfile -ExecutionPolicy Bypass -File $verifier -RepoRoot $Dir 2>&1
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
Write-Host "[A] 基线：仓库副本 15 项检查全过"
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
    $script:passCount++
}

# --- 场景 E：api-reference UDF 计数漂移（检查 1）---
Write-Host "[E] api-reference UDF 计数漂移应 FAIL（检查 1）"
$fixtureE = Copy-RepoFixture
$apiRef = Join-Path $fixtureE "rules\api-reference.md"
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

# --- 汇总 ---
Remove-Item -Recurse -Force $tmpRoot
Write-Host ""
Write-Host "=== Pass: $passCount  Fail: $failCount ==="
if ($failCount -gt 0) { exit 1 } else { exit 0 }