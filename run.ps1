# ClipTitle Run Script for Windows

$ErrorActionPreference = "Stop"

Write-Host "ClipTitle Run Script" -ForegroundColor Cyan
Write-Host "====================" -ForegroundColor Cyan

# Check if Ollama is running
Write-Host "`nChecking Ollama status..." -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "http://localhost:11434/api/tags" -Method Get -TimeoutSec 2 -ErrorAction SilentlyContinue
    if ($response.StatusCode -eq 200) {
        Write-Host "✓ Ollama is running" -ForegroundColor Green
    }
} catch {
    Write-Host "⚠ Ollama is not running or not accessible" -ForegroundColor Yellow
    Write-Host "  Please start Ollama with: ollama serve" -ForegroundColor Gray
    Write-Host "  And ensure a model is available: ollama run llama3:8b" -ForegroundColor Gray
    
    $continue = Read-Host "`nContinue anyway? (y/n)"
    if ($continue -ne 'y') {
        exit 0
    }
}

# Set paths
$projectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$exePath = Join-Path $projectRoot "ClipTitle\bin\Release\net9.0-windows10.0.19041.0\ClipTitle.exe"
$debugExePath = Join-Path $projectRoot "ClipTitle\bin\Debug\net9.0-windows10.0.19041.0\ClipTitle.exe"

# Check if Release build exists
if (Test-Path $exePath) {
    Write-Host "`nStarting ClipTitle (Release build)..." -ForegroundColor Green
    Write-Host "The app will minimize to system tray" -ForegroundColor Gray
    Start-Process -FilePath $exePath
} elseif (Test-Path $debugExePath) {
    Write-Host "`nRelease build not found, starting Debug build..." -ForegroundColor Yellow
    Write-Host "The app will minimize to system tray" -ForegroundColor Gray
    Start-Process -FilePath $debugExePath
} else {
    Write-Host "`n✗ ClipTitle.exe not found" -ForegroundColor Red
    Write-Host "  Please run .\build.ps1 first" -ForegroundColor Yellow
    exit 1
}

Write-Host "`n✓ ClipTitle started!" -ForegroundColor Green
Write-Host "  Look for the ClipTitle icon in your system tray" -ForegroundColor Gray
Write-Host "  Right-click the tray icon for options" -ForegroundColor Gray