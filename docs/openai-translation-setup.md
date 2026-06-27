# OpenAI Translation Setup

PearTranslator uses the mock translation provider by default. To enable real
OpenAI translation, set an API key before launching the WPF app.

## In-App Settings

Open PearTranslator and use the provider settings panel:

- Provider: choose `OpenAI / Compatible LLM`.
- API Key: paste your OpenAI-compatible key.
- Model: choose a preset model or `Custom...`.
- Base URL: keep `https://api.openai.com/v1/` for OpenAI, or use a compatible gateway URL.

Settings are saved to `%AppData%\PearTranslator\settings.json`.

The app also supports Azure Translator, DeepL, and Google Cloud Translation
Basic through the provider dropdown. If a selected provider is missing its key,
the app uses OCR preview instead of sending translation requests.

Traditional provider notes:

- Azure Translator: enter the subscription key and region such as `eastasia`.
  Leave endpoint blank to use `https://api.cognitive.microsofttranslator.com/`.
- DeepL: enter the auth key. Leave endpoint blank for
  `https://api-free.deepl.com/v2/`, or use the Pro endpoint when needed.
- Google Cloud Translation Basic: enter an API key. Leave endpoint blank for
  `https://translation.googleapis.com/language/translate/v2`.

## Environment Variables

Required:

```powershell
$env:OPENAI_API_KEY = "sk-..."
```

PearTranslator-specific override:

```powershell
$env:PEAR_TRANSLATOR_OPENAI_API_KEY = "sk-..."
```

Optional model override:

```powershell
$env:PEAR_TRANSLATOR_OPENAI_MODEL = "gpt-5.4-mini"
```

Optional base URI override for compatible gateways:

```powershell
$env:PEAR_TRANSLATOR_OPENAI_BASE_URI = "https://api.openai.com/v1/"
```

## Run

```powershell
dotnet run --project src/PearTranslator.App.Wpf/PearTranslator.App.Wpf.csproj
```

In-app settings take priority over environment variables. If no translation
provider is configured, the app uses OCR preview and shows recognized English
text without fake translation.
