# scaffold-udf.ps1 - UDF scaffold generator
# Usage: .\scripts\scaffold-udf.ps1 -Module Analytics -Name Weather -Prefix WEATHER
# Generates 4 files from templates/NewModule/ into the correct project directories.

param(
    [Parameter(Mandatory=$true)]
    [string]$Module,       # Target module folder under src/ (e.g. Analytics, DataToolkit)

    [Parameter(Mandatory=$true)]
    [string]$Name,         # PascalCase class name (e.g. Weather, Finance)

    [Parameter(Mandatory=$true)]
    [string]$Prefix        # UDF prefix in Excel (e.g. WEATHER, FIN)
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot  # project root

# Validate module exists
$modulePath = Join-Path $root "src" $Module
if (-not (Test-Path $modulePath)) {
    Write-Host "[FAIL] Module directory not found: $modulePath"
    Write-Host "       Available modules:"
    Get-ChildItem (Join-Path $root "src") -Directory | ForEach-Object { Write-Host "         - $($_.Name)" }
    exit 1
}

# Template directory
$tplDir = Join-Path $root "templates" "NewModule"
if (-not (Test-Path $tplDir)) {
    Write-Host "[FAIL] Template directory not found: $tplDir"
    exit 1
}

# Replacement map
$replacements = @{
    '{Name}'   = $Name
    '{Module}' = $Module
    '{PREFIX}' = $Prefix.ToUpper()
}

function Expand-Template {
    param([string]$TemplateFile, [string]$OutputFile)

    if (Test-Path $OutputFile) {
        Write-Host "[SKIP] Already exists: $OutputFile"
        return
    }

    $content = Get-Content $TemplateFile -Raw -Encoding UTF8
    foreach ($key in $replacements.Keys) {
        $content = $content.Replace($key, $replacements[$key])
    }

    $outDir = Split-Path -Parent $OutputFile
    if (-not (Test-Path $outDir)) {
        New-Item -ItemType Directory -Path $outDir -Force | Out-Null
    }

    [System.IO.File]::WriteAllText($OutputFile, $content, [System.Text.UTF8Encoding]::new($false))
    Write-Host "[OK]   $OutputFile"
}

# Generate 4 files
Write-Host ""
Write-Host "=== UDF Scaffold: $Name (Module=$Module, Prefix=$Prefix) ==="
Write-Host ""

Expand-Template (Join-Path $tplDir '{Name}Core.cs.template') `
                (Join-Path $modulePath "$Name`Core.cs")

Expand-Template (Join-Path $tplDir '{Name}Udf.cs.template') `
                (Join-Path $modulePath "$Name`Udf.cs")

$testProject = Join-Path $root "tests" "$Module.Tests"
Expand-Template (Join-Path $tplDir '{Name}Core.Tests.cs.template') `
                (Join-Path $testProject "$Name`CoreTests.cs")

# CrossVal template -> output as reference file in scripts/
Expand-Template (Join-Path $tplDir '{Name}CrossVal.py.template') `
                (Join-Path $root "scripts" "$Name`CrossVal.py")

Write-Host ""
Write-Host "=== Done. Next steps: ==="
Write-Host "  1. Implement core logic in src/$Module/$Name`Core.cs"
Write-Host "  2. Adjust UDF signatures in src/$Module/$Name`Udf.cs"
Write-Host "  3. Add tests in tests/$Module.Tests/$Name`CoreTests.cs"
Write-Host "  4. Merge CrossVal entries into scripts/verify-manual.py"
Write-Host "  5. Run: dotnet build; dotnet test --filter $Name"
Write-Host ""
