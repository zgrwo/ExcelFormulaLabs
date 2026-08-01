namespace ExcelFormulaLabs.Foundation
{
    /// <summary>
    /// Sentinel type representing an Excel empty cell.
    /// Distinct from <c>null</c> (no value), <see cref="System.DBNull"/> (database null),
    /// and <c>""</c> (empty string).
    /// </summary>
    /// <remarks>
    /// In VBA, <c>Empty</c> is a first-class variant subtype. It is the default
    /// value of an uninitialised Variant. An empty worksheet cell also returns
    /// <c>Empty</c> when its <c>.Value</c> is read.
    ///
    /// <see cref="ElementWiseMapper"/> propagates <see cref="ExcelEmpty"/> values
    /// through unchanged — an empty input cell produces an empty output cell.
    /// <see cref="InputNormalizer"/> recognises <see cref="ExcelEmpty"/> and
    /// converts it to default values (<c>""</c>, <c>0</c>, <c>false</c>) when
    /// type coercion is requested.
    /// </remarks>
    public sealed class ExcelEmpty
    {
        /// <summary>The single canonical instance.</summary>
        public static readonly ExcelEmpty Value = new ExcelEmpty();

        private ExcelEmpty() { }

        /// <summary>Returns "Empty".</summary>
        public override string ToString() => "Empty";

        /// <summary>All ExcelEmpty instances are equal by reference.</summary>
        public override bool Equals(object? obj) =>
            ReferenceEquals(this, obj);

        /// <inheritdoc/>
        public override int GetHashCode() => 0;
    }
}
