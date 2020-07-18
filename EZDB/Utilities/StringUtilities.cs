using System;
using System.Collections.Generic;
using System.Text;

namespace EZDB.Utilities
{
    internal static class StringUtilities
    {
        internal static string AppendCommaSeperatedString(string sValue, string sAppend)
        {
            return sValue.Length == 0
                ? sAppend
                : $"{sValue}, {sAppend}";
        }

        #region ExtensionMethods
        internal static string EscapeForSql(this string str)
        {
            return str.Replace("\'", "\'\'");
        }

        /// <summary>
        /// Get substring of specified number of characters on the right.
        /// </summary>
        public static string Right(this string value, int length)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;

            return value.Length <= length ? value : value.Substring(value.Length - length);
        }

        /// <summary>
        /// Get substring of specified number of characters on the left.
        /// </summary>
        public static string Left(this string value, int length)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;

            return value.Length <= length ? value : value.Substring(0, length);
        }
        #endregion
    }
}
