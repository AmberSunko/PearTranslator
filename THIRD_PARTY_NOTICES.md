# Third-Party Notices

中文摘要：PearTranslator 源码使用 MIT License，但第三方模型、字典、NuGet 包和翻译 API 不属于 PearTranslator 授权范围。公开发布源码或二进制包时，请保留本文件以及上游许可证文件。

PearTranslator source code is licensed under the MIT License. Third-party assets and dependencies are not relicensed by this project.

## Runtime Assets Downloaded by the App or Setup Script

The following files are downloaded by the in-app **配置模型** action or by `scripts/setup-third-party.ps1`. They are intentionally ignored by Git:

| Asset | Source | License |
| --- | --- | --- |
| ECDICT `ecdict.csv` | https://github.com/skywind3000/ECDICT | MIT |
| ECDICT `LICENSE` | https://github.com/skywind3000/ECDICT | MIT |
| PaddleOCR `LICENSE` | https://github.com/PaddlePaddle/PaddleOCR | Apache-2.0 |
| PP-OCRv6 small detection ONNX model | https://huggingface.co/PaddlePaddle/PP-OCRv6_small_det_onnx | Apache-2.0 |
| PP-OCRv6 small recognition ONNX model | https://huggingface.co/PaddlePaddle/PP-OCRv6_small_rec_onnx | Apache-2.0 |
| PP-OCRv6 dictionary | https://github.com/PaddlePaddle/PaddleOCR | Apache-2.0 |
| Korean PP-OCRv5 recognition ONNX model | https://huggingface.co/PaddlePaddle/korean_PP-OCRv5_mobile_rec_onnx | Apache-2.0 |
| Korean PP-OCRv5 dictionary | https://github.com/PaddlePaddle/PaddleOCR | Apache-2.0 |

PaddleOCR states that PP-OCRv6 is a multilingual OCR model family and that the small tier is designed for edge/mobile use. The model repositories listed above are maintained by PaddlePaddle and marked with Apache-2.0 license metadata.

## NuGet Dependencies

NuGet dependencies are restored by `dotnet restore`. Important runtime packages include:

- RapidOcrNet
- Vortice.Direct3D11
- Microsoft Windows SDK bindings
- ONNX Runtime dependencies pulled by OCR packages
- SkiaSharp dependencies pulled by OCR packages

Review each NuGet package license before redistributing binary builds.

## Translation APIs

PearTranslator can connect to OpenAI-compatible APIs, DeepL, Azure Translator, and Google Cloud Translation Basic. API providers have separate terms, pricing, rate limits, and data processing policies. Users are responsible for configuring their own API keys and complying with provider terms.

## Redistribution Note

If you publish a binary package that includes downloaded OCR models or dictionary data, include this notice and the upstream license texts. Do not imply that third-party models, dictionaries, or APIs are owned by PearTranslator.
