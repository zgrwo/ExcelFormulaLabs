using System;

namespace ExcelVbaLibraries.Foundation
{
    /// <summary>
    /// Represents an Excel error value (e.g. #VALUE!, #N/A, #DIV/0!).
    /// Immutable value type — two errors are equal when they have the same Code.
    /// </summary>
    /// <remarks>
    /// Error codes mirror the Excel COM xlCVError constants:
    ///   xlErrNull  = 2000
    ///   xlErrDiv0  = 2007
    ///   xlErrValue = 2015
    ///   xlErrRef   = 2023
    ///   xlErrName  = 2029
    ///   xlErrNum   = 2036
    ///   xlErrNA    = 2042
    ///
    /// These are the canonical codes used by VBA's CVErr() function and by
    /// Excel-DNA's ExcelError utilities. Foundation keeps its own copy to
    /// avoid an Excel-DNA dependency.
    /// </remarks>
    public readonly struct ExcelError : IEquatable<ExcelError>
    {
        /// <summary>Error code — matches the xlCVError COM constant.</summary>
        public int Code { get; }

        /// <summary>Create an error value with the given code.</summary>
        public ExcelError(int code) => Code = code;

        // ── Well-known error values ──────────────────────────────────────

        /// <summary>#NULL!  — Intersection of two ranges that don't intersect.</summary>
        public static readonly ExcelError Null  = new ExcelError(2000);

        /// <summary>#DIV/0! — Division by zero.</summary>
        public static readonly ExcelError Div0  = new ExcelError(2007);

        /// <summary>#VALUE! — Wrong type of argument or operand.</summary>
        public static readonly ExcelError Value = new ExcelError(2015);

        /// <summary>#REF!   — Invalid cell reference.</summary>
        public static readonly ExcelError Ref   = new ExcelError(2023);

        /// <summary>#NAME?  — Unrecognised formula name.</summary>
        public static readonly ExcelError Name  = new ExcelError(2029);

        /// <summary>#NUM!   — Invalid numeric value (e.g. sqrt of negative).</summary>
        public static readonly ExcelError Num   = new ExcelError(2036);

        /// <summary>#N/A    — Value not available (e.g. lookup failure).</summary>
        public static readonly ExcelError NA    = new ExcelError(2042);

        // ── Equality ─────────────────────────────────────────────────────

        /// <inheritdoc/>
        public bool Equals(ExcelError other) => Code == other.Code;

        /// <inheritdoc/>
        public override bool Equals(object? obj) =>
            obj is ExcelError other && Equals(other);

        /// <inheritdoc/>
        public override int GetHashCode() => Code;

        /// <summary>Structural equality — same code = same error.</summary>
        public static bool operator ==(ExcelError left, ExcelError right) =>
            left.Code == right.Code;

        /// <summary>Structural inequality.</summary>
        public static bool operator !=(ExcelError left, ExcelError right) =>
            left.Code != right.Code;

        // ── Diagnostics ──────────────────────────────────────────────────

        /// <summary>
        /// Excel error name for this error code (e.g. "#VALUE!", "#N/A").
        /// Returns <c>"#ERR({Code})"</c> for unknown error codes.
        /// </summary>
        public string ErrorName => Code switch
        {
            2000 => "#NULL!",
            2007 => "#DIV/0!",
            2015 => "#VALUE!",
            2023 => "#REF!",
            2029 => "#NAME?",
            2036 => "#NUM!",
            2042 => "#N/A",
            _    => $"#ERR({Code})"
        };

        /// <summary>Human-readable representation, e.g. "#VALUE!" for error 2015.</summary>
        public override string ToString() => ErrorName;
    }
}
