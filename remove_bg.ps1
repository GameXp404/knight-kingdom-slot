Add-Type -AssemblyName System.Drawing

$src = 'C:\Users\USER\Documents\Unity-Projects\KnightKingdomSlot\Assets\Resources\Symbols'
$backup = Join-Path $src '_backup_original'
if (-not (Test-Path $backup)) { New-Item -ItemType Directory -Path $backup -Force | Out-Null }

$files = @('10.png','J.png','Q.png','K.png','A.png','potion.png','skull.png','treasure.png','cavalry.png','knight.png','dragon.png','crown.jpg')

foreach ($f in $files) {
    $path = Join-Path $src $f
    if (-not (Test-Path $path)) { Write-Host "SKIP (not found): $f"; continue }

    Copy-Item $path (Join-Path $backup $f) -Force

    $orig = [System.Drawing.Bitmap]::FromFile($path)
    $w = $orig.Width; $h = $orig.Height
    $bmp = New-Object System.Drawing.Bitmap $w, $h, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.DrawImage($orig, 0, 0, $w, $h)
    $g.Dispose(); $orig.Dispose()

    $c1 = $bmp.GetPixel(2, 2)
    $c2 = $bmp.GetPixel($w-3, 2)
    $c3 = $bmp.GetPixel(2, $h-3)
    $c4 = $bmp.GetPixel($w-3, $h-3)
    $bgR = [int](([int]$c1.R + [int]$c2.R + [int]$c3.R + [int]$c4.R) / 4)
    $bgG = [int](([int]$c1.G + [int]$c2.G + [int]$c3.G + [int]$c4.G) / 4)
    $bgB = [int](([int]$c1.B + [int]$c2.B + [int]$c3.B + [int]$c4.B) / 4)

    $rect = New-Object System.Drawing.Rectangle 0, 0, $w, $h
    $data = $bmp.LockBits($rect, [System.Drawing.Imaging.ImageLockMode]::ReadWrite, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $bytes = New-Object byte[] ($data.Stride * $h)
    [System.Runtime.InteropServices.Marshal]::Copy($data.Scan0, $bytes, 0, $bytes.Length)

    $tol = 38
    $tolFade = 70
    $changed = 0
    for ($i = 0; $i -lt $bytes.Length; $i += 4) {
        $b = [int]$bytes[$i]; $g2 = [int]$bytes[$i+1]; $r = [int]$bytes[$i+2]
        $dR = [Math]::Abs($r - $bgR); $dG = [Math]::Abs($g2 - $bgG); $dB = [Math]::Abs($b - $bgB)
        $maxD = [Math]::Max($dR, [Math]::Max($dG, $dB))
        if ($maxD -lt $tol) {
            $bytes[$i+3] = 0; $changed++
        } elseif ($maxD -lt $tolFade) {
            $fade = ($maxD - $tol) / ($tolFade - $tol)
            $bytes[$i+3] = [byte]([int]([byte]$bytes[$i+3] * $fade))
        }
    }

    [System.Runtime.InteropServices.Marshal]::Copy($bytes, 0, $data.Scan0, $bytes.Length)
    $bmp.UnlockBits($data)

    $outName = [System.IO.Path]::GetFileNameWithoutExtension($f) + '.png'
    $outPath = Join-Path $src $outName
    $bmp.Save($outPath, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()

    if ($f -ne $outName) {
        Remove-Item $path -Force
        $oldMeta = "$path.meta"
        if (Test-Path $oldMeta) { Remove-Item $oldMeta -Force }
    }

    $pct = [math]::Round(($changed * 4 / $bytes.Length) * 100, 1)
    Write-Host ("OK: {0,-14} bg=({1,3},{2,3},{3,4}) cleared={4}%" -f $f, $bgR, $bgG, $bgB, $pct)
}

Write-Host "`nDone. Backup di: $backup"
