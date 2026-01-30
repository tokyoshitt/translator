if (!isNil "TRANSLATION_INITIALIZED") exitWith {};
TRANSLATION_INITIALIZED = true;
TRANSLATION_CALLBACKS = createHashMap;
TRANSLATION_REQUEST_COUNTER = 0;

fnc_translate = {
    params ["_text", "_fromLang", "_toLang", "_callback"];
    TRANSLATION_REQUEST_COUNTER = TRANSLATION_REQUEST_COUNTER + 1;
    private _requestId = format["%1_%2_%3", getPlayerUID player, diag_tickTime, TRANSLATION_REQUEST_COUNTER];
    TRANSLATION_CALLBACKS set [_requestId, _callback];
    "translator" callExtension ["Translate", [_text, _fromLang, _toLang, _requestId]];
};

addMissionEventHandler ["ExtensionCallback", {
    params ["_name", "_function", "_data"];
    if (_name == "translator" && _function == "Translate") then {
        private _parts = _data splitString "|";
        if (count _parts == 2) then {
            private _callback = TRANSLATION_CALLBACKS get (_parts select 0);
            if (!isNil "_callback") then {
                [toString parseSimpleArray (_parts select 1)] call _callback;
                TRANSLATION_CALLBACKS deleteAt (_parts select 0);
            };
        };
    };
}];
