# PearTranslator

[简体中文](README.md) | English

PearTranslator is a Windows desktop translator for game subtitles and screen text. It captures a selected screen region, runs OCR locally, and shows the translated result in a lightweight overlay.

The current product direction is Windows-first: good-looking WPF UI, low-latency capture, local high-accuracy OCR, and OpenAI-compatible translation providers.

## Preview

### Settings

![PearTranslator settings panel](docs/images/peartranslator-settings.png)

### Realtime Game Overlay

![PearTranslator realtime game overlay](docs/images/peartranslator-game-overlay.jpg)

### Bilingual Overlay

![PearTranslator bilingual overlay](docs/images/peartranslator-bilingual-overlay.jpg)

## Supported Environment

Supported:

- Windows 10 2004 or later, build 19041+
- Windows 11
- x64 Windows is recommended
- .NET 8 SDK for development

Not supported:

- macOS
- Linux
- mobile platforms

PearTranslator uses WPF, Windows desktop APIs, global hotkeys, tray integration, Windows Graphics Capture, and Windows-specific overlay behavior. Those parts do not run on macOS or Linux.

## Features

- Realtime region translation for games, browsers, and desktop windows.
- One-shot screenshot translation that can coexist with realtime overlay.
- Local OCR through RapidOcrNet and PaddleOCR ONNX models.
- OCR language modes for auto detection, English, Chinese, Japanese, and Korean.
- Target language filtering for simplified Chinese or English.
- OpenAI-compatible LLM providers, including OpenAI, DeepSeek, Qwen, Kimi, Zhipu, Doubao, and custom endpoints.
- Traditional translation providers, including Azure Translator, DeepL, and Google Cloud Translation Basic.
- Optional local first-word preview before the model response returns.
- Click-through overlay, selected-region marker toggle, and capture-exclusion support.

## Quick Start

For normal use, unzip the Windows release package and start `PearTranslator.App.Wpf.exe`. On first launch, click **配置模型** in the settings UI. The app downloads OCR models, the ECDICT dictionary, and required third-party license texts into:

```text
%LocalAppData%\PearTranslator\Assets
```

After the download finishes, region selection and translation can be used directly. Normal users do not need the .NET SDK or a setup script.

For source development, install the .NET 8 SDK, then clone the repository and run:

```powershell
powershell -ExecutionPolicy Bypass -File scripts/setup-third-party.ps1
dotnet restore PearTranslator.sln
dotnet run --project src/PearTranslator.App.Wpf/PearTranslator.App.Wpf.csproj
```

The setup script downloads third-party OCR models and dictionary data into `third_party/`. These files are intentionally not committed to Git because they are large and have their own licenses.

## Hotkeys

- `Ctrl+Alt+R`: select realtime region
- `Ctrl+Alt+T`: pause or resume realtime translation
- `Ctrl+Alt+X`: hide current realtime overlay
- `Ctrl+Alt+S`: one-shot screenshot translation
- `Esc`: cancel region selection

## Translation Settings

Open the app settings panel and choose a provider.

For OpenAI-compatible providers:

- Choose the model platform.
- Enter an API key.
- Pick a preset model or choose a custom model.
- For custom endpoints, enter a compatible base URL.

Settings are saved under:

```text
%AppData%\PearTranslator\settings.json
```

Do not commit that file. It may contain API keys.

The app can also read these environment variables:

```powershell
$env:OPENAI_API_KEY = "sk-..."
$env:PEAR_TRANSLATOR_OPENAI_API_KEY = "sk-..."
$env:PEAR_TRANSLATOR_OPENAI_MODEL = "gpt-5.4-mini"
$env:PEAR_TRANSLATOR_OPENAI_BASE_URI = "https://api.openai.com/v1/"
```

More details are in [docs/openai-translation-setup.md](docs/openai-translation-setup.md).

## Build

```powershell
dotnet build PearTranslator.sln
```

## Test

```powershell
dotnet test PearTranslator.sln
```

## Publish Windows Build

```powershell
dotnet publish src/PearTranslator.App.Wpf/PearTranslator.App.Wpf.csproj `
  -c Release `
  -r win-x64 `
  --self-contained true `
  -o artifacts/PearTranslator-win-x64
```

The publish output is ignored by Git. Zip the `artifacts/PearTranslator-win-x64` folder if you want to move it to another Windows computer.

## Third-Party Assets

This repository does not commit large OCR model files or ECDICT CSV data. Release users can click **配置模型** in the app to download runtime assets into `%LocalAppData%\PearTranslator\Assets`.

Source developers can also run:

```powershell
powershell -ExecutionPolicy Bypass -File scripts/setup-third-party.ps1
```

See [docs/third-party-assets.md](docs/third-party-assets.md) and [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md) for sources and license notes.

## License

PearTranslator source code is released under the MIT License. Third-party models, dictionaries, packages, and APIs remain under their own licenses and terms.
