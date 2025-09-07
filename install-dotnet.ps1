# .NET 9 SDK Installation Script for Windows
# This script downloads and installs the .NET 9 SDK

$ErrorActionPreference = "Stop"

Write-Host "==================================" -ForegroundColor Cyan
Write-Host ".NET 9 SDK Installation Script" -ForegroundColor Cyan
Write-Host "==================================" -ForegroundColor Cyan
Write-Host ""

# Check if running as administrator
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")

if (-not $isAdmin) {
    Write-Host "WARNING: Not running as administrator." -ForegroundColor Yellow
    Write-Host "Installation may require admin privileges." -ForegroundColor Yellow
    Write-Host ""
}

# Define download URL for .NET 9 SDK
$dotnetVersion = "9.0"
$dotnetUrl = "https://dot.net/v1/dotnet-install.ps1"

# Create temp directory
$tempDir = Join-Path $env:TEMP "dotnet-install"
if (-not (Test-Path $tempDir)) {
    New-Item -ItemType Directory -Path $tempDir | Out-Null
}

Write-Host "Downloading .NET installation script..." -ForegroundColor Yellow
$installScript = Join-Path $tempDir "dotnet-install.ps1"

try {
    # Download the installation script
    Invoke-WebRequest -Uri $dotnetUrl -OutFile $installScript -UseBasicParsing
    Write-Host "✓ Download completed" -ForegroundColor Green
} catch {
    Write-Host "✗ Failed to download installation script" -ForegroundColor Red
    Write-Host "Error: $_" -ForegroundColor Red
    Write-Host ""
    Write-Host "Alternative: Download .NET 9 SDK manually from:" -ForegroundColor Yellow
    Write-Host "https://dotnet.microsoft.com/download/dotnet/9.0" -ForegroundColor Cyan
    exit 1
}

Write-Host ""
Write-Host "Installing .NET 9 SDK..." -ForegroundColor Yellow
Write-Host "This may take several minutes..." -ForegroundColor Gray

try {
    # Run the installation script
    & $installScript -Channel 9.0 -InstallDir "$env:ProgramFiles\dotnet" -Architecture x64
    
    Write-Host "✓ .NET 9 SDK installed successfully" -ForegroundColor Green
    
    # Add to PATH if not already there
    $dotnetPath = "$env:ProgramFiles\dotnet"
    $currentPath = [Environment]::GetEnvironmentVariable("Path", "User")
    
    if ($currentPath -notlike "*$dotnetPath*") {
        Write-Host ""
        Write-Host "Adding .NET to PATH..." -ForegroundColor Yellow
        [Environment]::SetEnvironmentVariable("Path", "$currentPath;$dotnetPath", "User")
        $env:Path = "$env:Path;$dotnetPath"
        Write-Host "✓ Added to PATH" -ForegroundColor Green
    }
    
    Write-Host ""
    Write-Host "Verifying installation..." -ForegroundColor Yellow
    
    # Verify installation
    $dotnetExe = Join-Path $dotnetPath "dotnet.exe"
    if (Test-Path $dotnetExe) {
        & $dotnetExe --version
        Write-Host "✓ .NET SDK is ready to use" -ForegroundColor Green
    } else {
        Write-Host "⚠ Installation completed but dotnet.exe not found" -ForegroundColor Yellow
    }
    
} catch {
    Write-Host "✗ Installation failed" -ForegroundColor Red
    Write-Host "Error: $_" -ForegroundColor Red
    Write-Host ""
    Write-Host "Please download and install manually from:" -ForegroundColor Yellow
    Write-Host "https://dotnet.microsoft.com/download/dotnet/9.0" -ForegroundColor Cyan
    exit 1
} finally {
    # Clean up temp files
    if (Test-Path $tempDir) {
        Remove-Item -Path $tempDir -Recurse -Force -ErrorAction SilentlyContinue
    }
}

Write-Host ""
Write-Host "==================================" -ForegroundColor Green
Write-Host "Installation Complete!" -ForegroundColor Green
Write-Host "==================================" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "1. Close and reopen your terminal/PowerShell" -ForegroundColor White
Write-Host "2. Run: build.ps1" -ForegroundColor White
Write-Host "3. Run: run.ps1" -ForegroundColor White
Write-Host ""
Write-Host "Press any key to continue..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")