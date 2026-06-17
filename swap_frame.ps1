Add-Type -AssemblyName System.Drawing

$src = 'C:\Users\USER\OneDrive\Desktop\neoclassical-gold-frame-with-acanthus-leaf-border-decorated-metallic-metal-luxury-expensive-border_655090-2450853.jpg'
$dst = 'C:\Users\USER\Documents\Unity-Projects\KnightKingdomSlot\Assets\Resources\UI\frame_table.png'

$orig = [System.Drawing.Bitmap]::FromFile($src)
$w = $orig.Width; $h = $orig.Height
$bmp = New-Object System.Drawing.Bitmap $w, $h, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.DrawImage($orig, 0, 0, $w, $h)
$g.Dispose(); $orig.Dispose()

$rect = New-Object System.Drawing.Rectangle 0, 0, $w, $h
$data = $bmp.LockBits($rect, [System.Drawing.Imaging.ImageLockMode]::ReadWrite, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$bytes = New-Object byte[] ($data.Stride * $h)
[System.Runtime.InteropServices.Marshal]::Copy($data.Scan0, $bytes, 0, $bytes.Length)

$tol = 28
$tolFade = 60
$changed = 0
for ($i = 0; $i -lt $bytes.Length; $i += 4) {
    $b = [int]$bytes[$i]; $g2 = [int]$bytes[$i+1]; $r = [int]$bytes[$i+2]
    $maxC = [Math]::Max($r, [Math]::Max($g2, $b))
    if ($maxC -lt $tol) {
        $bytes[$i+3] = 0; $changed++
    } elseif ($maxC -lt $tolFade) {
        $fade = ($maxC - $tol) / ($tolFade - $tol)
        $bytes[$i+3] = [byte]([int]([byte]$bytes[$i+3] * $fade))
    }
}
[System.Runtime.InteropServices.Marshal]::Copy($bytes, 0, $data.Scan0, $bytes.Length)

# Detect hollow center
$cy = [int]($h / 2); $cx = [int]($w / 2)
$leftEdge = 0
for ($x = $cx; $x -ge 0; $x--) { $idx = ($cy * $data.Stride) + ($x * 4); if ($bytes[$idx + 3] -gt 30) { $leftEdge = $x + 1; break } }
$rightEdge = $w - 1
for ($x = $cx; $x -lt $w; $x++) { $idx = ($cy * $data.Stride) + ($x * 4); if ($bytes[$idx + 3] -gt 30) { $rightEdge = $x - 1; break } }
$topEdge = 0
for ($y = $cy; $y -ge 0; $y--) { $idx = ($y * $data.Stride) + ($cx * 4); if ($bytes[$idx + 3] -gt 30) { $topEdge = $y + 1; break } }
$bottomEdge = $h - 1
for ($y = $cy; $y -lt $h; $y++) { $idx = ($y * $data.Stride) + ($cx * 4); if ($bytes[$idx + 3] -gt 30) { $bottomEdge = $y - 1; break } }

$bmp.UnlockBits($data)
$bmp.Save($dst, [System.Drawing.Imaging.ImageFormat]::Png)
$bmp.Dispose()

$lBorder = $leftEdge
$rBorder = $w - $rightEdge - 1
$tBorder = $topEdge
$bBorder = $h - $bottomEdge - 1

Write-Host ("Image: {0}x{1}" -f $w, $h)
Write-Host ("Hollow: x=[{0},{1}] y=[{2},{3}]" -f $leftEdge, $rightEdge, $topEdge, $bottomEdge)
Write-Host ("Borders: L={0} R={1} T={2} B={3}" -f $lBorder, $rBorder, $tBorder, $bBorder)
Write-Host ("Suggest spriteBorder = ({0}, {1}, {2}, {3})" -f $lBorder, $bBorder, $rBorder, $tBorder)
