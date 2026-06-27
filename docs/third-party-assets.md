# 第三方资源

PearTranslator 不把大型第三方运行时资源直接提交到 Git。

普通用户使用发布包时，打开应用设置界面并点击 **配置模型**。应用会自动下载 OCR 模型、ECDICT 字典和必要的许可证文本到：

```text
%LocalAppData%\PearTranslator\Assets
```

源码开发者 clone 仓库后，也可以运行：

```powershell
powershell -ExecutionPolicy Bypass -File scripts/setup-third-party.ps1
```

脚本会把资源下载到 `third_party/`，目录结构与 WPF 项目文件中的复制规则保持一致。

## 应用内下载目录

```text
%LocalAppData%/
  PearTranslator/
    Assets/
      Licenses/
        PaddleOCR-LICENSE.txt
      Resources/
        ecdict.csv
        ecdict-LICENSE.txt
      models/
        v5/
          korean_PP-OCRv5_rec_mobile.onnx
          ppocrv5_korean_dict.txt
        v6/
          PP-OCRv6_small_det.onnx
          PP-OCRv6_small_rec.onnx
          ppocrv6_dict.txt
```

## 源码脚本目录结构

```text
third_party/
  dictionaries/
    ecdict/
      ecdict.csv
      LICENSE
  ocr/
    rapidocr/
      LICENSE-PaddleOCR.txt
      models/
        v5/
          korean_PP-OCRv5_rec_mobile.onnx
          ppocrv5_korean_dict.txt
        v6/
          PP-OCRv6_small_det.onnx
          PP-OCRv6_small_rec.onnx
          ppocrv6_dict.txt
```

RapidOcrNet 会通过 NuGet 还原/构建输出提供默认的 v5 检测模型、方向分类模型和拉丁识别模型。上面列出的文件是 PearTranslator 当前高精度和混合语言模式额外需要的资源。

## 下载来源

应用内 **配置模型** 和 `scripts/setup-third-party.ps1` 使用同一组上游来源，只是保存目录不同。下表用源码脚本的 `third_party/` 路径表示对应文件。

| 目标文件 | 下载来源 |
| --- | --- |
| `third_party/dictionaries/ecdict/ecdict.csv` | `https://raw.githubusercontent.com/skywind3000/ECDICT/master/ecdict.csv` |
| `third_party/dictionaries/ecdict/LICENSE` | `https://raw.githubusercontent.com/skywind3000/ECDICT/master/LICENSE` |
| `third_party/ocr/rapidocr/LICENSE-PaddleOCR.txt` | `https://raw.githubusercontent.com/PaddlePaddle/PaddleOCR/main/LICENSE` |
| `third_party/ocr/rapidocr/models/v6/PP-OCRv6_small_det.onnx` | `https://huggingface.co/PaddlePaddle/PP-OCRv6_small_det_onnx/resolve/main/inference.onnx` |
| `third_party/ocr/rapidocr/models/v6/PP-OCRv6_small_rec.onnx` | `https://huggingface.co/PaddlePaddle/PP-OCRv6_small_rec_onnx/resolve/main/inference.onnx` |
| `third_party/ocr/rapidocr/models/v6/ppocrv6_dict.txt` | `https://raw.githubusercontent.com/PaddlePaddle/PaddleOCR/main/ppocr/utils/dict/ppocrv6_dict.txt` |
| `third_party/ocr/rapidocr/models/v5/korean_PP-OCRv5_rec_mobile.onnx` | `https://huggingface.co/PaddlePaddle/korean_PP-OCRv5_mobile_rec_onnx/resolve/main/inference.onnx` |
| `third_party/ocr/rapidocr/models/v5/ppocrv5_korean_dict.txt` | `https://raw.githubusercontent.com/PaddlePaddle/PaddleOCR/main/ppocr/utils/dict/ppocrv5_korean_dict.txt` |

## 网络说明

Hugging Face 和 GitHub 在部分网络环境下可能较慢或不可用。如果下载中断，可以重复运行脚本；如果一直失败，可以手动下载上表中的文件并放到对应路径。

## 许可证说明

- ECDICT 使用 MIT 许可证。
- PaddleOCR 代码、字典和 PaddlePaddle 模型仓库使用 Apache-2.0 许可证。
- NuGet 包的许可证不会复制进本仓库；如果要重新分发二进制版本，请检查各包元数据。

本文档只做信息说明，不构成法律建议。
