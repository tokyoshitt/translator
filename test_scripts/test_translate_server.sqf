// Тест серверного перевода
["Привет мой друг", "ru", "en", {
    params ["_translated"];
    diag_log format["Translation result: %1", _translated];
    systemChat _translated;
}] call fnc_translate;

// Тест множественных переводов
["Hello", "en", "ru", {
    params ["_translated"];
    diag_log format["EN->RU: %1", _translated];
}] call fnc_translate;

["Bonjour", "fr", "en", {
    params ["_translated"];
    diag_log format["FR->EN: %1", _translated];
}] call fnc_translate;
