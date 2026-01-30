["Привет мой друг", "ru", "en", {
    params ["_translated"];
    systemChat _translated;
}] call fnc_translate;
