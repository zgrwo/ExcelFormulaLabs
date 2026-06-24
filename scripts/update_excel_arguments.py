#!/usr/bin/env python3
"""Strip existing [ExcelArgument] and inject new Excel-native names from api-reference.md."""
import re
from pathlib import Path

ROOT = Path(__file__).parent.parent

api_md = ROOT / "docs" / "api-reference.md"
func_params = {}
for m in re.finditer(r"\| `([A-Z]+\.[A-Z0-9_]+)` \| \(([^)]*)\) \|", api_md.read_text(encoding="utf-8")):
    func_params[m.group(1)] = [p.strip() for p in m.group(2).split(",") if p.strip()]

total = 0
for fpath in sorted(ROOT.glob("src/**/*Udf.cs")):
    content = fpath.read_text(encoding="utf-8")
    # Strip all existing [ExcelArgument]
    content = re.sub(r'\[ExcelArgument\("[^"]*"\)\]\s*', '', content)
    updated = content
    # Inject new ones
    for m in re.finditer(r'\[ExcelFunction\(Name\s*=\s*"([^"]+)"[^\]]*\)\]\s*public static object\s+\w+\s*\(([^)]*)\)', updated):
        func_name = m.group(1)
        raw_params = m.group(2).strip()
        if not raw_params:
            continue
        csharp_params = [p.strip() for p in raw_params.split(",") if p.strip()]
        new_names = func_params.get(func_name, [])
        if len(new_names) != len(csharp_params):
            continue
        new_parts = ['[ExcelArgument("{}")] {}'.format(an, cp) for cp, an in zip(csharp_params, new_names)]
        old_sig = "(" + raw_params + ")"
        new_sig = "(" + ", ".join(new_parts) + ")"
        updated = updated.replace(old_sig, new_sig, 1)
        total += 1
    if updated != content:
        fpath.write_text(updated, encoding="utf-8")
        print("  {}: synced".format(fpath.name))
print("Done. {} UDFs updated.".format(total))
