# Excel Formula Enhancement Library

> This is the English entry. The authoritative documentation is in Chinese (see README.md).

[![CI](https://github.com/zgrwo/ExcelFormulaLabs/actions/workflows/ci.yml/badge.svg)](https://github.com/zgrwo/ExcelFormulaLabs/actions/workflows/ci.yml)
[![Release](https://github.com/zgrwo/ExcelFormulaLabs/actions/workflows/release.yml/badge.svg)](https://github.com/zgrwo/ExcelFormulaLabs/actions/workflows/release.yml)
[![GitHub release](https://img.shields.io/github/v/release/zgrwo/ExcelFormulaLabs)](https://github.com/zgrwo/ExcelFormulaLabs/releases/latest)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

**Use functions like `=STATS.MEAN()`, `=STR.REVERSE()`, `=JSON.PARSE()` directly in Excel.** Built on a high-performance C# implementation with Python-level precision. The net48 build ships with IntelliSense parameter hints (the net8.0 build does not, due to a known Excel-DNA issue — see [Known Limitations](#known-limitations)), and all functions can be called directly from VBA via `Application.Run`. See the [API Reference](rules/api-reference.md) for the complete function list and count (the single source of truth for numbers; test status is shown in the CI badges above).

---

## Installation

### Option 1: No Runtime Installation Required (Recommended)

Windows 10/11 ship with .NET Framework 4.8, so you can load the net48 `.xll` directly:

> ⚠️ **Unblock the `.xll` after downloading from GitHub**: Windows marks files downloaded from the internet as "from another computer". If Excel reports "This file came from another computer and might be blocked to help protect this computer", right-click the `.xll` → Properties → check "Unblock" under Security → OK, then follow the steps below.

1. Excel → File → Options → Add-ins → Manage: Excel Add-ins → Go → Browse
2. Select the `.xll` file and click OK
3. Click "Enable" when the security prompt appears

| File | Modules included |
|------|---------|
| `Analytics-AddIn-net48-packed.xll` | STATS · LINALG · REGRESS · PHYCHEM · DOE (requires .NET Framework 4.8, 32-bit) |
| `Analytics-AddIn-net48-64-packed.xll` | STATS · LINALG · REGRESS · PHYCHEM · DOE (requires .NET Framework 4.8, 64-bit) |
| `Analytics-AddIn-net8.0-packed.xll` | STATS · LINALG · REGRESS · PHYCHEM · DOE (requires .NET 8 runtime, 32-bit) |
| `Analytics-AddIn-net8.0-64-packed.xll` | STATS · LINALG · REGRESS · PHYCHEM · DOE (requires .NET 8 runtime, 64-bit) |
| `DataToolkit-AddIn-net48-packed.xll` | STR · DT · REGEX · ARR · DICT · JSON/XML · PIVOT · SQL · FS · RANGE (requires .NET Framework 4.8, 32-bit) |
| `DataToolkit-AddIn-net48-64-packed.xll` | STR · DT · REGEX · ARR · DICT · JSON/XML · PIVOT · SQL · FS · RANGE (requires .NET Framework 4.8, 64-bit) |
| `DataToolkit-AddIn-net8.0-packed.xll` | STR · DT · REGEX · ARR · DICT · JSON/XML · PIVOT · SQL · FS · RANGE (requires .NET 8 runtime, 32-bit) |
| `DataToolkit-AddIn-net8.0-64-packed.xll` | STR · DT · REGEX · ARR · DICT · JSON/XML · PIVOT · SQL · FS · RANGE (requires .NET 8 runtime, 64-bit) |

> **Version selection**: for 64-bit Excel choose the `.xll` whose filename contains `64`; for 32-bit Excel choose the one without. The `-net48` versions require no additional runtime (built into Windows 10/11), while the `-net8.0` versions perform better but require the .NET 8 runtime. Both add-ins can be loaded at the same time, or you can install only one as needed.

### Option 2: Install the .NET 8 Runtime (Better Performance)

1. Download [.NET Desktop Runtime 8.0](https://dotnet.microsoft.com/download/dotnet/8.0) (~50 MB) and install it by double-clicking
2. Verify: run `dotnet --list-runtimes` in a command line; `Microsoft.NETCore.App 8.0.x` should appear
3. Load the net8.0 `.xll` (located under `net8.0-windows/publish/`)

### Verifying the Installation

Type `=STATS.MEAN(` in any cell; if Excel pops up the function auto-completion, the installation succeeded.

---

## Module Overview

> See the **[API Reference](rules/api-reference.md)** for complete signatures and parameter descriptions; see the **[User Manual](rules/user-manual.md)** for detailed examples of every function.

| Module | What it does | Try it |
|------|------|-------|
| `STATS.*` | Mean/variance/quantiles/t-tests/correlation/correlation matrix… benchmarked against scipy | `=STATS.SUMMARY(A1:A100)` |
| `STR.*` | Reverse/extract/encode-decode/edit distance/format… | `=STR.TEXTJOIN(",", TRUE, A1:A10)` |
| `REGEX.*` | Regex match/replace/capture groups (not natively available in Excel) | `=REGEX.MATCH(A1, "\d+")` |
| `DT.*` | ISO week/weekday/age/Easter/timestamps… | `=DT.AGEYEARS(B2, TODAY())` |
| `ARR.*` | Sort/filter/deduplicate/slice/shuffle… | `=ARR.UNIQUE(A1:A100)` |
| `JSON.*` / `XML.*` | Parse JSON, XPath queries | `=JSON.QUERY(A1, "0.Name")` |
| `DICT.*` | Frequency counts/intersection/union/key-value lookup | `=DICT.FREQUENCY(A1:A100)` |
| `LINALG.*` | Determinant/inverse/eigenvalues/SVD/QR/LU… | `=LINALG.SOLVE(A1:C3, D1:D3)` |
| `REGRESS.*` | OLS/WLS/ridge regression/ANOVA/feature importance | `=REGRESS.OLS(A1:A100, B1:C100)` |
| `PHYCHEM.*` | Molecular weight/temperature/pressure/volume/mass conversion | `=PHYCHEM.C_TO_F(100)` |
| `DOE.*` | Design-of-experiments matrix (full factorial, Minitab/JMP-aligned) | `=DOE.PLAN(2,2,0,2,"full",FALSE)` |
| `SQL.*` | Write SQL queries against Excel ranges | `=SQL.QUERY(A1:D100, "SELECT Col1, AVG(Col3) FROM data GROUP BY Col1")` |
| `PIVOT.*` | Pivot/unpivot/grouped aggregation/cross join | `=PIVOT.GROUPBY(A1:C100, {1}, 3, "avg")` |
| `RANGE.*` | Export HTML/JSON/Markdown/CSV | `=RANGE.TOMD(A1:D10, TRUE)` |
| `FS.*` | Read/write files/list directories/copy/delete | `=FS.READ("C:\data.txt")` |

---

## Calling from VBA

After loading the `.xll`, all functions can be called directly via `Application.Run` without references or declarations. See [API Reference → VBA](rules/api-reference.md#vba-调用).

---

## Usage Patterns

### Array Formulas

All functions support array inputs. Excel 365 spills automatically; older versions use `Ctrl+Shift+Enter`.

```
=STATS.MEAN(A1:A100)            ' Scalar result
=STATS.ABS(A1:A10)              ' Element-wise, returns an array
=LINALG.MATMUL(A1:C3, E1:G3)    ' Matrix multiplication, returns a 2-D array
```

### Multi-Argument Broadcasting

Multi-argument functions broadcast automatically. Scalar arguments are broadcast to the array size; arrays of equal length are paired element-wise. Mismatched sizes return `#VALUE!`.

```
=STR.STARTSWITH(A1:A10, B1)          ' Scalar B1 is broadcast to the whole array
=STATS.COVAR(A1:A10, B1:B10)          ' Arrays of equal length are paired element-wise
```

### Typical Scenarios

```
=STATS.SUMMARY(A1:A100)              ' One-call output of count/mean/stdev/min/Q1/median/Q3/max/IQR
=DT.AGEYEARS(DATE(1990,5,15), TODAY())  ' Calculate age
=REGEX.MATCHALL(A1, "\d+")           ' Extract all numbers
=JSON.QUERY(A1, "results[0].name")   ' Fetch a field from JSON
=SQL.QUERY(A1:D500, "SELECT Dept, AVG(Salary) FROM data GROUP BY Dept")
=FS.READ("C:\Users\Public\Documents\data.txt")
```

---

## Error Handling

Functions return two kinds of error values: **`#VALUE!`** (input/execution errors, fixable by the user) and **`#NUM!`** (the computed result is undefined — the data itself does not satisfy the mathematical conditions).

- Excel error values (`#N/A`, `#DIV/0!`, etc.) are passed through at the MapOver layer and skipped in statistical functions
- Blank cells follow sentinel NaN propagation in statistical functions — a range containing blanks yields `#NUM!` (unlike Excel's native `AVERAGE`/`SUM` which skip blanks; see Known Limitations). In MapOver layer functions blanks pass through unchanged
- Non-numeric cells return sentinel values (`0`/`false`/`""`) after type conversion and are not treated as errors
- When all inputs are filtered out, `#VALUE!` or `NaN` is returned

> See the **[API Reference → Error Reference](rules/api-reference.md#错误参考)** for the complete error list (the single source of truth).

---

## Security

### Filesystem Sandbox

> ⚠️ **Important**: `FS.*` functions have **no path restrictions** by default (`SandboxRoot` is `null`) and can access any filesystem path.
> If you distribute to untrusted users, be sure to enable the sandbox in `AutoOpen()` in `AddIn.cs`:

```csharp
FileSystemCore.Initialize(new SandboxConfig(@"C:\Users\Public\Documents"));
```

The configuration is an immutable record set once at startup, eliminating runtime races. Out-of-bounds access returns `#VALUE!`. The sandbox checks NTFS reparse points (junctions/symlinks) segment by segment.

### SQL Injection Protection

Data INSERTs use parameterized queries, and column names are sanitized to alphanumerics. User-provided SQL statements themselves cannot be parameterized — use them only on trusted input.

### Regex Timeout

All `REGEX.*` functions have a built-in 5-second timeout to prevent ReDoS attacks from hanging Excel.

---

## Quality Assurance

- **Full test suites on both .NET versions**, covering happy paths and degenerate inputs (zeros/empty/single-element/all-equal)
- **Python cross-validation**: Stats/Regression checked item by item against numpy/scipy to a precision of 1e-10; DataToolkit integration pipeline tests cover cross-module combinations
- **Manual verification**: Python cross-validation covers 224/236 UDF examples (sync variants; the remaining 12 *_ASYNC / shared-Core variants without standalone examples are covered by UDF-layer tests) to ensure the results match the source

---

## Known Limitations

### IntelliSense (Parameter Hints) Is net48-Only

- **net48 add-in**: after loading, typing a function name in the formula bar shows a floating tooltip with parameter names.
- **net8.0 add-in**: no parameter hints. This is a known Excel-DNA bug ([Issue #343](https://github.com/Excel-DNA/ExcelDna/issues/343)) — an internal null reference in `ExcelSynchronizationContext.Post` under .NET 8. UDF computation and the function list are completely unaffected.

> **Workaround**: select a cell containing the function name and press `Ctrl+Shift+A` to insert parameter name placeholders; or use Excel's `fx` button to view the function arguments dialog.

### Uninstalling Both Add-ins Together

When both add-ins (Analytics + DataToolkit) are loaded, it is recommended to uninstall them one at a time (uncheck one, click OK, then uncheck the other).

### Intermittent SyncMacro Error (Excel-DNA upstream issue)

In rare cases Excel reports `Unexpected error trying to run SyncMacro for queued macro execution` (AccessViolationException / TargetInvocationException) — a known Excel-DNA framework issue ([Issue #390](https://github.com/Excel-DNA/ExcelDna/issues/390), open), correlated with Excel language (non-English more likely), Office Click-to-Run version and calculation timing. It is **not related to this add-in's function logic**; a local 120-second stress test (including continuous `*_ASYNC` recalculation) did not reproduce it.

If it occurs frequently: ① unload and reload the add-in; ② avoid heavy repeated use of `*_ASYNC` functions in complex workbooks; ③ temporarily disable net48 IntelliSense (comment out `IntelliSenseServer.Install()` in `AddIn.AutoOpen` and repack). This add-in only uses Excel-DNA's official async mechanisms (net48 IntelliSense install + async UDF result marshalling) and queues no business macros.

### Statistical Functions Do Not Skip Blank Cells

Statistical functions (`STATS.*`/`REGRESS.*` etc.) treat blank cells as sentinel NaN propagation (a range containing blanks yields `#NUM!`), unlike Excel's native `AVERAGE`/`SUM` which skip blanks. To skip them, clean the data first with `FILTER`/`IF` or `ARR.FILTER`.

---

## Uninstallation

1. Excel → File → Options → Add-ins → Excel Add-ins → Go
2. Uncheck the add-in and click OK
3. Complete removal: delete the `.xll` files; to uninstall the .NET 8 Runtime, do so under Windows Settings → Apps

---

## Architecture Highlights

```
UDF layer (public static, [ExcelFunction])  ← entry: dispatch & adaptation only
  ↓ MapOver / MapOverMulti / V() dispatch
Core layer (internal static, pure logic)     ← zero Excel dependency
  ↓ depends on
Foundation (shared utilities)                ← InputNormalizer, MapOver, OutputWrapper
```

- ✅ UDFs contain no business logic; Core never references `ExcelDna.Integration`
- ❌ Direct cross-layer calls or reverse dependencies are forbidden
- **Dual TFM**: net48 (no installation needed, available out of the box on Windows 10/11) + net8.0 (better performance)
- **Sentinel contract L1-L5**: non-convertible values return zero-value sentinels instead of throwing
- **MapOver abstraction**: eliminates ~3000 lines of duplicated boilerplate code

---

## Building from Source

```bash
dotnet restore
dotnet build -c Release
dotnet test
```

Artifacts: `src/*/bin/Release/{net8.0-windows|net48}/publish/`

---

## Documentation Index

| Document | Role | Content |
|------|------|------|
| [README.en.md](README.en.md) | English entry | This page — entry point for international users |
| [API Reference](rules/api-reference.md) | Single source of truth for numbers | Complete function signatures, parameter descriptions, error tables |
| [User Manual](rules/user-manual.md) | Learning tutorial | Detailed examples for every function + result interpretation guide |
| [context.md](rules/context.md) | Glossary | Single definition of every term |
| [AGENTS.md](AGENTS.md) | Project constitution | Architecture layering, red-line rules, development workflow |
| [skill: excel-dna-project](skills/excel-dna-project.md) | Coding standards | MapOver selection, defensive rules, testing patterns |
| [skill: excel-dna-addins](skills/excel-dna-addins.md) | Packaging & distribution | UDF declarations, golden rules, .xll packaging |

---

## Governance System

This project follows the [Harmonization Governance Specification](https://github.com/zgrwo/Harmonization) template system:

| File | Audience | Responsibility |
|------|------|------|
| `AGENTS.md` | AI coding assistants | Project constitution — architecture, red lines, coding guidelines, anti-hallucination rules |
| `readme.md` | Human users | Feature guide — installation, module overview, usage patterns (this file) |
| `rules/` | AI + humans | Specification documents — API reference, user manual, glossary, governance rules |
| `skills/` | AI coding | Skill definitions — language pitfalls, coding patterns, refactoring guidelines |

**Core principles**: SSOT (each piece of information is defined in exactly one place), Skill-first (load the relevant skill before modifying code), and the four core guidelines.