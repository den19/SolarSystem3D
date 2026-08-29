# Downloads open-source probe portraits from Wikimedia Commons and writes 256x256 PNGs
# to Assets/Resources/ProbePortraits/. Run from repo root:
#   powershell -ExecutionPolicy Bypass -File Tools\fetch_probe_portraits.ps1

$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Drawing

$projectRoot = Split-Path -Parent $PSScriptRoot
$outDir = Join-Path $projectRoot "Assets\Resources\ProbePortraits"
$tempDir = Join-Path $projectRoot "Temp\probe_portrait_fetch"
$size = 256

$portraits = @(
    @{
        File = "Voyager.png"
        Url = "https://upload.wikimedia.org/wikipedia/commons/6/60/Voyager_spacecraft_model.png"
        Source = "https://commons.wikimedia.org/wiki/File:Voyager_spacecraft_model.png"
        License = "Public domain (NASA)"
        Author = "NASA"
    },
    @{
        File = "NewHorizons.png"
        Url = "https://upload.wikimedia.org/wikipedia/commons/e/ee/New_Horizons_spacecraft_model_1.png"
        Source = "https://commons.wikimedia.org/wiki/File:New_Horizons_spacecraft_model_1.png"
        License = "Public domain (NASA/JHUAPL)"
        Author = "NASA, Applied Physics Laboratory"
    },
    @{
        File = "Juno.png"
        Url = "https://upload.wikimedia.org/wikipedia/commons/f/f6/Juno_spacecraft_model_1.png"
        Source = "https://commons.wikimedia.org/wiki/File:Juno_spacecraft_model_1.png"
        License = "Public domain (NASA)"
        Author = "NASA"
    },
    @{
        File = "Luna16.png"
        Url = "https://upload.wikimedia.org/wikipedia/commons/b/bb/Luna_16.jpg"
        Source = "https://commons.wikimedia.org/wiki/File:Luna_16.jpg"
        License = "CC BY-SA 3.0"
        Author = "See Wikimedia file page"
    },
    @{
        File = "Mars3.png"
        Url = "https://upload.wikimedia.org/wikipedia/commons/8/88/Sonda_Mars_3_A74168720240329.jpg"
        Source = "https://commons.wikimedia.org/wiki/File:Sonda_Mars_3_A74168720240329.jpg"
        License = "CC BY-SA 4.0"
        Author = "Rjcastillo"
    },
    @{
        File = "Change4.png"
        Url = "https://upload.wikimedia.org/wikipedia/commons/d/d1/Chang%27e_4_lander.jpg"
        Source = "https://commons.wikimedia.org/wiki/File:Chang%27e_4_lander.jpg"
        License = "CC BY-SA 4.0"
        Author = "See Wikimedia file page"
    },
    @{
        File = "Tianwen1.png"
        Url = "https://upload.wikimedia.org/wikipedia/commons/7/7f/HK_WCN_%E7%81%A3%E4%BB%94%E5%8C%97_Wan_Chai_North_%E9%A6%99%E6%B8%AF%E6%9C%83%E5%B1%95_HKCEC_%E5%89%B5%E7%A7%91%E5%8D%9A%E8%A6%BD_InnoTech_Expo_HKSAR_%E8%88%AA%E5%A4%A9%E5%99%A8_outer_spacecraft_December_2022_Px3_04.jpg"
        Source = "https://commons.wikimedia.org/wiki/File:HK_WCN_Wan_Chai_North_HKCEC_InnoTech_Expo_outer_spacecraft_December_2022_Px3_04.jpg"
        License = "CC BY-SA 4.0"
        Author = "See Wikimedia file page"
    },
    @{
        File = "Change5.png"
        Url = "https://upload.wikimedia.org/wikipedia/commons/8/85/Chang%27e-5_mockup_at_ZHAL_01.jpg"
        Source = "https://commons.wikimedia.org/wiki/File:Chang%27e-5_mockup_at_ZHAL_01.jpg"
        License = "CC BY-SA 4.0"
        Author = "See Wikimedia file page"
    },
    @{
        File = "Hayabusa2.png"
        Url = "https://upload.wikimedia.org/wikipedia/commons/7/7e/20190605_hayabusa-diagram-tpr-01.png"
        Source = "https://commons.wikimedia.org/wiki/File:20190605_hayabusa-diagram-tpr-01.png"
        License = "CC BY 3.0 (JAXA)"
        Author = "JAXA"
    },
    @{
        File = "Akatsuki.png"
        Url = "https://upload.wikimedia.org/wikipedia/commons/d/d7/Akatsuki_CG01.png"
        Source = "https://commons.wikimedia.org/wiki/File:Akatsuki_CG01.png"
        License = "CC BY 4.0 (JAXA)"
        Author = "JAXA"
    },
    @{
        File = "Chandrayaan3.png"
        Url = "https://upload.wikimedia.org/wikipedia/commons/8/82/Chandrayaan-3_%E2%80%93_Image_of_Vikram_lander_on_lunar_surface_taken_by_Pragyan_rover_navcam_at_1104_IST%2C_30_August_2023_from_15_meters_away_%283x2_cropped%29.jpg"
        Source = "https://commons.wikimedia.org/wiki/File:Chandrayaan-3_%E2%80%93_Image_of_Vikram_lander_on_lunar_surface_taken_by_Pragyan_rover_navcam_at_1104_IST,_30_August_2023_from_15_meters_away_(3x2_cropped).jpg"
        License = "GODL-India (ISRO)"
        Author = "Indian Space Research Organisation"
    },
    @{
        File = "Venera7.png"
        Url = "https://upload.wikimedia.org/wikipedia/commons/2/28/Venera-7.jpg"
        Source = "https://commons.wikimedia.org/wiki/File:Venera-7.jpg"
        License = "CC BY-SA 4.0"
        Author = "Stanislav Kozlovskiy"
        Fit = "contain"  # full craft visible; dark navy letterbox (not center-crop)
    }
)

function Save-CenterCropPortrait {
    param(
        [string]$InputPath,
        [string]$OutputPath,
        [int]$TargetSize = 256
    )

    $img = [System.Drawing.Image]::FromFile($InputPath)
    try {
        $w = $img.Width
        $h = $img.Height
        $side = [Math]::Min($w, $h)
        $x = [int](($w - $side) / 2)
        $y = [int](($h - $side) / 2)

        $bmp = New-Object System.Drawing.Bitmap $TargetSize, $TargetSize
        $g = [System.Drawing.Graphics]::FromImage($bmp)
        try {
            $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
            $g.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
            $src = New-Object System.Drawing.Rectangle $x, $y, $side, $side
            $dst = New-Object System.Drawing.Rectangle 0, 0, $TargetSize, $TargetSize
            $g.DrawImage($img, $dst, $src, [System.Drawing.GraphicsUnit]::Pixel)
        }
        finally {
            $g.Dispose()
        }

        $bmp.Save($OutputPath, [System.Drawing.Imaging.ImageFormat]::Png)
        $bmp.Dispose()
    }
    finally {
        $img.Dispose()
    }
}

# Contain/fit: entire source visible in square with dark navy padding (matches Luna1/Mars3 cards).
function Save-ContainPortrait {
    param(
        [string]$InputPath,
        [string]$OutputPath,
        [int]$TargetSize = 256,
        [double]$Margin = 0.06
    )

    $padColor = [System.Drawing.Color]::FromArgb(255, 4, 8, 18)
    $img = [System.Drawing.Image]::FromFile($InputPath)
    try {
        $inner = [int]($TargetSize * (1.0 - 2.0 * $Margin))
        $scale = [Math]::Min($inner / [double]$img.Width, $inner / [double]$img.Height)
        $dw = [int][Math]::Round($img.Width * $scale)
        $dh = [int][Math]::Round($img.Height * $scale)
        $dx = [int](($TargetSize - $dw) / 2)
        $dy = [int](($TargetSize - $dh) / 2)

        $bmp = New-Object System.Drawing.Bitmap $TargetSize, $TargetSize
        $g = [System.Drawing.Graphics]::FromImage($bmp)
        try {
            $g.Clear($padColor)
            $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
            $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
            $g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
            $g.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
            $dst = New-Object System.Drawing.Rectangle $dx, $dy, $dw, $dh
            $g.DrawImage($img, $dst)
        }
        finally {
            $g.Dispose()
        }

        $bmp.Save($OutputPath, [System.Drawing.Imaging.ImageFormat]::Png)
        $bmp.Dispose()
    }
    finally {
        $img.Dispose()
    }
}

New-Item -ItemType Directory -Force -Path $outDir | Out-Null
New-Item -ItemType Directory -Force -Path $tempDir | Out-Null

$attributionLines = @(
    "Probe portrait image sources and licenses",
    "=========================================",
    "",
    "Most portraits are center-cropped/resized derivatives for in-app probe info cards (256x256).",
    "Venera7.png uses contain/fit (no crop) with dark navy padding so the full craft stays visible.",
    "Luna1.png attribution is preserved below; Custom.png remains procedural artwork.",
    ""
)

foreach ($entry in $portraits) {
    $baseName = [System.IO.Path]::GetFileNameWithoutExtension($entry.File)
    $tempFile = Join-Path $tempDir ($baseName + "_src" + [System.IO.Path]::GetExtension($entry.Url.Split("?")[0]))
    $outFile = Join-Path $outDir $entry.File

    Write-Host "Fetching $($entry.File) ..."
    Start-Sleep -Seconds 3
    Invoke-WebRequest -Uri $entry.Url -OutFile $tempFile -UseBasicParsing -Headers @{ "User-Agent" = "SolarSystemV7-probe-portraits/1.0 (educational app; contact: densappstudio)" }

    $fitMode = if ($entry.ContainsKey("Fit")) { $entry.Fit } else { "crop" }
    if ($fitMode -eq "contain") {
        Save-ContainPortrait -InputPath $tempFile -OutputPath $outFile -TargetSize $size
        Write-Host "  (contain/fit, no crop)"
    }
    else {
        Save-CenterCropPortrait -InputPath $tempFile -OutputPath $outFile -TargetSize $size
    }
    $bytes = (Get-Item $outFile).Length
    Write-Host "  -> $outFile ($bytes bytes)"

    $attributionLines += "$baseName.png"
    $attributionLines += "---------"
    $attributionLines += "Source:  $($entry.Source)"
    $attributionLines += "Author:  $($entry.Author)"
    $attributionLines += "License: $($entry.License)"
    if ($fitMode -eq "contain") {
        $attributionLines += "Derivative: full-frame contain/fit into 256x256 with dark navy padding (no crop)."
    }
    $attributionLines += ""
}

# Preserve existing Luna1 attribution (not in fetch list above)
$attributionLines += "Luna1.png"
$attributionLines += "---------"
$attributionLines += "Source:  https://commons.wikimedia.org/wiki/File:Luna_1_-_2_Spacecraft.png"
$attributionLines += "License: Public domain (NASA/NSSDCA)."
$attributionLines += ""
$attributionLines += "Custom.png"
$attributionLines += "----------"
$attributionLines += "Original procedural artwork (no third-party license). Re-bake via Solar System -> Bake Probe Portraits."

$attributionPath = Join-Path $outDir "ATTRIBUTION.txt"
$attributionLines | Set-Content -Path $attributionPath -Encoding UTF8

Write-Host ""
Write-Host "Done. Wrote $($portraits.Count) portraits to $outDir"
Write-Host "Updated $attributionPath"
