#!/usr/bin/env python3
"""Strip old [ExcelArgument("name")] and inject [ExcelArgument(Name="...", Description="...")]."""
import re
from pathlib import Path

ROOT = Path(__file__).parent.parent
PARAM_DESC = {
    "number1":"A range or array of numeric values",
    "array":"A range or 2D array","array1":"First range or array","array2":"Second range or array",
    "number":"A numeric value or array of numbers","text":"The text string to process",
    "text1":"First text string to compare","text2":"Second text string to compare",
    "pattern":"A regular expression pattern","ignore_case":"TRUE to ignore case, FALSE or omitted for case-sensitive",
    "match_case":"TRUE for case-sensitive match, FALSE for case-insensitive",
    "instance_num":"Which occurrence: 1=first (default), -1=last",
    "serial_number":"An Excel date serial number","start_date":"Start date as Excel serial number",
    "end_date":"End date as Excel serial number; default is today","year":"A year number, e.g. 2024",
    "month":"A month number between 1 and 12","file_path":"A file system path",
    "path1":"First path component","path2":"Second path component",
    "source_path":"Source file path to copy or move from","destination_path":"Destination file path to copy or move to",
    "source_range":"The input range or 2D array","sql_query":"A SQL query string; table name is data",
    "join_table":"A second range to join with the data table",
    "table1":"First data range for multi-table SQL","table2":"Second data range for multi-table SQL",
    "table3":"Third data range for multi-table SQL","delimiter":"The delimiter character or string",
    "num_chars":"Number of characters to generate, pad, or extract",
    "pad_text":"The padding character(s) to fill with","suffix":"Suffix to append when the text is truncated",
    "prefix":"Text to test at the start","substring":"Substring to search for and count",
    "replacement":"Replacement text for regex replace","search_pattern":"A file search pattern, e.g. *.txt or *.*",
    "file_content":"The text content to write to the file",
    "known_x":"The X variable range (independent variables)","known_y":"The Y variable range (dependent variable)",
    "weights":"Weight values for weighted least squares","lambda":"Regularization parameter; default is 1.0",
    "input_range":"Input data range with groups as columns",
    "formula_text":"A chemical formula, e.g. H2SO4 or Fe4[Fe(CN)6]3",
    "from_unit":"Source unit abbreviation, e.g. C, ATM, KG, L","to_unit":"Target unit abbreviation, e.g. F, PSI, LB, GAL",
    "celsius":"Temperature in degrees Celsius","fahrenheit":"Temperature in degrees Fahrenheit",
    "kg":"Mass in kilograms","lb":"Mass in pounds","liters":"Volume in liters",
    "gallons":"Volume in US gallons","atm":"Pressure in atmospheres",
    "psi":"Pressure in pounds per square inch","pressure":"Pressure (P) in the ideal gas law PV=nRT",
    "volume":"Volume (V) in ideal gas law or gas volume to convert",
    "moles":"Number of moles (n) in the ideal gas law PV=nRT",
    "temperature":"Temperature (T) in Kelvin for ideal gas law",
    "mass":"Mass value for density calculation","start_day":"Day the week starts: 0=Sunday, 1=Monday (default)",
    "date_unit":"Unit for date difference: d for days, m for months, y for years, w for weeks",
    "workdays":"Number of workdays to add; negative to subtract",
    "unix_timestamp":"Unix timestamp in seconds since 1970-01-01",
    "json_text":"A JSON string to parse or query","json_path":"A dot-path query, e.g. store.book[0].title",
    "xml_text":"An XML string to parse or query","xpath_text":"An XPath expression to select XML elements",
    "row_xpath":"XPath that selects each row element to extract",
    "has_headers":"TRUE if the first row contains column headers",
    "css_class":"CSS class name for the HTML table element",
    "pretty_print":"TRUE for indented, human-readable output",
    "quote_fields":"TRUE to quote all fields; FALSE for minimal quoting",
    "column_indices":"Array of 0-based column indices to select","row_indices":"Array of 0-based row indices to select",
    "row_field":"Column index for row labels in the pivot table",
    "col_field":"Column index for column labels in the pivot table",
    "value_field":"Column index for the values to aggregate",
    "aggregation":"Aggregation function: SUM, AVG, COUNT, MIN, or MAX",
    "id_fields":"Array of column indices that identify each row in unpivot",
    "value_fields":"Array of column indices to unpivot into value rows",
    "group_fields":"Array of column indices to group by","agg_column":"Column index for aggregation",
    "key_array":"Array of key values","value_array":"Array of values corresponding to each key",
    "dict_table":"A 2-column table with keys in column 0 and values in column 1",
    "lookup_value":"The value to search for in the array",
    "start_index":"Starting index, 0-based","num_elements":"Number of elements to extract",
    "sort_order":"TRUE for ascending (default), FALSE for descending",
    "sort_mode":"Sort mode: auto (default), text, or numeric",
    "criteria":"The criteria value to compare each element against",
    "comparison_operator":"Comparison operator: =, <>, >, <, >=, or <=",
    "count":"Number of elements to generate or fill",
    "start":"Starting value of the sequence","end":"Ending value of the sequence",
    "step":"Step size between consecutive values","value":"The value to fill or format",
    "value1":"Primary value; returned if not null or empty",
    "value2":"Fallback value if the primary is null or empty",
    "format_text":"A .NET format string, e.g. 0.00 or yyyy-MM-dd",
    "character_set":"Characters to randomly pick from; default is A-Z, a-z, 0-9",
    "keep_chars":"Characters to retain; all others are removed",
    "old_text":"Characters to remove from the text",
    "include_delimiters":"TRUE to include the delimiters in the result",
    "start_delimiter":"The left delimiter character(s)","end_delimiter":"The right delimiter character(s)",
    "text_array":"Array of text values to join together",
    "ignore_empty":"TRUE to skip empty cells when joining",
    "size":"The matrix dimension, e.g. 3 for a 3x3 identity matrix",
    "tolerance":"Tolerance threshold for numerical rank detection; default 1e-10",
    "k":"Percentile value between 0 and 100","x":"Hypothesized population mean for one-sample t-test",
}

api_md = ROOT / "docs" / "api-reference.md"
func_params = {}
for m in re.finditer(r"\| `([A-Z]+\.[A-Z0-9_]+)` \| \(([^)]*)\) \|", api_md.read_text(encoding="utf-8")):
    func_params[m.group(1)] = [p.strip() for p in m.group(2).split(",") if p.strip()]

total = 0
for fpath in sorted(ROOT.glob("src/**/*Udf.cs")):
    content = fpath.read_text(encoding="utf-8")
    content = re.sub(r'\[ExcelArgument\("[^"]*"\)\]\s*', '', content)
    updated = content
    for m in re.finditer(r'\[ExcelFunction\(Name\s*=\s*"([^"]+)"[^\]]*\)\]\s*public static object\s+\w+\s*\(([^)]*)\)', updated):
        func_name = m.group(1)
        raw_params = m.group(2).strip()
        if not raw_params:
            continue
        csharp_params = [p.strip() for p in raw_params.split(",") if p.strip()]
        new_names = func_params.get(func_name, [])
        if len(new_names) != len(csharp_params):
            continue
        new_parts = []
        for cp, an in zip(csharp_params, new_names):
            desc = PARAM_DESC.get(an, "The {} argument".format(an.replace("_", " ")))
            is_optional = '=null' in cp or '=null!' in cp
            display_name = '[{}]'.format(an) if is_optional else an
            new_parts.append('[ExcelArgument(Name="{}", Description="{}")] {}'.format(display_name, desc, cp))
        old_sig = "(" + raw_params + ")"
        new_sig = "(" + ", ".join(new_parts) + ")"
        updated = updated.replace(old_sig, new_sig, 1)
        total += 1
    if updated != content:
        fpath.write_text(updated, encoding="utf-8")
        print("  {}: synced".format(fpath.name))
print("Done. {} UDFs updated.".format(total))
