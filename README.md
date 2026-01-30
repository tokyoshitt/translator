Arma 3 Translator

## Installation

1. Place `translator.dll` or `translator_x64.dll` next to Arma 3 executable
2. Add to mission `init.sqf`:
```sqf
[] execVM "init_translation.sqf";
```

## Usage

```sqf
"translator" callExtension ["Translate", ["Hello world", "en", "ru"]];
```

## Configuration

Edit `translator/settings.ini`:

```ini
mode=2        # 1=API only, 2=Cache+API, 3=Cache only
provider=1    # 1=MyMemory, 2=Google, 3=DeepL, 4=LibreTranslate
apikey=       # For Google/DeepL
apiurl=       # For LibreTranslate
```
