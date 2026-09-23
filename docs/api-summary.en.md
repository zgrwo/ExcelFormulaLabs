# ExcelFormulaLabs — English API Summary

> This page is a compact English index of every function name, grouped by module.
> The authoritative signatures, parameter tables and error reference live in the
> [API Reference](specification/api-reference.md) (Chinese; single source of truth for numbers).
> For the full user guide see the [User Manual](user-manual/user-manual.md).

**Use functions like `=STATS.MEAN()`, `=STR.REVERSE()`, `=JSON.PARSE()` directly in Excel.**
C# / Excel-DNA add-in, dual target framework: `net48` (no runtime install needed on
Windows 10/11) and `net8.0` (better performance). All functions are also callable from
VBA via `Application.Run`.

- Install: see [README.en.md → Installation](../README.en.md#installation) or run `scripts/install.ps1`
- Samples: open [samples/ExcelFormulaLabs-Samples.xlsx](../samples/ExcelFormulaLabs-Samples.xlsx)
- Docs site: <https://zgrwo.github.io/ExcelFormulaLabs/>

## Module index

### Analytics add-in (`Analytics-AddIn-*.xll`)

| Module | Functions |
| :--- | :--- |
| `STATS.*` | `STATS.ABS` `STATS.CORRMATRIX` `STATS.COUNT` `STATS.COVAR` `STATS.COVARP` `STATS.EXP` `STATS.GEOMEAN` `STATS.HARMEAN` `STATS.IQR` `STATS.KURT` `STATS.LN` `STATS.LOG10` `STATS.MAX` `STATS.MEAN` `STATS.MEDIAN` `STATS.MIN` `STATS.MODE` `STATS.PEARSON` `STATS.PERCENTILE` `STATS.PRODUCT` `STATS.RANGE` `STATS.SIGN` `STATS.SKEW` `STATS.SPEARMAN` `STATS.SQRT` `STATS.STDEV` `STATS.STDEVP` `STATS.SUM` `STATS.SUMMARY` `STATS.TTEST1` `STATS.TTEST2` `STATS.VAR` `STATS.VARP` `STATS.ZSCORE` |
| `LINALG.*` | `LINALG.CHOLESKY` `LINALG.CHOLESKY_ASYNC` `LINALG.COND` `LINALG.DET` `LINALG.EIGEN` `LINALG.EIGEN_ASYNC` `LINALG.IDENTITY` `LINALG.LU_L` `LINALG.LU_P` `LINALG.LU_U` `LINALG.MATMUL` `LINALG.PINV` `LINALG.PINV_ASYNC` `LINALG.QR_Q` `LINALG.QR_Q_ASYNC` `LINALG.QR_R` `LINALG.QR_R_ASYNC` `LINALG.RANK` `LINALG.SOLVE` `LINALG.SOLVE_ASYNC` `LINALG.SVD_S` `LINALG.SVD_S_ASYNC` `LINALG.SVD_U` `LINALG.SVD_U_ASYNC` `LINALG.SVD_VT` `LINALG.SVD_VT_ASYNC` `LINALG.TRACE` `LINALG.TRANSPOSE` |
| `REGRESS.*` | `REGRESS.ANOVA1` `REGRESS.COEF` `REGRESS.FACTORIMP` `REGRESS.OLS` `REGRESS.OLS_ASYNC` `REGRESS.RIDGE` `REGRESS.RIDGE_ASYNC` `REGRESS.RSQ` `REGRESS.WLS` `REGRESS.WLS_ASYNC` |
| `SOLVE.*` | `SOLVE.EQUATION` `SOLVE.INVERSE` `SOLVE.PREDICT` `SOLVE.QUALITY` |
| `PHYCHEM.*` | `PHYCHEM.ATM_TO_PSI` `PHYCHEM.C_TO_F` `PHYCHEM.DENSITY` `PHYCHEM.F_TO_C` `PHYCHEM.GAL_TO_L` `PHYCHEM.GASSTP` `PHYCHEM.IDEALGAS` `PHYCHEM.KG_TO_LB` `PHYCHEM.LB_TO_KG` `PHYCHEM.L_TO_GAL` `PHYCHEM.MASS` `PHYCHEM.MOLWT` `PHYCHEM.PRESS` `PHYCHEM.PSI_TO_ATM` `PHYCHEM.TEMP` `PHYCHEM.VOL` |
| `DOE.*` | `DOE.ANALYZE` `DOE.ANOVA` `DOE.PARETO` `DOE.PLAN` |

### DataToolkit add-in (`DataToolkit-AddIn-*.xll`)

| Module | Functions |
| :--- | :--- |
| `STR.*` | `STR.BASE64DEC` `STR.BASE64ENC` `STR.COALESCE` `STR.COMMONPFX` `STR.COUNTSUB` `STR.ENDSWITH` `STR.EXTRACT` `STR.FORMAT` `STR.HTMLDECODE` `STR.HTMLENCODE` `STR.ISNULLEMPTY` `STR.ISNULLWS` `STR.KEEP` `STR.LEFTOF` `STR.LEVENSHTEIN` `STR.NORMWS` `STR.NTHWORD` `STR.PADLEFT` `STR.PADRIGHT` `STR.REMOVE` `STR.REVERSE` `STR.RIGHTOF` `STR.RNDALPHA` `STR.RNDNUM` `STR.RNDSTR` `STR.SOUNDEX` `STR.STARTSWITH` `STR.STRIPHTML` `STR.TEXTJOIN` `STR.TITLE` `STR.TRUNCATE` `STR.URLDECODE` `STR.URLENCODE` `STR.UUID` |
| `DT.*` | `DT.ADDWKD` `DT.AGEDAYS` `DT.AGEMONTHS` `DT.AGEYEARS` `DT.DATEDIFF` `DT.DIM` `DT.DOY` `DT.EASTER` `DT.EOM` `DT.EOW` `DT.FROMUNIX` `DT.ISLEAP` `DT.ISOWEEK` `DT.ISWE` `DT.NEXTWKD` `DT.QUARTER` `DT.SEMESTER` `DT.SOM` `DT.SOW` `DT.UNIXTS` `DT.WEEKDAY` `DT.WEEKDAYISO` `DT.WEEKDAYNAME` `DT.WKDBTWN` `DT.WOM` |
| `REGEX.*` | `REGEX.COUNT` `REGEX.ESCAPE` `REGEX.GROUPS` `REGEX.ISMATCH` `REGEX.MATCH` `REGEX.MATCHALL` `REGEX.REPLACE` `REGEX.SPLIT` `REGEX.TEST` |
| `ARR.*` | `ARR.CONCAT` `ARR.CONTAINS` `ARR.COUNT` `ARR.FILL` `ARR.FILTER` `ARR.FILTER_EQ` `ARR.FILTER_GT` `ARR.FILTER_LT` `ARR.FILTER_NE` `ARR.FLATTEN` `ARR.INDEXOF` `ARR.RANGE` `ARR.REVERSE` `ARR.SHUFFLE` `ARR.SLICE` `ARR.SORT` `ARR.SORTASC` `ARR.SORTDESC` `ARR.SORTNUM` `ARR.SORTTEXT` `ARR.TOSET` `ARR.UNIQUE` |
| `DICT.*` | `DICT.COUNT` `DICT.DICT` `DICT.EXCEPT` `DICT.FREQUENCY` `DICT.INTERSECT` `DICT.KEYS` `DICT.UNION` `DICT.VALUES` |
| `JSON.*` / `XML.*` | `JSON.PARSE` `JSON.PRETTIFY` `JSON.QUERY` `JSON.TOTABLE` `JSON.VALIDATE` / `XML.TOTABLE` `XML.VALIDATE` `XML.XPATH` |
| `PIVOT.*` | `PIVOT.CROSSJOIN` `PIVOT.GROUPBY` `PIVOT.PIVOT` `PIVOT.UNPIVOT` |
| `SQL.*` | `SQL.JOIN` `SQL.QUERY` `SQL.QUERY3` |
| `FS.*` | `FS.APPEND` `FS.BNAME` `FS.COMBINE` `FS.COPY` `FS.DELDIR` `FS.DELETE` `FS.DRIVES` `FS.EXT` `FS.FDEXISTS` `FS.FEXISTS` `FS.FNAME` `FS.FOLDER` `FS.FSIZE` `FS.LS` `FS.LSDIR` `FS.MKDIR` `FS.MOVE` `FS.NORM` `FS.PWD` `FS.READ` `FS.TEMP` `FS.WRITE` |
| `RANGE.*` | `RANGE.SELCOLS` `RANGE.SELROWS` `RANGE.TOCSV` `RANGE.TOCSVSEMI` `RANGE.TOCSVTAB` `RANGE.TOHTML` `RANGE.TOJSON` `RANGE.TOMD` `RANGE.TRANSPOSE` |

> `*_ASYNC` variants run the computation on a background thread; the net48 build
> provides IntelliSense hints, the net8.0 build does not (known Excel-DNA issue).

## Usage patterns

```text
=STATS.MEAN(A1:A100)                    ' scalar result
=STATS.ABS(A1:A10)                      ' element-wise array
=LINALG.MATMUL(A1:C3, E1:G3)            ' matrix product
=STR.STARTSWITH(A1:A10, B1)             ' scalar broadcast to array
=JSON.QUERY(A1, "results[0].name")      ' JSON path query
=SQL.QUERY(A1:D500, "SELECT Dept, AVG(Salary) FROM data GROUP BY Dept")
=SOLVE.INVERSE(A1:C11)                  ' process-parameter inversion
```

- **Array formulas**: Excel 365 spills automatically; older versions need `Ctrl+Shift+Enter`.
- **Broadcasting**: scalars broadcast to the array size; equal-length arrays pair element-wise;
  mismatched sizes return `#VALUE!`.
- **Blank cells**: element-wise functions pass blanks through unchanged; statistical functions
  propagate sentinel NaN (`#NUM!`) instead of skipping blanks.

## Error handling

| Return | Meaning |
| :--- | :--- |
| `#VALUE!` | Input/execution error (bad argument, unknown operator, regex timeout, unsafe path…) |
| `#NUM!` | Computed result is undefined for the given data (e.g. NaN/Inf propagation) |
| `#N/A` / `#DIV/0!` … | Excel error inputs pass through the MapOver layer unchanged |

## Security

- `FS.*` sandbox is **off by default** (`SandboxRoot = null`); enable it with
  `FileSystemCore.Initialize(new SandboxConfig(...))` before distributing to untrusted users.
- SQL `INSERT`s are parameterized and column names sanitized; user-supplied SQL statements
  themselves cannot be parameterized — only run them on trusted input.
- All `REGEX.*` functions carry a 5-second timeout plus a per-call budget to bound ReDoS.

## Verification

- Full xUnit suites run on both TFMs (net48 + net8.0).
- Numeric modules are cross-validated against independent Python implementations
  (numpy/scipy/sklearn) at 1e-10 precision via the `CrossVal` runner and `verify-manual.py`.
- Manual examples are recomputed by Python; self-checked and cross-validated examples are
  reported separately (no self-validation).

> Full documentation: [API Reference](specification/api-reference.md) ·
> [User Manual](user-manual/user-manual.md) ·
> [Glossary](governance/context.md) ·
> [Roadmap](../ROADMAP.md)
