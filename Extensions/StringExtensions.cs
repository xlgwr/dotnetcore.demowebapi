namespace System {
    /// <summary>
    /// Extension methods for String class.
    /// </summary>
    public static class StringExtensions {

        public static string IsNullToString (this string str, string defaultstr = "") {
            if (string.IsNullOrEmpty (str) || string.IsNullOrWhiteSpace (str))
                return defaultstr;
            return str;
        }

    }
}