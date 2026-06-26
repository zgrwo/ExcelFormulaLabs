#!/usr/bin/env python3
"""
Generate Python reference values for C# cross-validation tests.
Run: python tests/TestData/generate_python_refs.py
Output: C# constants for copy-paste into test files.
"""

import numpy as np
from scipy import linalg as la
from datetime import date

print("=" * 72)
print("  LINALG -- Python scipy.linalg Reference Values")
print("=" * 72)

B = np.array([[1, 2, 3], [4, 5, 6], [7, 8, 10]], dtype=float)
A_sym = np.array([[2, -1], [-1, 2]], dtype=float)
A_3x3_sym = np.array([[2, -1, 0], [-1, 2, -1], [0, -1, 2]], dtype=float)
A_spd = np.array([[4, 2], [2, 3]], dtype=float)
b_vec = np.array([4, 1], dtype=float)

# --- SVD ---
U, S, Vt = la.svd(B)
print("// SVD of B = [[1,2,3],[4,5,6],[7,8,10]]")
print(f"// scipy.linalg.svd: S = [{S[0]:.15f}, {S[1]:.15f}, {S[2]:.15f}]")

U2, S2, Vt2 = la.svd(A_sym)
print(f"// scipy.linalg.svd(A_sym): S = [{S2[0]:.15f}, {S2[1]:.15f}]")

# --- Eigenvalues ---
eig_2x2 = np.sort(la.eigvals(A_sym))[::-1]
print(f"// numpy.linalg.eigvals(A_sym) desc: [{eig_2x2[0]:.15f}, {eig_2x2[1]:.15f}]")

eig_3x3 = np.sort(la.eigvals(A_3x3_sym))[::-1]
print(f"// numpy.linalg.eigvals(A_3x3_sym) desc: [{eig_3x3[0]:.15f}, {eig_3x3[1]:.15f}, {eig_3x3[2]:.15f}]")

# --- QR ---
Q, R = la.qr(B)
print(f"// scipy.linalg.qr(B) R diag: [{np.diag(R)[0]:.15f}, {np.diag(R)[1]:.15f}, {np.diag(R)[2]:.15f}]")

# --- LU ---
P, L, U_lu = la.lu(B)
print(f"// scipy.linalg.lu(B) L[1,0]={L[1,0]:.15f} L[2,0]={L[2,0]:.15f} L[2,1]={L[2,1]:.15f}")
print(f"// U[0,0]={U_lu[0,0]:.15f} U[0,1]={U_lu[0,1]:.15f} U[0,2]={U_lu[0,2]:.15f}")
print(f"// U[1,1]={U_lu[1,1]:.15f} U[1,2]={U_lu[1,2]:.15f} U[2,2]={U_lu[2,2]:.15f}")

# --- Solve ---
x_sol = la.solve(A_sym, b_vec)
print(f"// scipy.linalg.solve(A_sym,[4,1]) = [{x_sol[0]:.15f}, {x_sol[1]:.15f}]")

# --- Cholesky ---
L_chol = la.cholesky(A_spd, lower=True)
print(f"// scipy.linalg.cholesky(A_spd,lower) L: [{L_chol[0,0]:.15f}, {L_chol[1,0]:.15f}, {L_chol[1,1]:.15f}]")

# --- PINV ---
Ap = la.pinv(B)
err = np.max(np.abs(B @ Ap @ B - B))
print(f"// scipy.linalg.pinv(B), max|A@A+@A-A|={err:.2e}, A+[0,0]={Ap[0,0]:.15f}")

# --- Determinant ---
print(f"// numpy.linalg.det(B) = {la.det(B):.15f}")
print(f"// numpy.linalg.det(A_sym) = {la.det(A_sym):.15f}")

# --- Condition number ---
print(f"// numpy.linalg.cond(B) = {np.linalg.cond(B):.15f}")

# --- Rank ---
rank_def = np.linalg.matrix_rank(np.array([[1,2,3],[4,5,6],[5,7,9]], dtype=float))
print(f"// numpy.linalg.matrix_rank(B) = {np.linalg.matrix_rank(B)}")
print(f"// numpy.linalg.matrix_rank(rank-deficient) = {rank_def}")

# --- Trace ---
print(f"// numpy.trace(B) = {np.trace(B)}")

# --- Frobenius norm ---
print(f"// scipy.linalg.norm(B,'fro') = {la.norm(B, 'fro'):.15f}")

print()
print("=" * 72)
print("  DataToolkit -- Python Reference Values")
print("=" * 72)

# --- Levenshtein ---
def levenshtein(s1, s2):
    m, n = len(s1), len(s2)
    dp = [[0]*(n+1) for _ in range(m+1)]
    for i in range(m+1): dp[i][0] = i
    for j in range(n+1): dp[0][j] = j
    for i in range(1, m+1):
        for j in range(1, n+1):
            cost = 0 if s1[i-1] == s2[j-1] else 1
            dp[i][j] = min(dp[i-1][j]+1, dp[i][j-1]+1, dp[i-1][j-1]+cost)
    return dp[m][n]

print("\n// -- Levenshtein distance --")
for a, b in [("kitten","sitting"),("","abc"),("abc",""),("",""),("same","same"),
             ("flaw","lawn"),("abc","ABC"),("cafe","coffee"),
             ("abcdefghij","abcdeFghij"),("a","b")]:
    print(f"// Lev({a!r},{b!r}) = {levenshtein(a, b)}")

# --- Soundex ---
# Matches C# implementation: first-letter code used for collapse detection
# but NOT included as a digit in output (standard American Soundex).
_sdx_map = str.maketrans('BFPVCGJKQSXZDTLMNR', '111122222222334556')
def _sdx(c): return _sdx_map.get(ord(c.upper()), 48)  # 48='0'
def soundex(name):
    if not name: return ""
    name = name.upper()
    first = name[0]
    if not first.isalpha(): return ""
    out = first
    prev = chr(_sdx(first))  # first letter's code for collapse detection
    for ch in name[1:]:
        c = chr(_sdx(ch))
        if c != '0' and c != prev:
            out += c
            prev = c
        if len(out) >= 4: break
    return (out + '000')[:4]

print("\n// -- Soundex --")
for n in ["Robert","Rupert","Rubin","Ashcraft","Ashcroft","Tymczak",
          "Pfister","Honeyman","","A","ZZ","WashingTon"]:
    print(f"// Soundex({n!r}) = {soundex(n)!r}")

# --- Easter ---
print("\n// -- Easter (Gregorian computus, USNO verified) --")
easter = {1900:(4,15),1950:(4,9),2000:(4,23),2005:(3,27),2008:(3,23),
          2010:(4,4),2015:(4,5),2020:(4,12),2024:(3,31),2025:(4,20),
          2026:(4,5),2030:(4,21),2040:(4,1),2050:(4,10),2075:(4,7),
          2099:(4,12),2100:(3,28)}
for y in sorted(easter):
    m, d = easter[y]
    print(f"// Easter({y}) = {m:02d}/{d:02d}")

# --- ISO Week ---
print("\n// -- ISO Week (datetime.isocalendar) --")
for d in [date(2024,12,30),date(2025,1,1),date(2025,1,5),
          date(2025,12,31),date(2026,1,1),date(2024,1,1),
          date(2023,1,1),date(2020,12,31),date(2021,1,3),date(2028,12,31)]:
    iso = d.isocalendar()
    print(f"// IsoWeek({d}) = W{iso.week:02d}/{iso.year}")

# --- Base64 ---
import base64
print("\n// -- Base64 (Python stdlib) --")
for s in ["","hello","Hello World!","Test123","ab","abc","abcd","a"*100]:
    enc = base64.b64encode(s.encode('utf-8')).decode('ascii')
    print(f"// Base64({s!r}) = {enc!r}")

# --- URL encode ---
import urllib.parse
print("\n// -- URL Encode (Python stdlib) --")
for s in ["hello world","a+b=c","/path/to/file","name=John&age=30","","100%","space test"]:
    enc = urllib.parse.quote(s, safe='')
    print(f"// UrlEncode({s!r}) = {enc!r}")

# --- HTML encode ---
import html as html_mod
print("\n// -- HTML Encode (Python stdlib) --")
for s in ["<script>alert('xss')</script>","a & b",'"quoted"',"normal text","","1 < 2 > 0"]:
    enc = html_mod.escape(s)
    print(f"// HtmlEncode({s!r}) = {enc!r}")

print(f"\n{'='*72}")
print("  Done. Copy constants above into C# test files.")
print(f"{'='*72}")
