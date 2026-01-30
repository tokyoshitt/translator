using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;

namespace translator.Utils
{
    public static class Logger
    {
        // Disabled logging for production
        public static void WriteLog(string level, string code, string details = "") { }
        public static void WriteLog(string message) { }
        public static void Success(string code, string details = "") { }
        public static void Warning(string code, string details = "") { }
        public static void Info(string code, string details = "") { }
        public static void Error(string code, string details = "") { }
        public static void CreateErrorCodesFile() { }
    }
}
