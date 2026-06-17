Add-Type -AssemblyName System.Drawing

$src = 'C:\Users\USER\Documents\Unity-Projects\KnightKingdomSlot\Assets\Resources\Symbols'
$backup = Join-Path $src '_backup_original'

# Restore all symbols from backup (intact with original backgrounds)
$files = @('10.png','J.png','Q.png','K.png','A.png','potion.png','skull.png','treasure.png','cavalry.png','knight.png','dragon.png')

foreach ($f in $files) {
    $bak = Join-Path $backup $f
    $dst = Join-Path $src $f
    if (Test-Path $bak) {
        Copy-Item $bak $dst -Force
        Write-Host "Restored: $f"
    }
}

# Process new crown image (convert .jpg to clean .png, keep intact)
$crownSrc = 'C:\Users\USER\OneDrive\Desktop\crown.png.jpg'
$crownDst = Join-Path $src 'crown.png'

if (Test-Path $crownSrc) {
    $orig = [System.Drawing.Bitmap]::FromFile($crownSrc)
    $w = $orig.Width; $h = $orig.Height
    $bmp = New-Object System.Drawing.Bitmap $w, $h, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.DrawImage($orig, 0, 0, $w, $h)
    $g.Dispose(); $orig.Dispose()
    $bmp.Save($crownDst, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
    Write-Host "New crown.png saved: ${w}x${h}"
} else {
    Write-Host "WARNING: crown source not found at $crownSrc"
}

Write-Host "`nDone. All symbols restored to original + crown updated."
