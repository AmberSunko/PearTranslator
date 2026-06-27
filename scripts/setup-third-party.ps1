param(
    [switch]$Force,
    [switch]$SkipEcdict
)

$ErrorActionPreference = "Stop"

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent $scriptRoot

function Join-RepoPath {
    param([Parameter(Mandatory = $true)][string[]]$Parts)

    $path = $repoRoot
    foreach ($part in $Parts) {
        $path = Join-Path $path $part
    }

    return $path
}

function Ensure-Directory {
    param([Parameter(Mandatory = $true)][string]$Path)

    if (-not (Test-Path -LiteralPath $Path)) {
        New-Item -ItemType Directory -Path $Path | Out-Null
    }
}

function Download-Asset {
    param(
        [Parameter(Mandatory = $true)][string]$Name,
        [Parameter(Mandatory = $true)][string]$Uri,
        [Parameter(Mandatory = $true)][string]$Destination,
        [Parameter(Mandatory = $true)][long]$MinimumBytes
    )

    $destinationDirectory = Split-Path -Parent $Destination
    Ensure-Directory -Path $destinationDirectory

    if ((Test-Path -LiteralPath $Destination) -and -not $Force) {
        $existing = Get-Item -LiteralPath $Destination
        if ($existing.Length -ge $MinimumBytes) {
            Write-Host "skip  $Name"
            return
        }
    }

    $temporary = "$Destination.download"
    if (Test-Path -LiteralPath $temporary) {
        Remove-Item -LiteralPath $temporary -Force
    }

    Write-Host "down  $Name"
    Invoke-WebRequest -Uri $Uri -OutFile $temporary -UseBasicParsing

    $downloaded = Get-Item -LiteralPath $temporary
    if ($downloaded.Length -lt $MinimumBytes) {
        Remove-Item -LiteralPath $temporary -Force
        throw "Downloaded file for '$Name' is smaller than expected. URL may be unavailable: $Uri"
    }

    Move-Item -LiteralPath $temporary -Destination $Destination -Force
}

$ecdictDirectory = Join-RepoPath -Parts @("third_party", "dictionaries", "ecdict")
$rapidOcrDirectory = Join-RepoPath -Parts @("third_party", "ocr", "rapidocr")
$ocrV5Directory = Join-RepoPath -Parts @("third_party", "ocr", "rapidocr", "models", "v5")
$ocrV6Directory = Join-RepoPath -Parts @("third_party", "ocr", "rapidocr", "models", "v6")

Ensure-Directory -Path $ecdictDirectory
Ensure-Directory -Path $rapidOcrDirectory
Ensure-Directory -Path $ocrV5Directory
Ensure-Directory -Path $ocrV6Directory

if (-not $SkipEcdict) {
    Download-Asset `
        -Name "ECDICT csv" `
        -Uri "https://raw.githubusercontent.com/skywind3000/ECDICT/master/ecdict.csv" `
        -Destination (Join-Path $ecdictDirectory "ecdict.csv") `
        -MinimumBytes 1000000

    Download-Asset `
        -Name "ECDICT license" `
        -Uri "https://raw.githubusercontent.com/skywind3000/ECDICT/master/LICENSE" `
        -Destination (Join-Path $ecdictDirectory "LICENSE") `
        -MinimumBytes 500
}

Download-Asset `
    -Name "PaddleOCR license" `
    -Uri "https://raw.githubusercontent.com/PaddlePaddle/PaddleOCR/main/LICENSE" `
    -Destination (Join-Path $rapidOcrDirectory "LICENSE-PaddleOCR.txt") `
    -MinimumBytes 500

Download-Asset `
    -Name "PP-OCRv6 small detection model" `
    -Uri "https://huggingface.co/PaddlePaddle/PP-OCRv6_small_det_onnx/resolve/main/inference.onnx" `
    -Destination (Join-Path $ocrV6Directory "PP-OCRv6_small_det.onnx") `
    -MinimumBytes 5000000

Download-Asset `
    -Name "PP-OCRv6 small recognition model" `
    -Uri "https://huggingface.co/PaddlePaddle/PP-OCRv6_small_rec_onnx/resolve/main/inference.onnx" `
    -Destination (Join-Path $ocrV6Directory "PP-OCRv6_small_rec.onnx") `
    -MinimumBytes 10000000

Download-Asset `
    -Name "PP-OCRv6 dictionary" `
    -Uri "https://raw.githubusercontent.com/PaddlePaddle/PaddleOCR/main/ppocr/utils/dict/ppocrv6_dict.txt" `
    -Destination (Join-Path $ocrV6Directory "ppocrv6_dict.txt") `
    -MinimumBytes 50000

Download-Asset `
    -Name "Korean PP-OCRv5 recognition model" `
    -Uri "https://huggingface.co/PaddlePaddle/korean_PP-OCRv5_mobile_rec_onnx/resolve/main/inference.onnx" `
    -Destination (Join-Path $ocrV5Directory "korean_PP-OCRv5_rec_mobile.onnx") `
    -MinimumBytes 5000000

Download-Asset `
    -Name "Korean PP-OCRv5 dictionary" `
    -Uri "https://raw.githubusercontent.com/PaddlePaddle/PaddleOCR/main/ppocr/utils/dict/ppocrv5_korean_dict.txt" `
    -Destination (Join-Path $ocrV5Directory "ppocrv5_korean_dict.txt") `
    -MinimumBytes 10000

Write-Host "Third-party assets are ready."
