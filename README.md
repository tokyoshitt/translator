## Installation

1. Place `translator.dll` or `translator_x64.dll` next to Arma 3 executable
2. Add to mission `init.sqf`:
```sqf
[] execVM "init_translation.sqf";
```

## Usage

```sqf
["Hello world", "en", "ru", {
    params ["_translated"];
    systemChat _translated;
}] call fnc_translate;
```

## Configuration

Edit `translator/settings.ini`:

```ini
mode=2        # 1=API only, 2=Cache+API, 3=Cache only
provider=1    # 1=MyMemory, 2=Google, 3=DeepL, 4=LibreTranslate
apikey=       # For Google/DeepL
apiurl=       # For LibreTranslate
```
