Add-Type -AssemblyName System.Drawing

$projectRoot = Split-Path -Parent $PSScriptRoot
$sourcePath = Join-Path $projectRoot 'docs\janela-de-escolha.png'
$iconPath = Join-Path $projectRoot 'extensions\shared\icon128.png'
$outputPath = Join-Path $projectRoot 'dist\FirawSelector-Store-Screenshot.png'
$operaIconPath = Join-Path $projectRoot 'dist\FirawSelector-Opera-Icon64.png'

$canvas = New-Object System.Drawing.Bitmap(1280, 800, ([System.Drawing.Imaging.PixelFormat]::Format24bppRgb))
$operaIcon = New-Object System.Drawing.Bitmap(64, 64, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb))
$graphics = [System.Drawing.Graphics]::FromImage($canvas)
$iconGraphics = [System.Drawing.Graphics]::FromImage($operaIcon)
$source = [System.Drawing.Image]::FromFile($sourcePath)
$icon = [System.Drawing.Image]::FromFile($iconPath)
$titleFont = New-Object System.Drawing.Font('Segoe UI', 30, [System.Drawing.FontStyle]::Bold)
$bodyFont = New-Object System.Drawing.Font('Segoe UI', 17)
$captionFont = New-Object System.Drawing.Font('Segoe UI', 13)
$white = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::White)
$muted = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(180, 198, 216))
$accent = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(39, 203, 225))

try {
    $graphics.Clear([System.Drawing.Color]::FromArgb(15, 23, 34))
    $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
    $graphics.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit
    $graphics.DrawImage($icon, 68, 60, 72, 72)
    $graphics.DrawString('FirawSelector', $titleFont, $white, 160, 59)
    $graphics.FillRectangle($accent, 68, 150, 1144, 3)
    $graphics.DrawString('Escolha onde abrir cada link', $titleFont, $white, 68, 205)
    $graphics.DrawString('Selecione o navegador ou perfil no aplicativo local para Windows.', $bodyFont, $muted, 68, 267)
    $graphics.DrawImage($source, 380, 305, $source.Width, $source.Height)
    $graphics.DrawString('Extensao + aplicativo FirawSelector', $captionFont, $muted, 68, 739)
    $canvas.Save($outputPath, [System.Drawing.Imaging.ImageFormat]::Png)
    $iconGraphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $iconGraphics.DrawImage($icon, 0, 0, 64, 64)
    $operaIcon.Save($operaIconPath, [System.Drawing.Imaging.ImageFormat]::Png)
}
finally {
    $accent.Dispose()
    $muted.Dispose()
    $white.Dispose()
    $captionFont.Dispose()
    $bodyFont.Dispose()
    $titleFont.Dispose()
    $icon.Dispose()
    $source.Dispose()
    $graphics.Dispose()
    $iconGraphics.Dispose()
    $canvas.Dispose()
    $operaIcon.Dispose()
}

Write-Output $outputPath
