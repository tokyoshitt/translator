using System;
using System.Runtime.InteropServices;
using System.Text;
using translator_x64.Handlers;
using translator_x64.Utils;

namespace translator_x64
{
    public class DllEntry
    {
        private static bool initialized = false;
        
        // Делегат для callback функции Arma
        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        public delegate int CallbackDelegate(
            [MarshalAs(UnmanagedType.LPStr)] string name,
            [MarshalAs(UnmanagedType.LPStr)] string function,
            [MarshalAs(UnmanagedType.LPStr)] string data);
        
        private static CallbackDelegate callbackFunc = null;

        private static void Initialize()
        {
            if (!initialized)
            {
                // Removed ERROR_CODES.txt generation
                initialized = true;
            }
        }
        
        // Регистрация callback функции для асинхронных вызовов
        [DllExport("RVExtensionRegisterCallback", CallingConvention = CallingConvention.Winapi)]
        public static void RvExtensionRegisterCallback(
            [MarshalAs(UnmanagedType.FunctionPtr)] CallbackDelegate func)
        {
            callbackFunc = func;
        }
        
        // Метод для вызова callback из обработчиков
        public static void InvokeCallback(string name, string function, string data)
        {
            if (callbackFunc != null)
            {
                try
                {
                    callbackFunc(name, function, data);
                }
                catch (Exception ex)
                {
                    Logger.WriteLog($"Callback error: {ex.Message}");
                }
            }
        }

        [DllExport("RVExtensionVersion", CallingConvention = CallingConvention.Winapi)]
        public static void RvExtensionVersion(StringBuilder output, int outputSize)
        {
            Initialize();
            output.Append("translator v1.0");
        }

        [DllExport("RVExtension", CallingConvention = CallingConvention.Winapi)]
        public static void RvExtension(StringBuilder output, int outputSize,
           [MarshalAs(UnmanagedType.LPStr)] string function)
        {
            Initialize();
            output.Append("Error: Use RVExtensionArgs for Translate");
        }

        [DllExport("RVExtensionArgs", CallingConvention = CallingConvention.Winapi)]
        public static int RvExtensionArgs(StringBuilder output, int outputSize,
           [MarshalAs(UnmanagedType.LPStr)] string function,
           [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPStr, SizeParamIndex = 4)] string[] args, int argCount)
        {
            Initialize();
            try
            {
                if (string.IsNullOrEmpty(function))
                {
                    output.Append("Error: Function is null");
                    return 0;
                }

                if (args == null)
                {
                    output.Append("Error: Args is null");
                    return 0;
                }

                // Translate handlers
                if (TranslateHandler.CanHandle(function))
                {
                    output.Append(TranslateHandler.Handle(function, args, argCount));
                }
                else
                {
                    output.Append("Error: Unknown function");
                }
            }
            catch (Exception ex)
            {
                output.Append($"Error: {ex.Message}");
            }

            return 0;
        }
    }
}
