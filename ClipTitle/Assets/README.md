# Assets Directory

This directory should contain the following image files for the application:

## Required Files

1. **ClipTitle.ico** - Main application icon (16x16, 32x32, 48x48, 256x256)
2. **Square150x150Logo.png** - Medium tile logo (150x150)
3. **Square44x44Logo.png** - Small tile logo (44x44)
4. **Wide310x150Logo.png** - Wide tile logo (310x150)
5. **SplashScreen.png** - Splash screen image (620x300)
6. **StoreLogo.png** - Store logo (50x50)

## Creating Placeholder Icons

For now, you can create simple placeholder images or use any icon generator tool.

### Quick Placeholder Generation (PowerShell)

```powershell
# This creates a simple colored square as placeholder
Add-Type -AssemblyName System.Drawing

$sizes = @(
    @{Name="Square150x150Logo.png"; Width=150; Height=150},
    @{Name="Square44x44Logo.png"; Width=44; Height=44},
    @{Name="Wide310x150Logo.png"; Width=310; Height=150},
    @{Name="SplashScreen.png"; Width=620; Height=300},
    @{Name="StoreLogo.png"; Width=50; Height=50}
)

foreach ($size in $sizes) {
    $bitmap = New-Object System.Drawing.Bitmap $size.Width, $size.Height
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.Clear([System.Drawing.Color]::DodgerBlue)
    $graphics.DrawString("CT", [System.Drawing.Font]::new("Arial", ($size.Height/3)), 
        [System.Drawing.Brushes]::White, ($size.Width/4), ($size.Height/4))
    $bitmap.Save((Join-Path $PSScriptRoot $size.Name))
    $graphics.Dispose()
    $bitmap.Dispose()
}
```

For the .ico file, you can use online converters or tools like:
- https://www.icoconverter.com/
- https://favicon.io/