@echo off
setlocal

echo ClipTitle Run Script
echo ====================
echo.

REM Check if Ollama is running
echo Checking Ollama status...
curl -s -o nul -w "%%{http_code}" http://localhost:11434/api/tags >nul 2>&1
if %errorlevel% equ 0 (
    echo Ollama is running
) else (
    echo WARNING: Ollama is not running or not accessible
    echo          Please start Ollama with: ollama serve
    echo          And ensure a model is available: ollama run llama3:8b
    echo.
    set /p continue="Continue anyway? (y/n): "
    if /i not "!continue!"=="y" exit /b 0
)

REM Check for executable
set RELEASE_EXE=%~dp0ClipTitle\bin\Release\net9.0-windows10.0.19041.0\ClipTitle.exe
set DEBUG_EXE=%~dp0ClipTitle\bin\Debug\net9.0-windows10.0.19041.0\ClipTitle.exe

if exist "%RELEASE_EXE%" (
    echo.
    echo Starting ClipTitle ^(Release build^)...
    echo The app will minimize to system tray
    start "" "%RELEASE_EXE%"
    echo.
    echo ClipTitle started!
    echo Look for the ClipTitle icon in your system tray
    echo Right-click the tray icon for options
) else if exist "%DEBUG_EXE%" (
    echo.
    echo Release build not found, starting Debug build...
    echo The app will minimize to system tray
    start "" "%DEBUG_EXE%"
    echo.
    echo ClipTitle started!
    echo Look for the ClipTitle icon in your system tray
    echo Right-click the tray icon for options
) else (
    echo.
    echo ERROR: ClipTitle.exe not found
    echo        Please run build.bat first
    pause
    exit /b 1
)

echo.
pause