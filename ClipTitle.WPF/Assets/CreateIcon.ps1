Add-Type -AssemblyName System.Drawing

# Create a 32x32 bitmap
$bitmap = New-Object System.Drawing.Bitmap 32, 32

# Create graphics object
$graphics = [System.Drawing.Graphics]::FromImage($bitmap)

# Set background color (blue)
$graphics.Clear([System.Drawing.Color]::FromArgb(37, 99, 235))

# Create font and brush for text
$font = New-Object System.Drawing.Font('Arial', 16, [System.Drawing.FontStyle]::Bold)
$brush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::White)

# Draw "C" in the center
$graphics.DrawString('C', $font, $brush, 8, 4)

# Convert to icon
$icon = [System.Drawing.Icon]::FromHandle($bitmap.GetHicon())

# Save to file
$outputPath = Join-Path $PSScriptRoot "ClipTitle.ico"
$fileStream = [System.IO.File]::Create($outputPath)
$icon.Save($fileStream)
$fileStream.Close()

# Clean up
$graphics.Dispose()
$bitmap.Dispose()

Write-Host "ICO file created successfully at: $outputPath"