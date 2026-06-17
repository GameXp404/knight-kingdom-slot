Add-Type -AssemblyName System.Drawing

$dst = 'C:\Users\USER\Documents\Unity-Projects\KnightKingdomSlot\Assets\Resources\UI'

foreach ($name in @('frame_table.png', 'frame_table_alt.png')) {
    $path = Join-Path $dst $name
    $bmp = [System.Drawing.Bitmap]::FromFile($path)
    $w = $bmp.Width; $h = $bmp.Height

    $rect = New-Object System.Drawing.Rectangle 0, 0, $w, $h
    $data = $bmp.LockBits($rect, [System.Drawing.Imaging.ImageLockMode]::ReadOnly, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $bytes = New-Object byte[] ($data.Stride * $h)
    [System.Runtime.InteropServices.Marshal]::Copy($data.Scan0, $bytes, 0, $bytes.Length)
    $bmp.UnlockBits($data)

    $cy = [int]($h / 2)
    $cx = [int]($w / 2)

    $leftEdge = 0
    for ($x = $cx; $x -ge 0; $x--) {
        $idx = ($cy * $data.Stride) + ($x * 4)
        if ($bytes[$idx + 3] -gt 30) { $leftEdge = $x + 1; break }
    }
    $rightEdge = $w - 1
    for ($x = $cx; $x -lt $w; $x++) {
        $idx = ($cy * $data.Stride) + ($x * 4)
        if ($bytes[$idx + 3] -gt 30) { $rightEdge = $x - 1; break }
    }
    $topEdge = 0
    for ($y = $cy; $y -ge 0; $y--) {
        $idx = ($y * $data.Stride) + ($cx * 4)
        if ($bytes[$idx + 3] -gt 30) { $topEdge = $y + 1; break }
    }
    $bottomEdge = $h - 1
    for ($y = $cy; $y -lt $h; $y++) {
        $idx = ($y * $data.Stride) + ($cx * 4)
        if ($bytes[$idx + 3] -gt 30) { $bottomEdge = $y - 1; break }
    }

    $hollowW = $rightEdge - $leftEdge
    $hollowH = $bottomEdge - $topEdge
    $rw = [math]::Round($hollowW / $w, 3)
    $rh = [math]::Round($hollowH / $h, 3)

    Write-Host ("{0}: image={1}x{2} hollow={3}x{4} ratio=({5},{6})" -f $name, $w, $h, $hollowW, $hollowH, $rw, $rh)
    $bmp.Dispose()
}
