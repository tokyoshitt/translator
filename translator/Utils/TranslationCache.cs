using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace translator.Utils
{
    public enum TranslationMode
    {
        ApiOnly = 1,        // Только API (асинхронный)
        ApiWithCache = 2,   // API + кэш (сначала кэш, потом API)
        CacheOnly = 3       // Только кэш (без API запросов)
    }
    
    public enum TranslationProvider
    {
        MyMemory = 1,       // MyMemory Translation API (бесплатный)
        GoogleTranslate = 2, // Google Translate API (требует ключ)
        DeepL = 3,          // DeepL API (требует ключ)
        LibreTranslate = 4  // LibreTranslate (требует URL сервера)
    }

    public static class TranslationCache
    {
        private static string cacheDirectory;
        private static string settingsFilePath;
        private static TranslationMode currentMode = TranslationMode.ApiWithCache;
        private static TranslationProvider currentProvider = TranslationProvider.MyMemory;
        private static string apiKey = "";
        private static string customApiUrl = "";
        private static Dictionary<string, Dictionary<string, string>> cache = new Dictionary<string, Dictionary<string, string>>();

        static TranslationCache()
        {
            try
            {
                // Получаем путь к DLL
                string dllPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                string dllDirectory = Path.GetDirectoryName(dllPath);
                
                // Создаем папку translator рядом с DLL
                cacheDirectory = Path.Combine(dllDirectory, "translator");
                settingsFilePath = Path.Combine(cacheDirectory, "settings.ini");
                
                // Создаем папку translator если её нет
                if (!Directory.Exists(cacheDirectory))
                {
                    Directory.CreateDirectory(cacheDirectory);
                    Logger.WriteLog($"Created translator directory: {cacheDirectory}");
                }
                
                // Создаем папку languages
                string languagesDir = Path.Combine(cacheDirectory, "languages");
                if (!Directory.Exists(languagesDir))
                {
                    Directory.CreateDirectory(languagesDir);
                    Logger.WriteLog($"Created languages directory: {languagesDir}");
                }
                
                // Загружаем настройки из settings.ini
                LoadSettings();
            }
            catch (Exception ex)
            {
                Logger.WriteLog($"TranslationCache initialization error: {ex.Message}");
            }
        }
        
        // Загрузить настройки из settings.ini
        private static void LoadSettings()
        {
            try
            {
                if (File.Exists(settingsFilePath))
                {
                    string[] lines = File.ReadAllLines(settingsFilePath, Encoding.UTF8);
                    foreach (string line in lines)
                    {
                        if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#") || line.StartsWith(";"))
                            continue;
                        
                        string[] parts = line.Split(new[] { '=' }, 2);
                        if (parts.Length != 2) continue;
                        
                        string key = parts[0].Trim().ToLower();
                        string value = parts[1].Trim();
                        
                        switch (key)
                        {
                            case "mode":
                                if (int.TryParse(value, out int mode) && mode >= 1 && mode <= 3)
                                {
                                    currentMode = (TranslationMode)mode;
                                    Logger.WriteLog($"Translation mode: {mode}");
                                }
                                break;
                                
                            case "provider":
                                if (int.TryParse(value, out int provider) && provider >= 1 && provider <= 4)
                                {
                                    currentProvider = (TranslationProvider)provider;
                                    Logger.WriteLog($"Translation provider: {provider}");
                                }
                                break;
                                
                            case "apikey":
                                apiKey = value;
                                Logger.WriteLog("API key loaded");
                                break;
                                
                            case "apiurl":
                                customApiUrl = value;
                                Logger.WriteLog($"Custom API URL: {value}");
                                break;
                        }
                    }
                }
                else
                {
                    CreateDefaultSettings();
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog($"Error loading settings: {ex.Message}");
                CreateDefaultSettings();
            }
        }
        
        // Создать settings.ini с настройками по умолчанию
        private static void CreateDefaultSettings()
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("# Translation Settings");
                sb.AppendLine();
                sb.AppendLine("# Mode options:");
                sb.AppendLine("# 1 = ApiOnly - Only use API (async, no cache)");
                sb.AppendLine("# 2 = ApiWithCache - Use cache first, then API if not found (default)");
                sb.AppendLine("# 3 = CacheOnly - Only use cache (no API requests)");
                sb.AppendLine("mode=2");
                sb.AppendLine();
                sb.AppendLine("# Translation Provider:");
                sb.AppendLine("# 1 = MyMemory (free, 5000 chars/day, no key needed)");
                sb.AppendLine("# 2 = Google Translate (requires API key)");
                sb.AppendLine("# 3 = DeepL (requires API key, best quality)");
                sb.AppendLine("# 4 = LibreTranslate (requires custom API URL)");
                sb.AppendLine("provider=1");
                sb.AppendLine();
                sb.AppendLine("# API Key (for Google Translate or DeepL)");
                sb.AppendLine("# Get Google key: https://cloud.google.com/translate");
                sb.AppendLine("# Get DeepL key: https://www.deepl.com/pro-api");
                sb.AppendLine("apikey=");
                sb.AppendLine();
                sb.AppendLine("# Custom API URL (for LibreTranslate or self-hosted)");
                sb.AppendLine("# Example: http://localhost:5000");
                sb.AppendLine("apiurl=");
                
                File.WriteAllText(settingsFilePath, sb.ToString(), Encoding.UTF8);
                Logger.WriteLog("Created default settings.ini");
            }
            catch (Exception ex)
            {
                Logger.WriteLog($"Error creating settings.ini: {ex.Message}");
            }
        }
        
        // Сохранить текущий режим в settings.ini
        private static void SaveSettings()
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("# Translation Settings");
                sb.AppendLine("# Mode options:");
                sb.AppendLine("# 1 = ApiOnly - Only use API (async, no cache)");
                sb.AppendLine("# 2 = ApiWithCache - Use cache first, then API if not found (default)");
                sb.AppendLine("# 3 = CacheOnly - Only use cache (no API requests)");
                sb.AppendLine();
                sb.AppendLine($"mode={((int)currentMode)}");
                
                File.WriteAllText(settingsFilePath, sb.ToString(), Encoding.UTF8);
            }
            catch (Exception ex)
            {
                Logger.WriteLog($"Error saving settings: {ex.Message}");
            }
        }

        public static void SetMode(TranslationMode mode)
        {
            currentMode = mode;
            SaveSettings(); // Сохраняем в settings.ini
        }

        public static TranslationMode GetMode()
        {
            return currentMode;
        }
        
        public static TranslationProvider GetProvider()
        {
            return currentProvider;
        }
        
        public static string GetApiKey()
        {
            return apiKey;
        }
        
        public static string GetCustomApiUrl()
        {
            return customApiUrl;
        }

        // Получить путь к файлу кэша для языковой пары
        private static string GetCacheFilePath(string fromLang, string toLang)
        {
            // Создаем папку для исходного языка если её нет
            string langDir = Path.Combine(cacheDirectory, "languages", fromLang);
            if (!Directory.Exists(langDir))
            {
                try
                {
                    Directory.CreateDirectory(langDir);
                    Logger.WriteLog($"Created language directory: {langDir}");
                }
                catch (Exception ex)
                {
                    Logger.WriteLog($"Error creating language directory: {ex.Message}");
                }
            }
            
            return Path.Combine(langDir, $"{fromLang}_{toLang}.txt");
        }

        // Загрузить кэш из файла
        private static void LoadCache(string fromLang, string toLang)
        {
            string key = $"{fromLang}_{toLang}";
            if (cache.ContainsKey(key))
                return; // Уже загружен

            cache[key] = new Dictionary<string, string>();
            string filePath = GetCacheFilePath(fromLang, toLang);

            if (File.Exists(filePath))
            {
                try
                {
                    string[] lines = File.ReadAllLines(filePath, Encoding.UTF8);
                    foreach (string line in lines)
                    {
                        if (string.IsNullOrWhiteSpace(line))
                            continue;

                        int separatorIndex = line.IndexOf('|');
                        if (separatorIndex > 0 && separatorIndex < line.Length - 1)
                        {
                            string original = line.Substring(0, separatorIndex);
                            string translated = line.Substring(separatorIndex + 1);
                            cache[key][original] = translated;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.WriteLog($"Error loading cache: {ex.Message}");
                }
            }
        }

        // Сохранить перевод в кэш
        public static void SaveTranslation(string fromLang, string toLang, string original, string translated)
        {
            string key = $"{fromLang}_{toLang}";
            LoadCache(fromLang, toLang);

            // Проверяем, есть ли уже такой перевод в памяти
            if (cache[key].ContainsKey(original))
            {
                // Если перевод уже есть и он такой же, не сохраняем
                if (cache[key][original] == translated)
                {
                    return;
                }
                // Если перевод другой, обновляем (перезаписываем весь файл)
                cache[key][original] = translated;
                SaveCacheToFile(fromLang, toLang);
            }
            else
            {
                // Добавляем новый перевод в память
                cache[key][original] = translated;

                // Добавляем в файл (append)
                try
                {
                    string filePath = GetCacheFilePath(fromLang, toLang);
                    using (StreamWriter writer = new StreamWriter(filePath, true, Encoding.UTF8))
                    {
                        writer.WriteLine($"{original}|{translated}");
                    }
                }
                catch (Exception ex)
                {
                    Logger.WriteLog($"Error saving cache: {ex.Message}");
                }
            }
        }
        
        // Сохранить весь кэш в файл (перезапись)
        private static void SaveCacheToFile(string fromLang, string toLang)
        {
            string key = $"{fromLang}_{toLang}";
            if (!cache.ContainsKey(key))
                return;

            try
            {
                string filePath = GetCacheFilePath(fromLang, toLang);
                using (StreamWriter writer = new StreamWriter(filePath, false, Encoding.UTF8))
                {
                    foreach (var pair in cache[key])
                    {
                        writer.WriteLine($"{pair.Key}|{pair.Value}");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog($"Error saving cache file: {ex.Message}");
            }
        }

        // Получить перевод из кэша
        public static string GetTranslation(string fromLang, string toLang, string original)
        {
            string key = $"{fromLang}_{toLang}";
            LoadCache(fromLang, toLang);

            if (cache.ContainsKey(key) && cache[key].ContainsKey(original))
            {
                return cache[key][original];
            }

            return null;
        }

        // Проверить, нужно ли использовать API
        public static bool ShouldUseApi()
        {
            return currentMode == TranslationMode.ApiOnly || currentMode == TranslationMode.ApiWithCache;
        }

        // Проверить, нужно ли использовать кэш
        public static bool ShouldUseCache()
        {
            return currentMode == TranslationMode.ApiWithCache || currentMode == TranslationMode.CacheOnly;
        }
        
        // Получить информацию о путях (для отладки)
        public static string GetInfo()
        {
            try
            {
                string dllPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"DLL Path: {dllPath}");
                sb.AppendLine($"DLL Directory: {Path.GetDirectoryName(dllPath)}");
                sb.AppendLine($"Cache Directory: {cacheDirectory}");
                sb.AppendLine($"Settings File: {settingsFilePath}");
                sb.AppendLine($"Current Mode: {currentMode} ({(int)currentMode})");
                sb.AppendLine($"Directory Exists: {Directory.Exists(cacheDirectory)}");
                return sb.ToString();
            }
            catch (Exception ex)
            {
                return $"Error getting info: {ex.Message}";
            }
        }
    }
}
