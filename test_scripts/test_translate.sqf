// Пример использования системы перевода
// Запускать ПОСЛЕ init_translation.sqf

// Проверяем что система инициализирована
if (isNil "TRANSLATION_INITIALIZED") then {
    systemChat "ERROR: Run init_translation.sqf first!";
} else {
    // Просто вызываем перевод - результат придет через callback
    "translator" callExtension ["Translate", ["Привет мой друг", "ru", "en"]];
    
    // Результат автоматически появится в чате через ExtensionCallback
    // Или можно использовать переменную LAST_TRANSLATION
};
