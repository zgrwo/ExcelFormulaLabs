<#
.SYNOPSIS
    Patch VERSIONINFO resource in an .xll (PE) file using the Windows
    BeginUpdateResource / UpdateResource / EndUpdateResource API.

.DESCRIPTION
    Reads the RT_VERSION resource via LoadLibraryEx (as data file),
    parses the VS_VERSION_INFO tree to locate String entries by key name,
    replaces their values in-place within the resource buffer (with
    null-padding for shorter strings, expanded buffer for longer strings),
    recalculates all wLength fields up the ancestor chain, and writes
    the modified resource back via UpdateResource.

    This approach:
      - Works regardless of the current VERSIONINFO default strings
      - Locates entries by key name, not by hardcoded byte patterns
      - Handles UTF-16 strings of any length correctly
      - Preserves all other VERSIONINFO entries untouched

.PARAMETER XllPath
    Path to the .xll file to patch.

.PARAMETER FileDescription
    New value for the FileDescription VERSIONINFO entry.

.PARAMETER ProductName
    New value for the ProductName VERSIONINFO entry.

.EXAMPLE
    .\patch-xll-version.ps1 -XllPath "Analytics-AddIn-packed.xll" `
        -FileDescription "统计 · 线性代数 · 回归 · 物理化学 — 75 个科学计算函数" `
        -ProductName "Excel 函数增强库"
#>

param(
    [Parameter(Mandatory = $true)] [string] $XllPath,
    [Parameter(Mandatory = $true)] [string] $FileDescription,
    [Parameter(Mandatory = $true)] [string] $ProductName
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

if (-not (Test-Path $XllPath)) {
    Write-Host "Skipping VERSIONINFO patch: $XllPath not found"
    exit 0
}

$resolvedPath = (Resolve-Path $XllPath).Path

$cs = @'
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

public static class VersionInfoPatcher
{
    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    static extern IntPtr LoadLibraryExW(string lpFileName, IntPtr hFile, uint dwFlags);

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool FreeLibrary(IntPtr hModule);

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern IntPtr FindResourceW(IntPtr hModule, IntPtr lpName, IntPtr lpType);

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern IntPtr FindResourceExW(IntPtr hModule, IntPtr lpType, IntPtr lpName, ushort wLanguage);

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern IntPtr LoadResource(IntPtr hModule, IntPtr hResInfo);

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern IntPtr LockResource(IntPtr hResData);

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern uint SizeofResource(IntPtr hModule, IntPtr hResInfo);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    static extern IntPtr BeginUpdateResourceW(string pFileName, bool bDeleteExistingResources);

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool UpdateResourceW(IntPtr hUpdate, IntPtr lpType, IntPtr lpName,
        ushort wLanguage, byte[] lpData, uint cb);

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool EndUpdateResourceW(IntPtr hUpdate, bool fDiscard);

    const uint LOAD_LIBRARY_AS_DATAFILE = 0x00000002;
    static readonly IntPtr RT_VERSION = (IntPtr)16;
    static readonly IntPtr VS_VERSION_INFO = (IntPtr)1;

    // In-memory tracking: (offset, length) of each string VALUE within the resource.
    struct StringEntry
    {
        public string Key;          // e.g. "FileDescription"
        public int ValueOffset;     // byte offset within the resource
        public int ValueBytes;      // current value length in bytes (UTF-16LE, null-terminated)
        public int wValueLengthOff; // offset of the wValueLength field (2 bytes)
        public int wLengthOff;      // offset of the wLength field (2 bytes)
    }

    // Ancestor chain for wLength recalculation.
    struct WLengthEntry
    {
        public int wLengthOff;  // byte offset of wLength field
        public int ChildOffset; // byte offset where the child (whose size changed) starts
        public int ChildWLenOff;// byte offset of child's wLength (to read new size)
    }

    // ---------------------------------------------------------------

    public static int Patch(string filePath, string newFileDescription, string newProductName)
    {
        // 1. Read VERSIONINFO resource bytes
        byte[] data;
        ushort language;
        if (!TryReadVersionResource(filePath, out data, out language))
        {
            Console.Error.WriteLine("ERROR: Cannot read VERSIONINFO resource.");
            return 3;
        }

        // 2. Walk tree, locate FileDescription + ProductName entries and their
        //    ancestor wLength chains.
        var entries = new List<StringEntry>();
        var ancestors = new Dictionary<string, List<WLengthEntry>>(StringComparer.OrdinalIgnoreCase);
        try { WalkTree(data, 0, data.Length, new List<WLengthEntry>(), entries, ancestors); }
        catch (Exception ex)
        {
            Console.Error.WriteLine("ERROR: Walk failed - " + ex.Message);
            return 4;
        }

        var updates = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        updates["FileDescription"] = newFileDescription;
        updates["ProductName"] = newProductName;

        // 3. Patch each target entry in value-offset order. VERSIONINFO structures are
        //    stored sequentially, so after expanding/contracting entry N, every field of
        //    entry N+1 (ValueOffset, wLengthOff, wValueLengthOff) has moved by the
        //    accumulated delta - track it as shift. Ancestor wLength fields (root,
        //    StringFileInfo, StringTable) always sit BEFORE the first patched String,
        //    so their offsets never move; only their values accumulate deltas.
        int shift = 0;
        int patched = 0;
        foreach (var entry in entries.OrderBy(e => e.ValueOffset))
        {
            string newValue;
            if (!updates.TryGetValue(entry.Key, out newValue))
                continue;

            int vo = entry.ValueOffset + shift;
            int wLengthOff = entry.wLengthOff + shift;
            int wValueLengthOff = entry.wValueLengthOff + shift;

            byte[] newBytes = Encoding.Unicode.GetBytes(newValue + "\0");
            // Ensure WORD alignment (value length is stored in WORDS)
            if (newBytes.Length % 2 != 0)
            {
                byte[] p = new byte[newBytes.Length + 1];
                Buffer.BlockCopy(newBytes, 0, p, 0, newBytes.Length);
                newBytes = p;
            }

            // Expand/contract at the ALIGNED end of the old value so subsequent
            // structures keep their 4-byte alignment.
            int oldEnd = vo + entry.ValueBytes;
            int oldEndAligned = Align4(oldEnd);
            int newEndAligned = Align4(vo + newBytes.Length);
            int delta = newEndAligned - oldEndAligned;
            if (delta != 0)
                data = ExpandBuffer(data, oldEndAligned, delta);

            // Write the new value; zero-fill any padding up to the aligned end.
            Buffer.BlockCopy(newBytes, 0, data, vo, newBytes.Length);
            int paddedEnd = Align4(vo + newBytes.Length);
            for (int i = vo + newBytes.Length; i < paddedEnd; i++)
                data[i] = 0;

            // Patch wValueLength (WORDS) - offset never moves (before the value).
            WriteU16(data, wValueLengthOff, (ushort)(newBytes.Length / 2));

            // Patch wLength of the String node itself and every ancestor.
            ushort selfLen = BitConverter.ToUInt16(data, wLengthOff);
            WriteU16(data, wLengthOff, (ushort)(selfLen + delta));
            if (ancestors.ContainsKey(entry.Key))
            {
                foreach (var wle in ancestors[entry.Key])
                {
                    // Ancestor offsets never shift (they precede all String values).
                    ushort cur = BitConverter.ToUInt16(data, wle.wLengthOff);
                    WriteU16(data, wle.wLengthOff, (ushort)Math.Min(ushort.MaxValue, Math.Max(0, cur + delta)));
                }
            }

            Console.WriteLine("  [{0}] patched (delta {1})", entry.Key, delta);
            shift += delta;
            patched++;
        }

        if (patched == 0)
        {
            Console.WriteLine("Nothing patched (entries not found; already patched?)");
            return 0;
        }

        // 4. Write back via UpdateResource
        if (!WriteVersionResource(filePath, data, language))
        {
            Console.Error.WriteLine("ERROR: UpdateResource failed (file in use?).");
            return 5;
        }

        Console.WriteLine(string.Format("VERSIONINFO patched successfully (language 0x{0:X4}).", language));
        return 0;
    }
    // ---------------------------------------------------------------
    // Tree walker — finds String entries and tracks wLength ancestor chain.
    // ---------------------------------------------------------------

    static int WalkTree(byte[] data, int offset, int bound,
        List<WLengthEntry> chain,
        List<StringEntry> entries,
        Dictionary<string, List<WLengthEntry>> ancestors)
    {
        if (offset + 6 > bound) return bound;

        ushort wLength = BitConverter.ToUInt16(data, offset);
        ushort wValueLength = BitConverter.ToUInt16(data, offset + 2);
        ushort wType = BitConverter.ToUInt16(data, offset + 4);

        // Read key
        int ks = offset + 6;
        int ke = ks;
        while (ke + 1 < bound && !(data[ke] == 0 && data[ke + 1] == 0)) ke += 2;
        int kbl = ke - ks;
        string key = Encoding.Unicode.GetString(data, ks, kbl);

        int afterKey = Align4(ke + 2);

        if (wType == 1 && wValueLength > 0)
        {
            // This is a String entry (leaf) — record it
            var se = new StringEntry
            {
                Key = key,
                ValueOffset = afterKey,
                ValueBytes = wValueLength * 2,
                wValueLengthOff = offset + 2,
                wLengthOff = offset
            };
            entries.Add(se);
            // Save ancestor chain for wLength patching later
            ancestors[key] = new List<WLengthEntry>(chain);
        }

        // Root value is VS_FIXEDFILEINFO (always 52 bytes), not wValueLength * 2.
        bool isRoot = (key == "VS_VERSION_INFO");
        int valueBytes = isRoot ? 52 : wValueLength * 2;

        // Walk children — some nodes have both a value AND children (e.g. root)
        int childOffset = wValueLength > 0
            ? afterKey + valueBytes   // after value
            : afterKey;               // after key (container)

        int nodeEnd = offset + wLength;
        int effectiveEnd = Math.Min(nodeEnd, bound);

        bool hasChildren = (wValueLength == 0) || isRoot || (childOffset + 6 <= effectiveEnd);

        if (hasChildren)
        {
            while (childOffset + 6 <= effectiveEnd)
            {
                var wle = new WLengthEntry
                {
                    wLengthOff = offset,
                    ChildOffset = childOffset,
                    ChildWLenOff = childOffset
                };
                chain.Add(wle);

                childOffset = WalkTree(data, childOffset, effectiveEnd, chain, entries, ancestors);

                if (chain.Count > 0) chain.RemoveAt(chain.Count - 1);

                childOffset = Align4(childOffset);
            }
        }

        return Math.Max(nodeEnd, childOffset);
    }

    // ---------------------------------------------------------------
    // In-place buffer expansion / contraction
    // ---------------------------------------------------------------

    static byte[] ExpandBuffer(byte[] data, int insertAt, int delta)
    {
        if (delta == 0) return data;
        byte[] newData = new byte[data.Length + delta];
        int copyBefore = insertAt;
        Buffer.BlockCopy(data, 0, newData, 0, copyBefore);
        int tailStart = insertAt;
        int tailLen = data.Length - tailStart;
        if (delta > 0)
        {
            // Expanding: tail moves right
            Buffer.BlockCopy(data, tailStart, newData, insertAt + delta, tailLen);
        }
        else
        {
            // Contracting: tail moves left
            Buffer.BlockCopy(data, tailStart, newData, insertAt + delta, tailLen);
        }
        return newData;
    }

    // ---------------------------------------------------------------
    // wLength chain updater — walks up ancestors adding delta to each.
    // ---------------------------------------------------------------

    static void UpdateWLengthChain(byte[] data, List<WLengthEntry> chain, int delta)
    {
        for (int i = chain.Count - 1; i >= 0; i--)
        {
            var wle = chain[i];
            ushort cur = BitConverter.ToUInt16(data, wle.wLengthOff);
            int adj = cur + delta;
            if (adj < 0) adj = 0;
            if (adj > ushort.MaxValue) adj = ushort.MaxValue;
            WriteU16(data, wle.wLengthOff, (ushort)adj);
        }
    }

    // ---------------------------------------------------------------
    // P/Invoke wrappers
    // ---------------------------------------------------------------

    static bool TryReadVersionResource(string path, out byte[] data, out ushort language)
    {
        data = null;
        language = 0;
        IntPtr hModule = LoadLibraryExW(path, IntPtr.Zero, LOAD_LIBRARY_AS_DATAFILE);
        if (hModule == IntPtr.Zero) return false;
        try
        {
            // Find the resource language explicitly. UpdateResourceW writes back with the
            // SAME language; writing with language=0 creates a duplicate resource that
            // FileVersionInfo/Explorer (which read 0x0409) never see — the historical
            // "VERSIONINFO patched successfully but reads empty" bug.
            foreach (ushort lang in new ushort[] { 0x0409, 0x0000, 0x0400, 0x0804, 0x0C09, 0x0411, 0x0809 })
            {
                IntPtr hRes = FindResourceExW(hModule, RT_VERSION, VS_VERSION_INFO, lang);
                if (hRes == IntPtr.Zero) continue;
                uint size = SizeofResource(hModule, hRes);
                if (size == 0) continue;
                IntPtr hLock = LockResource(LoadResource(hModule, hRes));
                if (hLock == IntPtr.Zero) continue;
                data = new byte[size];
                Marshal.Copy(hLock, data, 0, (int)size);
                language = lang;
                return true;
            }
            // Fallback: language-agnostic lookup
            IntPtr hRes2 = FindResourceW(hModule, VS_VERSION_INFO, RT_VERSION);
            if (hRes2 == IntPtr.Zero) return false;
            uint size2 = SizeofResource(hModule, hRes2);
            if (size2 == 0) return false;
            IntPtr hLock2 = LockResource(LoadResource(hModule, hRes2));
            if (hLock2 == IntPtr.Zero) return false;
            data = new byte[size2];
            Marshal.Copy(hLock2, data, 0, (int)size2);
            language = 0;
            return true;
        }
        finally { FreeLibrary(hModule); }
    }

    static bool WriteVersionResource(string path, byte[] resource, ushort language)
    {
        IntPtr hUpdate = BeginUpdateResourceW(path, false);
        if (hUpdate == IntPtr.Zero) return false;
        try
        {
            // P1-12 (review): write back with the ORIGINAL language — language=0 produced a
            // duplicate resource invisible to standard readers (FileVersionInfo/Explorer).
            if (!UpdateResourceW(hUpdate, RT_VERSION, VS_VERSION_INFO, language,
                resource, (uint)resource.Length)) return false;
            return EndUpdateResourceW(hUpdate, false);
        }
        catch
        {
            EndUpdateResourceW(hUpdate, true);
            throw;
        }
    }

    // ---------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------

    static int Align4(int v) { return (v + 3) & ~3; }

    static void WriteU16(byte[] buf, int offset, ushort val)
    {
        buf[offset] = (byte)(val & 0xFF);
        buf[offset + 1] = (byte)(val >> 8);
    }
}
'@

Add-Type -TypeDefinition $cs -ErrorAction Stop
Write-Host "Patching VERSIONINFO: $resolvedPath"
$exitCode = [VersionInfoPatcher]::Patch($resolvedPath, $FileDescription, $ProductName)
exit $exitCode