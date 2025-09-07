@echo off
echo Testing ClipTitle Application
echo ==============================
echo.

set EXE_PATH=B:\Dropbox\CWD 8-23-2025\Clip-to-md\ClipTitle\bin\Release\net9.0-windows10.0.19041.0\win-x64\ClipTitle.exe

if not exist "%EXE_PATH%" (
    echo ERROR: ClipTitle.exe not found
    echo Path: %EXE_PATH%
    pause
    exit /b 1
)

echo Found executable: %EXE_PATH%
echo.
echo Starting ClipTitle...
echo.

start "" "%EXE_PATH%"

echo.
echo If the application doesn't appear to start:
echo.
echo 1. Check system tray for ClipTitle icon
echo 2. Install Windows App Runtime from:
echo    https://aka.ms/windowsappsdk/1.6/latest/windowsappruntimeinstall-x64.exe
echo 3. Install Visual C++ Redistributables from:
echo    https://aka.ms/vs/17/release/vc_redist.x64.exe
echo.
pause