Add-Type -AssemblyName System.Drawing

$lines = @(
    @(0,0,0,0,0), @(1,1,1,1,1), @(2,2,2,2,2), @(3,3,3,3,3),
    @(0,1,2,1,0), @(1,2,3,2,1), @(3,2,1,2,3), @(2,1,0,1,2),
    @(0,0,1,0,0), @(1,1,2,1,1), @(2,2,3,2,2), @(3,3,2,3,3),
    @(0,1,1,1,0), @(1,2,2,2,1), @(2,3,3,3,2), @(3,2,2,2,3),
    @(0,1,0,1,0), @(1,2,1,2,1), @(2,3,2,3,2), @(3,2,3,2,3),
    @(0,0,2,0,0), @(1,1,3,1,1), @(0,2,0,2,0), @(1,3,1,3,1),
    @(0,1,2,3,0), @(1,2,3,2,0), @(0,2,3,2,1), @(3,1,0,1,3),
    @(2,0,1,0,2), @(3,3,0,3,3), @(0,0,3,0,0), @(1,3,2,3,1),
    @(2,0,3,0,2), @(0,1,3,1,0), @(3,2,0,2,3), @(1,0,2,0,1),
    @(2,3,1,3,2), @(0,3,0,3,0), @(3,0,3,0,3), @(1,2,0,2,1)
)

$cellSize = 38
$cellGap = 2
$padding = 14
$labelHeight = 26
$rows = 4
$cols = 5
$patternW = $cellSize * $cols + $cellGap * ($cols - 1)
$patternH = $cellSize * $rows + $cellGap * ($rows - 1) + $labelHeight + 6
$gridCols = 4
$gridRows = 10
$totalW = $gridCols * $patternW + ($gridCols + 1) * $padding
$totalH = $gridRows * $patternH + ($gridRows + 1) * $padding

$bmp = New-Object System.Drawing.Bitmap $totalW, $totalH
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.SmoothingMode = 'AntiAlias'
$g.TextRenderingHint = 'AntiAliasGridFit'
$g.Clear([System.Drawing.Color]::FromArgb(245, 245, 250))

$brushRed = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(220, 40, 40))
$brushWhite = [System.Drawing.Brushes]::White
$brushLine = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(40, 40, 40))
$penBorder = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(180, 180, 190), 1)
$penConnect = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(220, 40, 40), 3)
$penConnect.LineJoin = 'Round'
$font = New-Object System.Drawing.Font ("Segoe UI", 11, [System.Drawing.FontStyle]::Bold)

for ($i = 0; $i -lt $lines.Count; $i++) {
    $col = $i % $gridCols
    $row = [Math]::Floor($i / $gridCols)
    $x0 = $padding + $col * ($patternW + $padding)
    $y0 = $padding + $row * ($patternH + $padding)

    $label = "Line " + ($i + 1)
    $g.DrawString($label, $font, $brushLine, [single]$x0, [single]$y0)

    $gridY0 = $y0 + $labelHeight
    $line = $lines[$i]

    # Draw all cells (white bg with thin border)
    for ($r = 0; $r -lt $rows; $r++) {
        for ($c = 0; $c -lt $cols; $c++) {
            $cellX = $x0 + $c * ($cellSize + $cellGap)
            $cellY = $gridY0 + $r * ($cellSize + $cellGap)
            $g.FillRectangle($brushWhite, $cellX, $cellY, $cellSize, $cellSize)
            $g.DrawRectangle($penBorder, $cellX, $cellY, $cellSize, $cellSize)
        }
    }

    # Highlight payline cells in red
    $points = New-Object 'System.Collections.Generic.List[System.Drawing.PointF]'
    for ($c = 0; $c -lt $cols; $c++) {
        $r = $line[$c]
        $cellX = $x0 + $c * ($cellSize + $cellGap)
        $cellY = $gridY0 + $r * ($cellSize + $cellGap)
        $g.FillRectangle($brushRed, $cellX, $cellY, $cellSize, $cellSize)
        $cx = $cellX + $cellSize / 2.0
        $cy = $cellY + $cellSize / 2.0
        $points.Add((New-Object System.Drawing.PointF $cx, $cy))
    }

    # Draw connecting line through cells
    for ($k = 0; $k -lt ($points.Count - 1); $k++) {
        $g.DrawLine($penConnect, $points[$k], $points[$k+1])
    }
}

$g.Dispose()
$out = 'C:\Users\USER\OneDrive\Desktop\paylines_visual.png'
$bmp.Save($out, [System.Drawing.Imaging.ImageFormat]::Png)
$bmp.Dispose()
Write-Host "Saved: $out (${totalW}x${totalH})"
