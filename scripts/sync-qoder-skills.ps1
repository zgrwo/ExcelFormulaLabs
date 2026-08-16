# sync-qoder-skills.ps1 - 同步 skills/ 到 .qoder/skills/ 镜像（单一信源）
# ============================================================================
# 用法：
#   .\scripts\sync-qoder-skills.ps1              # 执行同步（写 .qoder 副本）
#   .\scripts\sync-qoder-skills.ps1 -CheckOnly   # 只校验一致性（verify-docs 调用）
#
# 背景：
#   skills/*.md 是技能唯一定义（父级相对链接，如 ../rules/context.md）。
#   .qoder/skills/<name>/SKILL.md 是 Qoder 本地工具的镜像副本（不入库），链接需适配
#   嵌套目录布局（如 ../../rules/context.md）。
#   本脚本执行「复制 + 链接重写」；本机存在 .qoder 时 verify-docs.ps1 检查 13 会
#   以 -CheckOnly 校验镜像一致性（CI 环境无 .qoder 自动跳过，不视为失败）。
# ============================================================================
param(
    [switch]$CheckOnly
)
$ErrorActionPreference = "Stop"

$utf8NoBom = New-Object System.Text.UTF8Encoding($false)
$root = Split-Path -Parent $PSScriptRoot

$skillNames = @(
    "excel-dna-project",
    "excel-dna-addins",
    "architecture-reviewer",
    "refactoring-guardian",
    "project-plan-review"
)

# 链接重写表：顶层 skills/*.md 使用父级相对链接（../rules/...），
# .qoder 副本位于 .qoder/skills/<name>/，需再加一层 ../（../../rules/...）。
# 顺序敏感：技能互链先于通用规则。
function ConvertTo-QoderLinks {
    param([string]$Text)
    $pairs = @(
        @('](excel-dna-project.md',     '](../excel-dna-project/SKILL.md'),
        @('](excel-dna-addins.md',      '](../excel-dna-addins/SKILL.md'),
        @('](architecture-reviewer.md', '](../architecture-reviewer/SKILL.md'),
        @('](refactoring-guardian.md',  '](../refactoring-guardian/SKILL.md'),
        @('](project-plan-review.md',   '](../project-plan-review/SKILL.md'),
        @('](../AGENTS.md',             '](../../AGENTS.md'),
        @('](../README.md',             '](../../README.md'),
        @('](../CHANGELOG.md',          '](../../CHANGELOG.md'),
        @('](../CONTRIBUTING.md',       '](../../CONTRIBUTING.md'),
        @('](../LICENSE',               '](../../LICENSE'),
        @('](../SECURITY.md',           '](../../SECURITY.md'),
        @('](../CODE_OF_CONDUCT.md',    '](../../CODE_OF_CONDUCT.md'),
        @('](../rules/',                '](../../rules/'),
        @('](../docs/',                 '](../../docs/'),
        @('](../scripts/',              '](../../scripts/'),
        @('](../templates/',            '](../../templates/'),
        @('](../tests/',                '](../../tests/'),
        @('](../benchmarks/',           '](../../benchmarks/'),
        @('](../build/',                '](../../build/'),
        @('](../tools/',                '](../../tools/')
    )
    foreach ($p in $pairs) {
        $Text = $Text.Replace($p[0], $p[1])
    }
    return $Text
}

$mismatches = @()
foreach ($name in $skillNames) {
    $src = Join-Path $root "skills\$name.md"
    $dst = Join-Path $root ".qoder\skills\$name\SKILL.md"
    if (-not (Test-Path $src)) {
        $mismatches += "源文件缺失: skills/$name.md"
        continue
    }
    if (-not (Test-Path $dst)) {
        $mismatches += "镜像缺失: .qoder/skills/$name/SKILL.md（请运行 sync-qoder-skills.ps1）"
        continue
    }
    $content = [System.IO.File]::ReadAllText($src, $utf8NoBom)
    $converted = ConvertTo-QoderLinks $content
    $existing = [System.IO.File]::ReadAllText($dst, $utf8NoBom)
    if ($converted -cne $existing) {
        $mismatches += "${name}: 镜像与源不一致（请运行 sync-qoder-skills.ps1 重新同步）"
    }
}

if ($CheckOnly) {
    # .qoder 为本地工具镜像（不入库）：整体缺失时跳过（CI 场景），不视为失败
    if (-not (Test-Path (Join-Path $root ".qoder\skills"))) {
        Write-Host "[SKIP] .qoder 目录不存在（本地工具镜像，不入库）" -ForegroundColor DarkYellow
        exit 0
    }
    if ($mismatches.Count -gt 0) {
        Write-Host "[FAIL] .qoder skills 镜像一致性检查失败:" -ForegroundColor Red
        foreach ($m in $mismatches) { Write-Host "    - $m" -ForegroundColor Red }
        exit 1
    }
    Write-Host "[OK] .qoder skills 镜像一致（$($skillNames.Count) 个技能）" -ForegroundColor Green
    exit 0
}

if ($mismatches.Count -gt 0) {
    # 非 CheckOnly 模式：直接重写全部镜像（修复漂移）
    foreach ($name in $skillNames) {
        $src = Join-Path $root "skills\$name.md"
        $dst = Join-Path $root ".qoder\skills\$name\SKILL.md"
        $content = [System.IO.File]::ReadAllText($src, $utf8NoBom)
        $converted = ConvertTo-QoderLinks $content
        [System.IO.File]::WriteAllText($dst, $converted, $utf8NoBom)
        Write-Host "[SYNC] $name -> .qoder/skills/$name/SKILL.md"
    }
    Write-Host "[OK] 已修复 $($mismatches.Count) 处漂移，重新同步完成" -ForegroundColor Green
} else {
    Write-Host "[OK] 镜像已是最新（$($skillNames.Count) 个技能）" -ForegroundColor Green
}
