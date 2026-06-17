Add-Type -AssemblyName System.Drawing

$inputs = @(
    @{ Src = 'C:\Users\USER\OneDrive\Desktop\OIG2.aLcX.jpg'; Out = 'frame_table.png' },
    @{ Src = 'C:\Users\USER\OneDrive\Desktop\OIG2.E_phDSMuGIbFGzLz.jpg'; Out = 'frame_table_alt.png' }
)

$dst = 'C:\Users\USER\Documents\Unity-Projects\KnightKingdomSlot\Assets\Resources\UI'

foreach ($item in $inputs) {
    $orig = [System.Drawing.Bitmap]::FromFile($item.Src)
    $w = $orig.Width; $h = $orig.Height
    $bmp = New-Object System.Drawing.Bitmap $w, $h, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.DrawImage($orig, 0, 0, $w, $h)
    $g.Dispose(); $orig.Dispose()

    $rect = New-Object System.Drawing.Rectangle 0, 0, $w, $h
    $data = $bmp.LockBits($rect, [System.Drawing.Imaging.ImageLockMode]::ReadWrite, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $bytes = New-Object byte[] ($data.Stride * $h)
    [System.Runtime.InteropServices.Marshal]::Copy($data.Scan0, $bytes, 0, $bytes.Length)

    $tol = 30
    $tolFade = 70
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
    $bmp.UnlockBits($data)
    $outPath = Join-Path $dst $item.Out
    $bmp.Save($outPath, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
    $pct = [math]::Round(($changed * 4 / $bytes.Length) * 100, 1)
    Write-Host ("OK: {0} -> {1} cleared={2}%" -f $item.Src.Substring($item.Src.LastIndexOf('\')+1), $item.Out, $pct)
}
