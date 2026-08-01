using System;
using System.Resources;

namespace ExcelFormulaLabs.Foundation
{
    /// <summary>
    /// Centralized error message accessor. All user-facing exception messages
    /// should be sourced from <c>ErrorMessages.resx</c> via this class,
    /// enabling future localization without touching Core logic.
    /// </summary>
    /// <remarks>
    /// Usage:
    /// <code>
    ///   throw new ArgumentException(ErrorMsg.Get("FS_ReadLimitExceeded", MaxReadBytes));
    /// </code>
    /// Falls back to the key name itself if the resource is missing (fail-safe).
    /// </remarks>
    public static class ErrorMsg
    {
        private static readonly ResourceManager Rm = new(
            "ExcelFormulaLabs.Foundation.ErrorMessages",
            typeof(ErrorMsg).Assembly);

        /// <summary>
        /// Retrieve a localized error message by key, with optional format arguments.
        /// </summary>
        /// <param name="key">Resource key (e.g. "FS_ReadLimitExceeded").</param>
        /// <param name="args">Format arguments for placeholders {0}, {1}, etc.</param>
        /// <returns>Formatted message string; falls back to key if resource missing.</returns>
        public static string Get(string key, params object[] args)
        {
            var template = Rm.GetString(key);
            if (template == null)
                return key; // fail-safe: return key name so message is never null
            return args.Length > 0
                ? string.Format(System.Globalization.CultureInfo.CurrentCulture, template, args)
                : template;
        }
    }
}
