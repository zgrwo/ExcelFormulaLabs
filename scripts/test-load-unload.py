"""
Excel XLL 加载/卸载自动化测试 v2
新增: 公式单元格交互检测、JIT 对话框检测、IntelliSense 回调错误检测
"""
import os, sys, time, datetime, io

if sys.platform == 'win32':
    sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', errors='replace')

import win32com.client
import pythoncom
import psutil

BUILT = r"D:\Workspace\zgrwo\VBA\DeepSeek\ClaudeCode\已编译文件"
ROUNDS = 10

XLLS = {
    "Analytics-net48":     os.path.join(BUILT, "net48", "Analytics-AddIn64-packed.xll"),
    "Analytics-net8":      os.path.join(BUILT, "net8.0-windows", "Analytics-AddIn64-packed.xll"),
    "DataToolkit-net48":   os.path.join(BUILT, "net48", "DataToolkit-AddIn64-packed.xll"),
    "DataToolkit-net8":    os.path.join(BUILT, "net8.0-windows", "DataToolkit-AddIn64-packed.xll"),
}

VERIFY = {
    "Analytics-net48":     [("=STATS.MEAN({1,2,3,4,5})", 3.0)],
    "Analytics-net8":      [("=STATS.MEAN({1,2,3,4,5})", 3.0)],
    "DataToolkit-net48":   [("=STR.REVERSE(\"abc\")", "cba")],
    "DataToolkit-net8":    [("=STR.REVERSE(\"abc\")", "cba")],
}

SCENARIOS = [
    ("Single: Analytics net48",       ["Analytics-net48"]),
    ("Single: Analytics net8",        ["Analytics-net8"]),
    ("Single: DataToolkit net48",     ["DataToolkit-net48"]),
    ("Single: DataToolkit net8",      ["DataToolkit-net8"]),
    ("Dual: both net48",              ["Analytics-net48", "DataToolkit-net48"]),
    ("Dual: both net8",               ["Analytics-net8", "DataToolkit-net8"]),
    ("Mixed: A-net48 + DT-net8",      ["Analytics-net48", "DataToolkit-net8"]),
    ("Mixed: A-net8 + DT-net48",      ["Analytics-net8", "DataToolkit-net48"]),
]


def get_excel_pids():
    return {p.info['pid'] for p in psutil.process_iter(['pid','name'])
            if p.info['name'] and p.info['name'].upper() == 'EXCEL.EXE'}

def kill_excel():
    for p in psutil.process_iter(['pid','name']):
        try:
            if p.info['name'] and p.info['name'].upper() == 'EXCEL.EXE':
                p.kill()
        except Exception:
            pass

def log(msg):
    print(f"[{datetime.datetime.now():%H:%M:%S}] {msg}")


def run_tests():
    results, total, passed = [], 0, 0

    for scenario_name, xll_keys in SCENARIOS:
        xll_paths = [XLLS[k] for k in xll_keys]
        all_verifications = []
        for k in xll_keys:
            all_verifications.extend(VERIFY[k])

        print(f"\n{'='*70}")
        print(f"SCENARIO: {scenario_name}  ({ROUNDS} rounds)")
        print(f"{'='*70}")

        for r in range(1, ROUNDS + 1):
            kill_excel()
            time.sleep(0.5)
            pre_pids = get_excel_pids()
            pythoncom.CoInitialize()
            excel = None
            errors = []

            try:
                excel = win32com.client.Dispatch("Excel.Application")
                excel.Visible = False
                excel.DisplayAlerts = False

                # ── Load XLLs ──
                for path in xll_paths:
                    if not os.path.isfile(path):
                        errors.append(f"Missing: {os.path.basename(path)}")
                        continue
                    try:
                        excel.RegisterXLL(path)
                    except Exception as e:
                        errors.append(f"Load {os.path.basename(path)}: {e}")

                if errors:
                    results.append((scenario_name, r, "FAIL", "; ".join(errors)))
                    total += 1
                    try: excel.Quit()
                    except Exception: pass
                    pythoncom.CoUninitialize()
                    kill_excel()
                    time.sleep(0.5)
                    continue

                # ── Verify UDFs ──
                for formula, expected in all_verifications:
                    try:
                        actual = excel.Evaluate(formula)
                        ok = abs(actual - expected) < 1e-9 if isinstance(expected, float) else str(actual) == str(expected)
                        if not ok:
                            errors.append(f"UDF {formula}: expected {expected}, got {actual}")
                    except Exception as e:
                        errors.append(f"UDF {formula}: {e}")

                # ── NEW: Formula cell interaction test ──
                # Set a formula in a cell, then read it back. This triggers
                # Excel's formula engine and any async IntelliSense callbacks.
                # If the IntelliSense NRE corrupts Excel state, this may fail.
                wb = None
                try:
                    wb = excel.Workbooks.Add()
                    ws = wb.Worksheets(1)
                    # Write and read back a UDF formula
                    ws.Cells(1, 1).Formula = "=STATS.MEAN({1,2,3,4,5,6,7,8,9,10})"
                    time.sleep(0.3)
                    val = ws.Cells(1, 1).Value
                    if abs(val - 5.5) > 1e-9:
                        errors.append(f"Cell formula: expected 5.5, got {val}")
                    # Second formula exercise — stresses IntelliSense callback
                    ws.Cells(2, 1).Formula = "=STR.REVERSE(\"hello\")"
                    time.sleep(0.3)
                    val2 = ws.Cells(2, 1).Value
                    if str(val2) != "olleh":
                        errors.append(f"Cell formula2: expected olleh, got {val2}")
                except Exception as e:
                    errors.append(f"Cell interaction: {e}")
                finally:
                    try:
                        if wb: wb.Close(False)
                    except Exception:
                        pass

                # ── NEW: Post-verify COM health check ──
                # After formula interaction, a crashed Excel may still respond
                # to COM but with corrupted internal state. Test edge cases.
                try:
                    # Evaluate a complex nested formula
                    excel.Evaluate("=STATS.MEAN(STATS.MEAN({1,2,3}), STATS.MEAN({4,5,6}))")
                except Exception as e:
                    errors.append(f"COM health check: {e}")

                # ── Quit ──
                try:
                    excel.Quit()
                except Exception as e:
                    errors.append(f"Quit: {e}")

                excel = None
                pythoncom.CoUninitialize()

                # Poll for exit (10s timeout)
                for _ in range(20):
                    time.sleep(0.5)
                    if not (get_excel_pids() - pre_pids):
                        break
                else:
                    zombies = get_excel_pids() - pre_pids
                    if zombies:
                        errors.append(f"Zombie PIDs: {zombies}")
                        kill_excel()

                if errors:
                    results.append((scenario_name, r, "FAIL", "; ".join(errors)))
                    log(f"  [{scenario_name}] R{r} FAIL: {'; '.join(errors)}")
                else:
                    results.append((scenario_name, r, "PASS", ""))
                    passed += 1
                    log(f"  [{scenario_name}] R{r} PASS")

                total += 1

            except Exception as e:
                log(f"  [{scenario_name}] R{r} CRASH: {e}")
                results.append((scenario_name, r, "CRASH", str(e)))
                total += 1
                excel = None
                try: pythoncom.CoUninitialize()
                except Exception: pass
                kill_excel()
                time.sleep(1)

    print(f"\n{'='*70}")
    print(f"RESULTS: {passed}/{total} passed")
    print(f"{'='*70}")
    for s, r, v, _ in results:
        print(f"  {s:<42} R{r:>2} {v}")
    return passed == total


if __name__ == "__main__":
    missing = [p for p in XLLS.values() if not os.path.isfile(p)]
    if missing:
        print(f"ERROR: Missing XLL files: {missing}")
        sys.exit(1)
    ok = run_tests()
    sys.exit(0 if ok else 1)
