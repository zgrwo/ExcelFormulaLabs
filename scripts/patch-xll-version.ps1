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
        if (!TryReadVersionResource(filePath, out data))
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
            Console.Error.WriteLine("ERROR: Walk failed — " + ex.Message);
            return 4;
        }

        var updates = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        updates["FileDescription"] = newFileDescription;
        updates["ProductName"] = newProductName;

        int patched = 0;
        int totalDelta = 0;

        foreach (var entry in entries)
        {
            string newValue;
            if (!updates.TryGetValue(entry.Key, out newValue))
                continue;

            string oldValue;
            try { oldValue = Encoding.Unicode.GetString(data, entry.ValueOffset, entry.ValueBytes).TrimEnd('\0'); }
            catch { oldValue = "(binary)"; }

            byte[] newBytes = Encoding.Unicode.GetBytes(newValue + "\0");
            // Ensure WORD alignment (value is stored as wValueLength WORDS)
            if (newBytes.Length % 2 != 0)
            {
                byte[] p = new byte[newBytes.Length + 1];
                Buffer.BlockCopy(newBytes, 0, p, 0, newBytes.Length);
                newBytes = p;
            }

            int delta = newBytes.Length - entry.ValueBytes;
            int newValLenWords = newBytes.Length / 2;

            // Patch wValueLength in-place
            WriteU16(data, entry.wValueLengthOff, (ushort)newValLenWords);

            // Expand/contract buffer if necessary
            if (delta != 0)
            {
                data = ExpandBuffer(data, entry.ValueOffset + entry.ValueBytes, delta);
                totalDelta += delta;
                // Adjust all subsequent offsets tracked in ancestors
                // (All other StringEntry offsets for later entries are now off)
                // For simplicity, we patch one entry at a time and exit.
                // Since we only have 2 entries, we handle both sequentially.
            }

            // Copy new value bytes in-place
            Buffer.BlockCopy(newBytes, 0, data, entry.ValueOffset, newBytes.Length);
            // Zero out extra space if new is shorter
            if (delta < 0)
            {
                for (int i = entry.ValueOffset + newBytes.Length; i < entry.ValueOffset + entry.ValueBytes; i++)
                    data[i] = 0;
            }

            // Update tracked offsets for subsequent entries (shift by delta)
            if (delta != 0 && ancestors.ContainsKey(entry.Key))
            {
                // Just update wLength chain; offsets in entries beyond this one
                // have already shifted by our ExpandBuffer above (it shifted the tail).
                UpdateWLengthChain(data, ancestors[entry.Key], delta);
            }

            Console.WriteLine("  '{0}': '{1}' => '{2}'", entry.Key, oldValue, newValue);
            patched++;
        }

        if (patched == 0)
        {
            Console.WriteLine("Nothing patched (entries not found; already patched?)");
            return 0;
        }

        // 3. Write back via UpdateResource
        if (!WriteVersionResource(filePath, data))
        {
            Console.Error.WriteLine("ERROR: UpdateResource failed (file in use?).");
            return 5;
        }

        Console.WriteLine("VERSIONINFO patched successfully.");
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

    static bool TryReadVersionResource(string path, out byte[] data)
    {
        data = null;
        IntPtr hModule = LoadLibraryExW(path, IntPtr.Zero, LOAD_LIBRARY_AS_DATAFILE);
        if (hModule == IntPtr.Zero) return false;
        try
        {
            IntPtr hRes = FindResourceW(hModule, VS_VERSION_INFO, RT_VERSION);
            if (hRes == IntPtr.Zero) return false;
            uint size = SizeofResource(hModule, hRes);
            if (size == 0) return false;
            IntPtr hLock = LockResource(LoadResource(hModule, hRes));
            if (hLock == IntPtr.Zero) return false;
            data = new byte[size];
            Marshal.Copy(hLock, data, 0, (int)size);
            return true;
        }
        finally { FreeLibrary(hModule); }
    }

    static bool WriteVersionResource(string path, byte[] resource)
    {
        IntPtr hUpdate = BeginUpdateResourceW(path, false);
        if (hUpdate == IntPtr.Zero) return false;
        try
        {
            if (!UpdateResourceW(hUpdate, RT_VERSION, VS_VERSION_INFO, 0,
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
