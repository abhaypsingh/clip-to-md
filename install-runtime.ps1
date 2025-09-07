# Windows App Runtime Installation Script
# This script downloads and installs the Windows App Runtime required for ClipTitle

$ErrorActionPreference = "Stop"

Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "Windows App Runtime Installer" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""

# Check if running as administrator
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")

if (-not $isAdmin) {
    Write-Host "NOTE: Not running as administrator." -ForegroundColor Yellow
    Write-Host "If installation fails, please run as administrator." -ForegroundColor Yellow
    Write-Host ""
}

# Define download URL and paths
$runtimeUrl = "https://aka.ms/windowsappsdk/1.6/latest/windowsappruntimeinstall-x64.exe"
$tempDir = Join-Path $env:TEMP "ClipTitle-Runtime"
$installerPath = Join-Path $tempDir "WindowsAppRuntimeInstall.exe"

# Create temp directory
Write-Host "Creating temporary directory..." -ForegroundColor Yellow
if (-not (Test-Path $tempDir)) {
    New-Item -ItemType Directory -Path $tempDir | Out-Null
}

# Download the installer
Write-Host "Downloading Windows App Runtime 1.6..." -ForegroundColor Yellow
Write-Host "From: $runtimeUrl" -ForegroundColor Gray

try {
    # Use WebClient for faster download with progress
    $webClient = New-Object System.Net.WebClient
    
    # Register event for progress updates
    $progressActivity = "Downloading Windows App Runtime"
    Register-ObjectEvent -InputObject $webClient -EventName DownloadProgressChanged -Action {
        $percent = $EventArgs.ProgressPercentage
        Write-Progress -Activity $progressActivity -Status "$percent% Complete" -PercentComplete $percent
    } | Out-Null
    
    # Download the file
    $webClient.DownloadFile($runtimeUrl, $installerPath)
    Write-Progress -Activity $progressActivity -Completed
    
    Write-Host "✓ Download completed" -ForegroundColor Green
    Write-Host "  Saved to: $installerPath" -ForegroundColor Gray
}
catch {
    Write-Host "✗ Failed to download Windows App Runtime" -ForegroundColor Red
    Write-Host "Error: $_" -ForegroundColor Red
    Write-Host ""
    Write-Host "Please download manually from:" -ForegroundColor Yellow
    Write-Host "$runtimeUrl" -ForegroundColor Cyan
    exit 1
}

# Install the runtime
Write-Host ""
Write-Host "Installing Windows App Runtime..." -ForegroundColor Yellow
Write-Host "This may take a minute..." -ForegroundColor Gray

try {
    # Run the installer silently
    $process = Start-Process -FilePath $installerPath -ArgumentList "--quiet" -Wait -PassThru
    
    if ($process.ExitCode -eq 0) {
        Write-Host "✓ Windows App Runtime installed successfully" -ForegroundColor Green
    }
    elseif ($process.ExitCode -eq 1) {
        Write-Host "✓ Windows App Runtime is already installed" -ForegroundColor Green
    }
    else {
        Write-Host "⚠ Installation completed with exit code: $($process.ExitCode)" -ForegroundColor Yellow
        Write-Host "  This may indicate it's already installed or requires admin rights" -ForegroundColor Gray
    }
}
catch {
    Write-Host "✗ Failed to install Windows App Runtime" -ForegroundColor Red
    Write-Host "Error: $_" -ForegroundColor Red
    Write-Host ""
    Write-Host "Try running this script as Administrator" -ForegroundColor Yellow
    exit 1
}

# Clean up temp files
Write-Host ""
Write-Host "Cleaning up temporary files..." -ForegroundColor Yellow
try {
    Remove-Item -Path $tempDir -Recurse -Force -ErrorAction SilentlyContinue
    Write-Host "✓ Cleanup completed" -ForegroundColor Green
}
catch {
    Write-Host "⚠ Could not clean up temp files at: $tempDir" -ForegroundColor Yellow
}

# Verify installation
Write-Host ""
Write-Host "Verifying installation..." -ForegroundColor Yellow
$appRuntime = Get-AppxPackage -Name "Microsoft.WindowsAppRuntime.*" 2>$null
if ($appRuntime) {
    Write-Host "✓ Windows App Runtime is installed" -ForegroundColor Green
    Write-Host "  Version: $($appRuntime.Version)" -ForegroundColor Gray
}
else {
    # Check if it's installed but not visible as AppX
    $installPath = "${env:ProgramFiles}\WindowsApps\Microsoft.WindowsAppRuntime*"
    if (Test-Path $installPath) {
        Write-Host "✓ Windows App Runtime files found" -ForegroundColor Green
    }
    else {
        Write-Host "⚠ Could not verify installation" -ForegroundColor Yellow
        Write-Host "  The runtime may still be installed correctly" -ForegroundColor Gray
    }
}

Write-Host ""
Write-Host "=====================================" -ForegroundColor Green
Write-Host "Installation Complete!" -ForegroundColor Green
Write-Host "=====================================" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "1. Run ClipTitle using: run-cliptitle.cmd" -ForegroundColor White
Write-Host "2. Or directly from:" -ForegroundColor White
Write-Host "   ClipTitle\bin\Release\net9.0-windows10.0.19041.0\win-x64\ClipTitle.exe" -ForegroundColor Gray
Write-Host ""
Write-Host "Press any key to continue..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")