using System;
using System.IO;
using System.Net;
using System.Text;
using System.Linq;
using System.Web;
using translator_x64.Utils;

namespace translator_x64.Handlers
{
    public static class TranslateHandler
    {
        public static bool CanHandle(string function)
        {
            return function == "Translate" || function == "SetTranslateMode" || function == "GetCacheInfo";
        }

        public static string Handle(string function, string[] args, int argCount)
        {
            if (function == "Translate")
            {
                if (argCount >= 3)
                {
                    string requestId = argCount >= 4 ? args[3] : "";
                    return Translate(args[0], args[1], args[2], requestId);
                }
                return "Error: Translate requires 3 arguments [text, fromLang, toLang] and optional [requestId]";
            }
            
            if (function == "SetTranslateMode")
            {
                if (argCount >= 1)
                {
                    return SetTranslateMode(args[0]);
                }
                return "Error: SetTranslateMode requires 1 argument [mode: 1=ApiOnly, 2=ApiWithCache, 3=CacheOnly]";
            }
            
            if (function == "GetCacheInfo")
            {
                return TranslationCache.GetInfo();
            }
            
            return "Unknown function";
        }

        private static string SetTranslateMode(string modeStr)
        {
            try
            {
                modeStr = StringUtils.CleanParameter(modeStr);
                int mode = int.Parse(modeStr);
                
                if (mode < 1 || mode > 3)
                {
                    return "Error: Mode must be 1, 2, or 3";
                }
                
                TranslationCache.SetMode((TranslationMode)mode);
                return $"OK: Mode set to {mode}";
            }
            catch (Exception ex)
            {
                return "ERROR: " + ex.Message;
            }
        }

        private static string Translate(string text, string fromLang, string toLang, string requestId)
        {
            try
            {
                text = StringUtils.CleanParameter(text);
                fromLang = StringUtils.CleanParameter(fromLang);
                toLang = StringUtils.CleanParameter(toLang);
                requestId = StringUtils.CleanParameter(requestId);
                
                string originalText = text; // Сохраняем оригинал для возврата при ошибке

                // Проверяем кэш если режим позволяет
                if (TranslationCache.ShouldUseCache())
                {
                    string cachedTranslation = TranslationCache.GetTranslation(fromLang, toLang, text);
                    if (cachedTranslation != null)
                    {
                        // Убираем ВСЕ управляющие символы и пробелы по краям
                        cachedTranslation = cachedTranslation.Replace("\r", "").Replace("\n", "").Replace("\t", "").Trim();
                        
                        // Конвертируем в массив Unicode кодов для SQF
                        int[] unicodeCodes = cachedTranslation.Select(c => (int)c).ToArray();
                        string arrayStr = "[" + string.Join(",", unicodeCodes) + "]";
                        
                        // Добавляем requestId к ответу
                        string response = string.IsNullOrEmpty(requestId) ? arrayStr : requestId + "|" + arrayStr;
                        
                        // Режим 3: Только кэш - возвращаем сразу (НЕ через callback!)
                        if (TranslationCache.GetMode() == TranslationMode.CacheOnly)
                        {
                            return response;
                        }
                        
                        // Режим 2: Найдено в кэше - возвращаем через callback
                        DllEntry.InvokeCallback("translator", "Translate", response);
                        return "OK";
                    }
                    
                    // Если режим только кэш и перевода нет - возвращаем оригинал
                    if (TranslationCache.GetMode() == TranslationMode.CacheOnly)
                    {
                        return originalText;
                    }
                }

                // Если дошли сюда, используем API
                if (!TranslationCache.ShouldUseApi())
                {
                    return originalText; // Возвращаем оригинал если API отключен
                }

                // Получаем текущий провайдер
                TranslationProvider provider = TranslationCache.GetProvider();
                
                try
                {
                    // Строим URL для запроса
                    string url = TranslationProviders.BuildTranslationUrl(text, fromLang, toLang);
                    
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                    // Проверяем нужен ли POST запрос
                    if (TranslationProviders.RequiresPost(provider))
                    {
                        // POST запрос (для LibreTranslate)
                        WebClient client = new WebClient();
                        client.Encoding = Encoding.UTF8;
                        client.Headers[HttpRequestHeader.ContentType] = "application/json";
                        
                        string postData = TranslationProviders.BuildPostData(text, fromLang, toLang, provider);
                        
                        client.UploadStringCompleted += (sender, e) => {
                            HandleApiResponse(e.Error, e.Cancelled, e.Result, provider, fromLang, toLang, originalText, requestId);
                            client.Dispose();
                        };
                        
                        client.UploadStringAsync(new Uri(url), "POST", postData);
                    }
                    else
                    {
                        // GET запрос (для MyMemory, Google, DeepL)
                        WebClient client = new WebClient();
                        client.Encoding = Encoding.UTF8;
                        
                        client.DownloadStringCompleted += (sender, e) => {
                            HandleApiResponse(e.Error, e.Cancelled, e.Result, provider, fromLang, toLang, originalText, requestId);
                            client.Dispose();
                        };
                        
                        client.DownloadStringAsync(new Uri(url));
                    }
                    
                    return "OK"; // Возвращаем сразу, результат придет через callback
                }
                catch (Exception ex)
                {
                    Logger.WriteLog($"Translation API error: {ex.Message}");
                    return originalText; // При ошибке возвращаем оригинал
                }
            }
            catch (Exception ex)
            {
                return text; // При любой ошибке возвращаем оригинал
            }
        }
        
        // Обработка ответа от API (общий метод для GET и POST)
        private static void HandleApiResponse(Exception error, bool cancelled, string response, 
            TranslationProvider provider, string fromLang, string toLang, string originalText, string requestId)
        {
            try
            {
                if (error == null && !cancelled && !string.IsNullOrEmpty(response))
                {
                    // Парсим ответ используя TranslationProviders
                    string translated = TranslationProviders.ParseTranslationResponse(response, provider);
                    
                    if (!string.IsNullOrEmpty(translated))
                    {
                        // Декодируем unicode escape sequences
                        translated = System.Text.RegularExpressions.Regex.Unescape(translated);
                        // Убираем ВСЕ управляющие символы
                        translated = translated.Replace("\r", "").Replace("\n", "").Replace("\t", "").Trim();
                        
                        // Проверяем что перевод не пустой
                        if (!string.IsNullOrWhiteSpace(translated))
                        {
                            // Сохраняем в кэш
                            TranslationCache.SaveTranslation(fromLang, toLang, originalText, translated);
                            
                            // Конвертируем в массив Unicode кодов для SQF
                            int[] unicodeCodes = translated.Select(c => (int)c).ToArray();
                            string arrayStr = "[" + string.Join(",", unicodeCodes) + "]";
                            
                            // Добавляем requestId к ответу
                            string result = string.IsNullOrEmpty(requestId) ? arrayStr : requestId + "|" + arrayStr;
                            
                            // Вызываем callback с результатом
                            DllEntry.InvokeCallback("translator", "Translate", result);
                            return;
                        }
                    }
                }
                
                // Если что-то пошло не так - возвращаем оригинал
                Logger.WriteLog($"Translation failed, returning original text");
                string fallback = string.IsNullOrEmpty(requestId) ? originalText : requestId + "|" + originalText;
                DllEntry.InvokeCallback("translator", "Translate", fallback);
            }
            catch (Exception ex)
            {
                Logger.WriteLog($"HandleApiResponse error: {ex.Message}");
                // При ошибке возвращаем оригинал
                string fallback = string.IsNullOrEmpty(requestId) ? originalText : requestId + "|" + originalText;
                DllEntry.InvokeCallback("translator", "Translate", fallback);
            }
        }
    }
}
