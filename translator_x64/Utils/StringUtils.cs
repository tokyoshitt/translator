using System;

namespace translator_x64.Utils
{
    public static class StringUtils
    {
        public static string EscapeJson(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            return text.Replace("\\", "\\\\")
                       .Replace("\"", "\\\"")
                       .Replace("\n", "\\n")
                       .Replace("\r", "\\r")
                       .Replace("\t", "\\t");
        }

        public static string CleanParameter(string param)
        {
            if (param == null) return "";
            return param.Trim().Trim('"').Trim('\'');
        }
    }
}
