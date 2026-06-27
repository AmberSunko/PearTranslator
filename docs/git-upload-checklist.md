# Git 上传检查清单

公开开源 PearTranslator 前，先按这份清单检查一遍。

## 仓库内容

- 源码放在 `src/`。
- 测试放在 `tests/`。
- 开发工具放在 `tools/`。
- 文档放在 `docs/`。
- 不提交 `bin/`、`obj/`、`.vs/`、`.idea/`、`.vscode/`、`artifacts/`、发布 zip、本地设置文件。
- 不提交 API Key 或 `%AppData%\PearTranslator\settings.json`。
- 不提交 `third_party/` 中下载的 OCR 模型和大型字典 CSV 文件。

## 第三方资源

二进制发布包用户应通过应用设置里的 **配置模型** 按钮下载运行时资源。它会把 OCR 模型、ECDICT 字典和必要许可证文本放到：

```text
%LocalAppData%\PearTranslator\Assets
```

源码开发者 clone 仓库后，可以运行：

```powershell
powershell -ExecutionPolicy Bypass -File scripts/setup-third-party.ps1
```

如果二进制发布包包含下载后的模型或字典，请一起附带 `THIRD_PARTY_NOTICES.md` 和上游许可证文件。

## 支持环境

文档中要明确写出当前支持环境：

- Windows 10 2004 / build 19041 或更高版本
- Windows 11
- 推荐 x64
- 开发环境需要 .NET 8 SDK

当前暂不支持 macOS 和 Linux，因为应用依赖 WPF 以及 Windows 桌面捕获/overlay API。

## 验证

上传前运行：

```powershell
dotnet restore PearTranslator.sln
dotnet build PearTranslator.sln
dotnet test PearTranslator.sln
```

如果已安装 Git：

```powershell
git status --short
git check-ignore -v artifacts/PearTranslator-win-x64.zip
git check-ignore -v third_party/ocr/rapidocr/models/v6/PP-OCRv6_small_rec.onnx
git check-ignore -v third_party/dictionaries/ecdict/ecdict.csv
```

第一次公开 push 前，认真检查 staged 文件列表。
