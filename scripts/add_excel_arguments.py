#!/usr/bin/env python3
"""Add/update [ExcelArgument] attributes on all UDF parameters from api-reference.md."""
import re
from pathlib import Path

ROOT = Path(__file__).parent.parent

def parse_api_params():
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

def process_file(filepath):
    content = filepath.read_text(encoding="utf-8")
    updated = content

    # Find all [ExcelFunction] lines with their method signatures
    # Match: [ExcelFunction(...)] public static object NAME(params)
    pattern = re.compile(
        r'\[ExcelFunction\(Name\s*=\s*"([^"]+)"[^\]]*\)\]'
        r'\s*(?:public|private)\s+static\s+\S+\s+(\w+)\s*\(([^)]*)\)',
        re.DOTALL
    )

    changes = 0
    for m in pattern.finditer(content):
        func_name = m.group(1)
        raw_params = m.group(3).strip()
        if not raw_params:
            continue

        csharp_params_raw = [p.strip() for p in raw_params.split(",") if p.strip()]

        # Strip any existing [ExcelArgument] to get clean "type name"
        csharp_params = []
        for p in csharp_params_raw:
            cleaned = re.sub(r'\[ExcelArgument\("[^"]*"\)\]\s*', '', p).strip()
            csharp_params.append(cleaned)

        arg_names = FUNC_PARAMS.get(func_name, [])
        if not arg_names or len(arg_names) != len(csharp_params):
            continue

        # Build new signature
        new_params_list = []
        for raw, an in zip(csharp_params, arg_names):
            new_params_list.append(f'[ExcelArgument("{an}")] {raw}')
        old_sig = f'({raw_params})'
        new_sig = f'({", ".join(new_params_list)})'

        if old_sig != new_sig and old_sig in updated:
            updated = updated.replace(old_sig, new_sig, 1)
            changes += 1

    if changes > 0:
        filepath.write_text(updated, encoding="utf-8")
        print(f"  {filepath.name}: {changes} UDF(s) updated")
    return changes


udf_files = sorted(ROOT.glob("src/**/*Udf.cs"))
total = 0
for f in udf_files:
    total += process_file(f)
print(f"Done. {total} UDFs updated.")
