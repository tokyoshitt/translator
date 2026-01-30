using System;
using System.Net;
using System.Text;

namespace translator_x64.Utils
{
    public static class TranslationProviders
    {
        // Построить URL для запроса перевода
        public static string BuildTranslationUrl(string text, string fromLang, string toLang)
        {
            string encodedText = Uri.EscapeDataString(text);
            TranslationProvider provider = TranslationCache.GetProvider();
            string apiKey = TranslationCache.GetApiKey();
            string customUrl = TranslationCache.GetCustomApiUrl();

            switch (provider)
            {
                case TranslationProvider.MyMemory:
                    // MyMemory API - бесплатный
                    string url = $"https://api.mymemory.translated.net/get?q={encodedText}&langpair={fromLang}|{toLang}";
                    if (!string.IsNullOrEmpty(apiKey))
                    {
                        url += $"&key={apiKey}";
                    }
                    return url;

                case TranslationProvider.GoogleTranslate:
                    // Google Translate API
                    if (string.IsNullOrEmpty(apiKey))
                    {
                        throw new Exception("Google Translate requires API key in settings.ini");
                    }
                    return $"https://translation.googleapis.com/language/translate/v2?key={apiKey}&q={encodedText}&source={fromLang}&target={toLang}";

                case TranslationProvider.DeepL:
                    // DeepL API
                    if (string.IsNullOrEmpty(apiKey))
                    {
                        throw new Exception("DeepL requires API key in settings.ini");
                    }
                    // DeepL использует разные URL для free и pro ключей
                    string deeplUrl = apiKey.EndsWith(":fx") 
                        ? "https://api-free.deepl.com/v2/translate" 
                        : "https://api.deepl.com/v2/translate";
                    return $"{deeplUrl}?auth_key={apiKey}&text={encodedText}&source_lang={fromLang.ToUpper()}&target_lang={toLang.ToUpper()}";

                case TranslationProvider.LibreTranslate:
                    // LibreTranslate (self-hosted или публичный)
                    if (string.IsNullOrEmpty(customUrl))
                    {
                        throw new Exception("LibreTranslate requires apiurl in settings.ini");
                    }
                    return $"{customUrl}/translate";

                default:
                    throw new Exception($"Unknown translation provider: {provider}");
            }
        }

        // Парсинг ответа от API
        public static string ParseTranslationResponse(string response, TranslationProvider provider)
        {
            try
            {
                switch (provider)
                {
                    case TranslationProvider.MyMemory:
                        return ParseMyMemoryResponse(response);

                    case TranslationProvider.GoogleTranslate:
                        return ParseGoogleTranslateResponse(response);

                    case TranslationProvider.DeepL:
                        return ParseDeepLResponse(response);

                    case TranslationProvider.LibreTranslate:
                        return ParseLibreTranslateResponse(response);

                    default:
                        return null;
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog($"Parse error: {ex.Message}");
                return null;
            }
        }

        private static string ParseMyMemoryResponse(string response)
        {
            int startIndex = response.IndexOf("\"translatedText\":\"");
            if (startIndex > 0)
            {
                startIndex += 18;
                int endIndex = response.IndexOf("\"", startIndex);
                if (endIndex > startIndex)
                {
                    return response.Substring(startIndex, endIndex - startIndex);
                }
            }
            return null;
        }

        private static string ParseGoogleTranslateResponse(string response)
        {
            // Google: {"data":{"translations":[{"translatedText":"Hello"}]}}
            int startIndex = response.IndexOf("\"translatedText\":\"");
            if (startIndex > 0)
            {
                startIndex += 18;
                int endIndex = response.IndexOf("\"", startIndex);
                if (endIndex > startIndex)
                {
                    return response.Substring(startIndex, endIndex - startIndex);
                }
            }
            return null;
        }

        private static string ParseDeepLResponse(string response)
        {
            // DeepL: {"translations":[{"detected_source_language":"EN","text":"Hallo"}]}
            int startIndex = response.IndexOf("\"text\":\"");
            if (startIndex > 0)
            {
                startIndex += 8;
                int endIndex = response.IndexOf("\"", startIndex);
                if (endIndex > startIndex)
                {
                    return response.Substring(startIndex, endIndex - startIndex);
                }
            }
            return null;
        }

        private static string ParseLibreTranslateResponse(string response)
        {
            // LibreTranslate: {"translatedText":"Hello"}
            int startIndex = response.IndexOf("\"translatedText\":\"");
            if (startIndex > 0)
            {
                startIndex += 18;
                int endIndex = response.IndexOf("\"", startIndex);
                if (endIndex > startIndex)
                {
                    return response.Substring(startIndex, endIndex - startIndex);
                }
            }
            return null;
        }

        // Нужен ли POST запрос для этого провайдера
        public static bool RequiresPost(TranslationProvider provider)
        {
            return provider == TranslationProvider.LibreTranslate;
        }

        // Построить POST данные
        public static string BuildPostData(string text, string fromLang, string toLang, TranslationProvider provider)
        {
            if (provider == TranslationProvider.LibreTranslate)
            {
                string apiKey = TranslationCache.GetApiKey();
                string json = "{" +
                    "\"q\":\"" + StringUtils.EscapeJson(text) + "\"," +
                    "\"source\":\"" + fromLang + "\"," +
                    "\"target\":\"" + toLang + "\"";
                
                if (!string.IsNullOrEmpty(apiKey))
                {
                    json += ",\"api_key\":\"" + apiKey + "\"";
                }
                
                json += "}";
                return json;
            }
            return null;
        }
    }
}
