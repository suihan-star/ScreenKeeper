# Regenerates ScreenKeeper's approved path-only SVG artwork using Windows WPF.
# Run: powershell -NoProfile -STA -File tools/Generate-Icons.ps1
[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName PresentationCore, WindowsBase
$assetRoot = Join-Path $PSScriptRoot '../OLED-Sleeper/Assets'
$sizes = @(16, 20, 24, 32, 40, 48, 64, 128, 256)

foreach ($name in @('icon', 'icon-dark')) {
    [xml]$svg = Get-Content -LiteralPath (Join-Path $assetRoot "$name.svg") -Raw
    $brush = [Windows.Media.BrushConverter]::new().ConvertFromInvariantString($svg.svg.fill)
    $frames = @()
    foreach ($size in $sizes) {
        $visual = [Windows.Media.DrawingVisual]::new()
        $context = $visual.RenderOpen()
        $context.PushTransform([Windows.Media.ScaleTransform]::new($size / 16.0, $size / 16.0))
        foreach ($path in $svg.svg.path) {
            $geometry = [Windows.Media.Geometry]::Parse($path.d).Clone()
            if ($path.HasAttribute('transform')) {
                # Support the translate-then-scale transform used by the crescent artwork.
                if ($path.transform -notmatch '^translate\(([-\d.]+) ([-\d.]+)\) scale\(([\d.]+)\)$') { throw 'Unsupported SVG transform' }
                $culture = [Globalization.CultureInfo]::InvariantCulture
                $offsetX = [double]::Parse($Matches[1], $culture)
                $offsetY = [double]::Parse($Matches[2], $culture)
                $scale = [double]::Parse($Matches[3], $culture)
                $geometry.Transform = [Windows.Media.MatrixTransform]::new($scale, 0, 0, $scale, $offsetX, $offsetY)
            }
            $context.DrawGeometry($brush, $null, $geometry)
        }
        $context.Pop()
        $context.Close()
        $bitmap = [Windows.Media.Imaging.RenderTargetBitmap]::new($size, $size, 96, 96, [Windows.Media.PixelFormats]::Pbgra32)
        $bitmap.Render($visual)
        $encoder = [Windows.Media.Imaging.PngBitmapEncoder]::new()
        $encoder.Frames.Add([Windows.Media.Imaging.BitmapFrame]::Create($bitmap))
        $stream = [IO.MemoryStream]::new()
        try { $encoder.Save($stream); $frames += ,$stream.ToArray() } finally { $stream.Dispose() }
    }
    $output = [IO.File]::Create((Join-Path $assetRoot "$name.ico"))
    $writer = [IO.BinaryWriter]::new($output)
    try {
        $writer.Write([uint16]0); $writer.Write([uint16]1); $writer.Write([uint16]$sizes.Count)
        $offset = 6 + 16 * $sizes.Count
        for ($i = 0; $i -lt $sizes.Count; $i++) {
            $dimension = if ($sizes[$i] -eq 256) { 0 } else { $sizes[$i] }
            $writer.Write([byte]$dimension); $writer.Write([byte]$dimension)
            $writer.Write([byte]0); $writer.Write([byte]0)
            $writer.Write([uint16]1); $writer.Write([uint16]32)
            $writer.Write([uint32]$frames[$i].Length); $writer.Write([uint32]$offset)
            $offset += $frames[$i].Length
        }
        foreach ($frame in $frames) { $writer.Write([byte[]]$frame) }
    } finally { $writer.Dispose() }
    Write-Output "$name.ico: $($sizes -join ', ') px"
}
