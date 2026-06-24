#!/usr/bin/env python3
"""Add [ExcelArgument] attributes to all UDF parameters based on api-reference.md."""
import re
from pathlib import Path

ROOT = Path(__file__).parent.parent

def parse_api_params():
    """Parse api-reference.md to map ExcelFunction name -> list of param names."""
    api_md = ROOT / "docs" / "api-reference.md"
    text = api_md.read_text(encoding="utf-8")
    pattern = re.compile(r'\| `([A-Z]+\.[A-Z0-9_]+)` \| \(([^)]*)\) \|')
    func_params = {}
    for m in pattern.finditer(text):
        name = m.group(1)
        params_str = m.group(2).strip()
        params = [p.strip() for p in params_str.split(",") if p.strip()]
        func_params[name] = params
    return func_params

FUNC_PARAMS = parse_api_params()

def patch_source(text, func_name, csharp_params):
    """Insert [ExcelArgument] into a single UDF method body in `text`.
    text: the full source of the Udf.cs file
    func_name: "STATS.MEAN" etc.
    csharp_params: list of raw param declarations e.g. ["object data"]
    Returns (new_text, changed_bool)
    """
    arg_names = FUNC_PARAMS.get(func_name, [])
    if not arg_names or len(arg_names) != len(csharp_params):
        return text, False

    # Build old and new param strings
    old_joined = ", ".join(csharp_params)
    new_params = []
    for raw, an in zip(csharp_params, arg_names):
        # raw = "object data" or just "data" (no type)
        parts = raw.strip().split()
        if len(parts) == 2:
            new_params.append(f'[ExcelArgument("{an}")] {raw}')
        else:
            new_params.append(f'[ExcelArgument("{an}")] {raw}')
    new_joined = ", ".join(new_params)

    if old_joined == new_joined:
        return text, False

    # Replace in the source
    new_text = text.replace(f"({old_joined})", f"({new_joined})")
    if new_text != text:
        return new_text, True

    # Try with different whitespace
    for old_sep in [", ", " , ", ",", " ,"]:
        for new_sep in [", ", ","]:
            trial_old = old_sep.join(csharp_params)
            trial_new = new_sep.join(new_params)
            new_text = text.replace(f"({trial_old})", f"({trial_new})")
            if new_text != text:
                return new_text, True

    return text, False

def process_file(filepath):
    content = filepath.read_text(encoding="utf-8")
    updated = content

    # Find all [ExcelFunction] lines and the following method signature
    pattern = re.compile(
        r'\[ExcelFunction\(Name\s*=\s*"([^"]+)"[^\]]*\)\]'
        r'\s*public static object\s+(\w+)\s*\(([^)]*)\)',
        re.DOTALL
    )

    changes = 0
    for m in pattern.finditer(content):
        func_name = m.group(1)
        raw_params = m.group(3).strip()
        if not raw_params:
            continue
        csharp_params = [p.strip() for p in raw_params.split(",") if p.strip()]

        old_sig = f'({raw_params})'
        arg_names = FUNC_PARAMS.get(func_name, [])
        if not arg_names or len(arg_names) != len(csharp_params):
            continue

        new_params_list = []
        for raw, an in zip(csharp_params, arg_names):
            new_params_list.append(f'[ExcelArgument("{an}")] {raw}')
        new_sig = f'({", ".join(new_params_list)})'

        # Only replace exact match in the source
        if old_sig in updated:
            updated = updated.replace(old_sig, new_sig, 1)
            changes += 1

    if changes > 0:
        filepath.write_text(updated, encoding="utf-8")
        print(f"  {filepath.name}: {changes}/{len(FUNC_PARAMS)} UDF(s)")
    return changes


udf_files = sorted(ROOT.glob("src/**/*Udf.cs"))
total = 0
for f in udf_files:
    total += process_file(f)

print(f"Done. {total} UDFs updated with [ExcelArgument] attributes.")
