# ClipTitle Build Script for Windows
# Requires: .NET 9 SDK

$ErrorActionPreference = "Stop"

Write-Host "ClipTitle Build Script" -ForegroundColor Cyan
Write-Host "======================" -ForegroundColor Cyan

# Check if dotnet is installed
try {
    $dotnetVersion = dotnet --version
    Write-Host "✓ Found .NET SDK version: $dotnetVersion" -ForegroundColor Green
} catch {
    Write-Host "✗ .NET SDK not found. Please install .NET 9 SDK from:" -ForegroundColor Red
    Write-Host "  https://dotnet.microsoft.com/download/dotnet/9.0" -ForegroundColor Yellow
    exit 1
}

# Set working directory
$projectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $projectRoot
Write-Host "Working directory: $projectRoot" -ForegroundColor Gray

# Clean previous builds
Write-Host "`nCleaning previous builds..." -ForegroundColor Yellow
if (Test-Path "ClipTitle\bin") {
    Remove-Item -Path "ClipTitle\bin" -Recurse -Force
}
if (Test-Path "ClipTitle\obj") {
    Remove-Item -Path "ClipTitle\obj" -Recurse -Force
}

# Restore NuGet packages
Write-Host "`nRestoring NuGet packages..." -ForegroundColor Yellow
dotnet restore ClipTitle.sln
if ($LASTEXITCODE -ne 0) {
    Write-Host "✗ Failed to restore packages" -ForegroundColor Red
    exit 1
}
Write-Host "✓ Packages restored successfully" -ForegroundColor Green

# Build the solution
Write-Host "`nBuilding ClipTitle..." -ForegroundColor Yellow
dotnet build ClipTitle.sln --configuration Release --no-restore
if ($LASTEXITCODE -ne 0) {
    Write-Host "✗ Build failed" -ForegroundColor Red
    exit 1
}
Write-Host "✓ Build completed successfully" -ForegroundColor Green

# Display output location
$outputPath = Join-Path $projectRoot "ClipTitle\bin\Release\net9.0-windows10.0.19041.0"
Write-Host "`nBuild output location:" -ForegroundColor Cyan
Write-Host "  $outputPath" -ForegroundColor White

# Check if executable exists
$exePath = Join-Path $outputPath "ClipTitle.exe"
if (Test-Path $exePath) {
    Write-Host "`n✓ Executable created:" -ForegroundColor Green
    Write-Host "  $exePath" -ForegroundColor White
    
    Write-Host "`nTo run the application:" -ForegroundColor Cyan
    Write-Host "  .\run.ps1" -ForegroundColor White
    Write-Host "  or directly:" -ForegroundColor Gray
    Write-Host "  & '$exePath'" -ForegroundColor White
} else {
    Write-Host "`n⚠ Executable not found at expected location" -ForegroundColor Yellow
    Write-Host "  Check the build output for errors" -ForegroundColor Yellow
}

Write-Host "`nBuild script completed!" -ForegroundColor Green