# Security Policy

## Supported Versions

| Version | Supported          |
| ------- | ------------------ |
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
- **File system path validation**: File I/O UDFs (e.g., `FS.READ`, `FS.WRITE`) validate and canonicalize paths via `Path.GetFullPath()` with sandbox root checks. Path traversal attacks (`..`, symlinks) are blocked by the `SandboxRoot` constraint **when the sandbox is enabled** (default is unrestricted — `SandboxRoot` is null until `FileSystemCore.Initialize` is called; see README).
- **SQL parameterized queries**: Data access layers use parameterized queries exclusively. No raw string concatenation in SQL statements. SQLite operations use bound parameters (System.Data.SQLite on net48, Microsoft.Data.Sqlite on net8.0).
- **Regex timeout**: All `Regex` operations specify a `matchTimeout` of **5 seconds** to prevent ReDoS (Regular Expression Denial of Service) attacks from maliciously crafted input strings.

## Disclosure Policy

We follow coordinated disclosure. Once a fix is released, we will publish a security advisory crediting the reporter (unless anonymity is requested).