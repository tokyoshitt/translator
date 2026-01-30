// Инициализация системы перевода
// Запускать ОДИН РАЗ при старте миссии!
// Например в init.sqf: [] execVM "init_translation.sqf";

// Проверяем, не инициализирован ли уже
if (!isNil "TRANSLATION_INITIALIZED") exitWith {
    systemChat "Translation system already initialized";
};

TRANSLATION_INITIALIZED = true;

// Функция декодирования Unicode escape sequences
fnc_decodeUnicode = {
    params ["_encoded"];
    private _result = "";
    private _i = 0;
    private _len = count _encoded;
    
    while {_i < _len} do {
        private _char = _encoded select [_i, 1];
        if (_char == "\") then {
            private _next = _encoded select [_i + 1, 1];
            if (_next == "u") then {
                // Декодируем \uXXXX
                private _hex = _encoded select [_i + 2, 4];
                private _hexChars = "0123456789ABCDEF";
                private _code = 0;
                
                for "_j" from 0 to 3 do {
                    private _c = toUpper (_hex select [_j, 1]);
                    private _value = _hexChars find _c;
                    if (_value >= 0) then {
                        _code = (_code * 16) + _value;
                    };
                };
                
                _result = _result + toString [_code];
                _i = _i + 6;
            } else {
                _result = _result + _char;
                _i = _i + 1;
            };
        } else {
            _result = _result + _char;
            _i = _i + 1;
        };
    };
    _result
};

// Регистрируем обработчик ExtensionCallback ОДИН РАЗ
addMissionEventHandler ["ExtensionCallback", {
    params ["_name", "_function", "_data"];
    
    // Обрабатываем только наши переводы
    if (_name == "translator" && _function == "Translate") then {
        // Декодируем Unicode escape sequences
        private _decoded = [_data] call fnc_decodeUnicode;
        
        // Показываем переведенное сообщение
        systemChat format["Перевод: %1", _decoded];
        
        // Сохраняем в глобальную переменную
        LAST_TRANSLATION = _decoded;
    };
}];

systemChat "Translation system initialized";
