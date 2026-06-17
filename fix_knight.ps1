Add-Type -AssemblyName System.Drawing

$src = 'C:\Users\USER\Documents\Unity-Projects\KnightKingdomSlot\Assets\Resources\Symbols\_backup_original\knight.png'
$dst = 'C:\Users\USER\Documents\Unity-Projects\KnightKingdomSlot\Assets\Resources\Symbols\knight.png'

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

# Luminance-based masking
# Pixels with lum < cutoffLow = fully transparent
# Pixels with lum > cutoffHigh = fully opaque
# Smooth falloff in between
$cutoffLow = 32   # 0-255 scale
$cutoffHigh = 95
$range = $cutoffHigh - $cutoffLow
$changed = 0
$total = 0

for ($i = 0; $i -lt $bytes.Length; $i += 4) {
    $b = [int]$bytes[$i]; $g2 = [int]$bytes[$i+1]; $r = [int]$bytes[$i+2]
    # Standard luminance: 0.299R + 0.587G + 0.114B
    $lum = ($r * 299 + $g2 * 587 + $b * 114) / 1000
    $total++

    if ($lum -lt $cutoffLow) {
        $bytes[$i+3] = 0
        $changed++
    } elseif ($lum -lt $cutoffHigh) {
        $fade = ($lum - $cutoffLow) / $range
        $bytes[$i+3] = [byte]([int]([byte]$bytes[$i+3] * $fade))
    }
}

[System.Runtime.InteropServices.Marshal]::Copy($bytes, 0, $data.Scan0, $bytes.Length)
$bmp.UnlockBits($data)
$bmp.Save($dst, [System.Drawing.Imaging.ImageFormat]::Png)
$bmp.Dispose()

$pct = [math]::Round(($changed / $total) * 100, 1)
Write-Host ("Knight.png processed: cleared={0}% (total {1} pixels)" -f $pct, $total)
