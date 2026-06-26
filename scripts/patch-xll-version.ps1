param(
    [Parameter(Mandatory=$true)] [string]$XllPath,
    [Parameter(Mandatory=$true)] [string]$FileDescription,
    [Parameter(Mandatory=$true)] [string]$ProductName
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path $XllPath)) {
    Write-Host "Skipping VERSIONINFO patch: $XllPath not found"
    exit 0
}

$bytes = [System.IO.File]::ReadAllBytes((Resolve-Path $XllPath))

function Replace-UnicodeString {
    param([byte[]]$data, [string]$old, [string]$new)

    $oldBytes = [System.Text.Encoding]::Unicode.GetBytes($old)
    $newBytes = [System.Text.Encoding]::Unicode.GetBytes($new)

    if ($newBytes.Length -gt $oldBytes.Length) {
        Write-Warning "New string '$new' longer than old ($($newBytes.Length) > $($oldBytes.Length) bytes), truncating"
        $newBytes = $newBytes[0..($oldBytes.Length - 1)]
    }

    $padded = New-Object byte[] $oldBytes.Length
    [Array]::Copy($newBytes, 0, $padded, 0, [Math]::Min($newBytes.Length, $oldBytes.Length))

    $found = 0
    for ($i = 0; $i -le $data.Length - $oldBytes.Length; $i++) {
        $match = $true
        for ($j = 0; $j -lt $oldBytes.Length; $j++) {
            if ($data[$i + $j] -ne $oldBytes[$j]) { $match = $false; break }
        }
        if ($match) {
            [Array]::Copy($padded, 0, $data, $i, $padded.Length)
            $found++
            $i += $oldBytes.Length - 1
        }
    }

    if ($found -gt 0) {
        Write-Host "  '$old' -> '$new' ($found occurrences)"
    }
    return $found
}

Write-Host "Patching VERSIONINFO: $XllPath"

$n1 = Replace-UnicodeString -data $bytes -old "Excel-DNA Dynamic Link Library" -new $FileDescription
$n2 = Replace-UnicodeString -data $bytes -old "Excel-DNA Add-In Framework for Microsoft Excel" -new $ProductName

if ($n1 -gt 0 -or $n2 -gt 0) {
    [System.IO.File]::WriteAllBytes((Resolve-Path $XllPath), $bytes)
    Write-Host "VERSIONINFO patched ($n1 + $n2 strings). Build again to see new descriptions."
} else {
    Write-Host "Nothing patched (target strings not found; already patched?)"
}
