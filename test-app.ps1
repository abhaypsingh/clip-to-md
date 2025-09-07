# Test ClipTitle Application Launch

Write-Host "Testing ClipTitle Application" -ForegroundColor Cyan
Write-Host "==============================" -ForegroundColor Cyan
Write-Host ""

$exePath = "B:\Dropbox\CWD 8-23-2025\Clip-to-md\ClipTitle\bin\Release\net9.0-windows10.0.19041.0\win-x64\ClipTitle.exe"

# Check if executable exists
if (-not (Test-Path $exePath)) {
    Write-Host "ERROR: ClipTitle.exe not found at expected location" -ForegroundColor Red
    Write-Host "Path: $exePath" -ForegroundColor Yellow
    exit 1
}

Write-Host "Found executable at: $exePath" -ForegroundColor Green
Write-Host ""

# Check for Windows App Runtime
Write-Host "Checking for Windows App Runtime..." -ForegroundColor Yellow
$appRuntime = Get-AppxPackage -Name "Microsoft.WindowsAppRuntime.*" 2>$null
if ($appRuntime) {
    Write-Host "✓ Windows App Runtime installed: $($appRuntime.Version)" -ForegroundColor Green
} else {
    Write-Host "⚠ Windows App Runtime not found - app may not launch" -ForegroundColor Yellow
    Write-Host "  Download from: https://aka.ms/windowsappsdk/1.6/latest/windowsappruntimeinstall-x64.exe" -ForegroundColor Gray
}

Write-Host ""
Write-Host "Attempting to launch ClipTitle..." -ForegroundColor Yellow

try {
    # Try to run the app and capture any immediate errors
    $process = Start-Process -FilePath $exePath -PassThru -WindowStyle Normal -ErrorAction Stop
    
    Start-Sleep -Seconds 2
    
    if ($process.HasExited) {
        Write-Host "✗ Application exited immediately with code: $($process.ExitCode)" -ForegroundColor Red
        
        # Check event log for errors
        Write-Host ""
        Write-Host "Checking Windows Event Log for errors..." -ForegroundColor Yellow
        $events = Get-WinEvent -LogName Application -MaxEvents 10 2>$null | 
                  Where-Object {$_.Message -like "*ClipTitle*" -or $_.Message -like "*.NET*"}
        
        if ($events) {
            Write-Host "Recent related events:" -ForegroundColor Yellow
            $events | ForEach-Object {
                Write-Host "  [$($_.Level)] $($_.Message.Substring(0, [Math]::Min(200, $_.Message.Length)))..." -ForegroundColor Gray
            }
        }
    } else {
        Write-Host "✓ Application launched (PID: $($process.Id))" -ForegroundColor Green
        Write-Host ""
        Write-Host "The app should be running now. Check:" -ForegroundColor Cyan
        Write-Host "  1. System tray for ClipTitle icon" -ForegroundColor White
        Write-Host "  2. Task Manager for ClipTitle.exe process" -ForegroundColor White
        Write-Host "  3. Copy some text to test clipboard monitoring" -ForegroundColor White
    }
}
catch {
    Write-Host "✗ Failed to launch application" -ForegroundColor Red
    Write-Host "Error: $_" -ForegroundColor Red
    
    Write-Host ""
    Write-Host "Possible solutions:" -ForegroundColor Yellow
    Write-Host "  1. Install Windows App Runtime:" -ForegroundColor White
    Write-Host "     https://aka.ms/windowsappsdk/1.6/latest/windowsappruntimeinstall-x64.exe" -ForegroundColor Gray
    Write-Host "  2. Install Visual C++ Redistributables:" -ForegroundColor White
    Write-Host "     https://aka.ms/vs/17/release/vc_redist.x64.exe" -ForegroundColor Gray
    Write-Host "  3. Run as Administrator" -ForegroundColor White
}