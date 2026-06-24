#!/usr/bin/env python3
"""Fix duplicate [ExcelArgument] and remove from non-UDF helper methods."""
import re
from pathlib import Path

ROOT = Path(__file__).parent.parent

for fpath in sorted(ROOT.glob("src/**/*Udf.cs")):
    content = fpath.read_text(encoding="utf-8")
    original = content

    # Fix: remove duplicate [ExcelArgument] (e.g. "...] [ExcelArgument(...]")
    for _ in range(20):  # iterate until stable
        new_content = re.sub(
            r'\[ExcelArgument\("[^"]*"\)\]\s*\[ExcelArgument\("',
            '[ExcelArgument("',
            content)
        if new_content == content:
            break
        content = new_content

    # Fix: remove [ExcelArgument] from V(), M(), D() helpers
    for helper_re in [
        r'(private static double\[\] V\()\s*\[ExcelArgument\("[^"]*"\)\]\s*',
        r'(private static double\[,\] M\()\s*\[ExcelArgument\("[^"]*"\)\]\s*',
        r'(private static DateTime D\()\s*\[ExcelArgument\("[^"]*"\)\]\s*',
    ]:
        content = re.sub(helper_re, r'\1', content)

    if content != original:
        fpath.write_text(content, encoding="utf-8")
        print(f"Fixed: {fpath.name}")
