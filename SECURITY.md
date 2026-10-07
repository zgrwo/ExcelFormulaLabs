# Security Policy

## Supported Versions

| Version | Supported          |
| ------- | ------------------ |
| 2.2.x   | :white_check_mark: |
| 2.1.x   | :white_check_mark: |
| 2.0.x   | :white_check_mark: |
| 1.0.x   | :x:                |
| < 1.0.0 | :x:                |

## Reporting a Vulnerability

**Please do not report security vulnerabilities through public GitHub issues.**

Instead, report them privately via GitHub's [Security Advisories](https://github.com/zgrwo/ExcelFormulaLabs/security/advisories/new) feature, or email the maintainer directly.

### What to include

- A description of the vulnerability
- Steps to reproduce
- Affected module(s) and version(s)
- Any potential impact

### What to expect

- **Acknowledgment**: Within 48 hours
- **Status update**: Within 5 business days
- **Resolution timeline**: Depends on severity -- critical issues are prioritized for immediate patching

### Scope

This project is a C# Excel-DNA add-in library running inside Microsoft Excel. Security considerations include:

- **Excel-DNA security**: Excel-DNA loads managed assemblies into the Excel process. Third-party NuGet dependencies are pinned to specific versions.
- **UDF sandboxing**: User-defined functions execute within Excel's calculation engine. All UDF inputs are normalized through a five-layer sentinel contract (L1-L5) that converts unrepresentable values to type-zero sentinels (`double`→`NaN`, `string`→`""`, etc.) instead of throwing exceptions.
- **File system path validation**: File I/O UDFs (e.g., `FS.READ`, `FS.WRITE`) validate and canonicalize paths via `Path.GetFullPath()` with sandbox root checks. Path traversal attacks (`..`, symlinks) are blocked by the `SandboxRoot` constraint **when the sandbox is enabled** (default is unrestricted — `SandboxRoot` is null until `FileSystemCore.Initialize` is called; see [File System Sandbox (default OFF)](#file-system-sandbox-default-off)).
- **SQL parameterized queries**: Data access layers use parameterized queries exclusively. No raw string concatenation in SQL statements. SQLite operations use bound parameters (System.Data.SQLite on net48, Microsoft.Data.Sqlite on net8.0).
- **Regex timeout**: All `Regex` operations specify a `matchTimeout` of **5 seconds** to prevent ReDoS (Regular Expression Denial of Service) attacks from maliciously crafted input strings.

## File System Sandbox (default OFF)

**The `FS.*` file-system sandbox is shipped disabled, and the default build provides no path
confinement.** `FileSystemCore.SandboxRoot` is `null` until `FileSystemCore.Initialize(...)` is called;
while it is `null`, `ValidatePath` performs no sandbox check at all and `FS.*` can read, write, delete,
copy and enumerate any path the Excel process can reach. This is a deliberate product decision
(ease of use for local workbooks) — **not** a protection claim. Concretely, with the default build:

- the path-traversal (`..`) and NTFS reparse-point (junction/symlink) blocking described above is
  **inactive**;
- `FS.DRIVES` / `FS.CWD` / `FS.TEMP*` return real system values instead of sandbox-confined ones;
- the per-file read/write size caps (100 MB) are unaffected — they are not part of the sandbox root.

### How to enable it

`SandboxConfig` is immutable for the lifetime of the process (see
[ADR-0005](docs/adr/0005-sandboxconfig-immutable.md)); the sandbox is therefore enabled at **build
time only**. Add the call to `src/DataToolkit/AddIn.cs` → `AutoOpen()` (the XML doc comment above
`AutoOpen` carries the same snippet):

```csharp
FileSystemCore.Initialize(new SandboxConfig(Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
    "ExcelFormulaLabs", "sandbox")));
```

and rebuild the XLL (`dotnet build ExcelFormulaLabs.sln -c Release`). There is no runtime switch,
no environment variable and no registry setting.

### How the default is made visible

Because a silently-absent control is indistinguishable from a working one, the add-in reports the
sandbox state on every load through two **non-blocking** channels (no modal dialog is ever shown):

1. **Append-only log** — `%LOCALAPPDATA%\ExcelFormulaLabs\logs\sandbox-status.log` (one line per
   load, `yyyy-MM-dd HH:mm:ss` timestamp, rotated to `.1` at 1 MB). Written by
   `src/DataToolkit/SandboxStatus.cs`; a write failure is swallowed and never surfaces to Excel.
2. **Excel status bar** — a one-time note on load (e.g. *"FS.* 文件系统沙箱未启用（出厂默认），
   路径不受限制 — 详见 <log path>"*). It does not block, requires no acknowledgement, and
   disappears with normal Excel status-bar use.

Both channels report the same fact the documentation states here, so the documented behaviour and
the shipped behaviour cannot silently diverge. See also the [README security section](README.md#文件系统沙箱).

## Disclosure Policy

We follow coordinated disclosure. Once a fix is released, we will publish a security advisory crediting the reporter (unless anonymity is requested).