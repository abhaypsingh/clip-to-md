# PowerShell script to convert SVG to ICO
# This script creates an ICO file with multiple sizes for best system tray appearance

$svgPath = "ClipTitle.svg"
$icoPath = "ClipTitle.ico"

# Check if ImageMagick is installed
$magickPath = Get-Command magick -ErrorAction SilentlyContinue

if ($magickPath) {
    Write-Host "Using ImageMagick to convert SVG to ICO..."
    
    # Convert SVG to multiple PNG sizes then combine into ICO
    magick convert -background transparent "$svgPath" -resize 16x16 temp_16.png
    magick convert -background transparent "$svgPath" -resize 32x32 temp_32.png
    magick convert -background transparent "$svgPath" -resize 48x48 temp_48.png
    magick convert -background transparent "$svgPath" -resize 64x64 temp_64.png
    magick convert -background transparent "$svgPath" -resize 128x128 temp_128.png
    magick convert -background transparent "$svgPath" -resize 256x256 temp_256.png
    
    # Combine into ICO
    magick convert temp_16.png temp_32.png temp_48.png temp_64.png temp_128.png temp_256.png "$icoPath"
    
    # Clean up temp files
    Remove-Item temp_*.png
    
    Write-Host "ICO file created successfully: $icoPath"
} else {
    Write-Host "ImageMagick not found. Please install ImageMagick or use an online SVG to ICO converter."
    Write-Host "You can download ImageMagick from: https://imagemagick.org/script/download.php"
    Write-Host ""
    Write-Host "Alternatively, you can use online converters like:"
    Write-Host "- https://convertio.co/svg-ico/"
    Write-Host "- https://cloudconvert.com/svg-to-ico"
    Write-Host ""
    Write-Host "For now, creating a placeholder ICO file..."
}