// Серверная версия с polling (для dedicated server)
if (!isNil "TRANSLATION_INITIALIZED") exitWith {};
TRANSLATION_INITIALIZED = true;
TRANSLATION_CALLBACKS = createHashMap;
TRANSLATION_REQUEST_COUNTER = 0;
TRANSLATION_PENDING_REQUESTS = createHashMap;

fnc_translate = {
    params ["_text", "_fromLang", "_toLang", "_callback"];
    TRANSLATION_REQUEST_COUNTER = TRANSLATION_REQUEST_COUNTER + 1;
    private _requestId = format["srv_%1_%2", diag_tickTime, TRANSLATION_REQUEST_COUNTER];
    TRANSLATION_CALLBACKS set [_requestId, _callback];
    
    private _result = "translator" callExtension ["Translate", [_text, _fromLang, _toLang, _requestId]];
    
    if (_result != "OK") then {
        // Результат из кэша сразу (режим CacheOnly)
        [_requestId, _result] call fnc_handleTranslationResult;
    } else {
        // Запускаем polling для асинхронного результата
        TRANSLATION_PENDING_REQUESTS set [_requestId, diag_tickTime];
        [_requestId] call fnc_pollTranslationResult;
    };
};

fnc_pollTranslationResult = {
    params ["_requestId"];
    [{
        params ["_requestId"];
        private _result = "translator" callExtension ["GetResult", [_requestId]];
        
        if (_result == "PENDING") then {
            // Проверяем таймаут (10 секунд)
            private _startTime = TRANSLATION_PENDING_REQUESTS get _requestId;
            if (diag_tickTime - _startTime > 10) then {
                // Таймаут - вызываем callback с пустым результатом
                TRANSLATION_PENDING_REQUESTS deleteAt _requestId;
                private _callback = TRANSLATION_CALLBACKS get _requestId;
                TRANSLATION_CALLBACKS deleteAt _requestId;
                if (!isNil "_callback") then {
                    [""] call _callback;
                };
            } else {
                // Продолжаем проверять через 0.3 сек
                [fnc_pollTranslationResult, [_requestId], 0.3] call CBA_fnc_waitAndExecute;
            };
        } else {
            // Результат готов
            TRANSLATION_PENDING_REQUESTS deleteAt _requestId;
            [_requestId, _result] call fnc_handleTranslationResult;
        };
    }, [_requestId], 0.3] call CBA_fnc_waitAndExecute;
};

fnc_handleTranslationResult = {
    params ["_requestId", "_result"];
    private _callback = TRANSLATION_CALLBACKS get _requestId;
    TRANSLATION_CALLBACKS deleteAt _requestId;
    
    if (!isNil "_callback") then {
        private _parts = _result splitString "|";
        private _translatedText = if (count _parts == 2) then {
            toString parseSimpleArray (_parts select 1)
        } else {
            toString parseSimpleArray _result
        };
        [_translatedText] call _callback;
    };
};

// Очистка старых запросов каждые 60 секунд
[{
    private _currentTime = diag_tickTime;
    {
        if (_currentTime - _y > 30) then {
            TRANSLATION_PENDING_REQUESTS deleteAt _x;
            TRANSLATION_CALLBACKS deleteAt _x;
        };
    } forEach TRANSLATION_PENDING_REQUESTS;
}, [], 60] call CBA_fnc_addPerFrameHandler;
