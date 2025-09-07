@echo off
echo =====================================
echo Windows App Runtime Installer
echo =====================================
echo.

set RUNTIME_URL=https://aka.ms/windowsappsdk/1.6/latest/windowsappruntimeinstall-x64.exe
set INSTALLER_PATH=%TEMP%\WindowsAppRuntimeInstall.exe

echo Downloading Windows App Runtime 1.6...
echo.

powershell -Command "Invoke-WebRequest -Uri '%RUNTIME_URL%' -OutFile '%INSTALLER_PATH%' -UseBasicParsing"

if %errorlevel% neq 0 (
    echo ERROR: Failed to download Windows App Runtime
    echo.
    echo Please download manually from:
    echo %RUNTIME_URL%
    pause
    exit /b 1
)

echo Download completed!
echo.
echo Installing Windows App Runtime...
echo This may require administrator privileges.
echo.

"%INSTALLER_PATH%" --quiet

if %errorlevel% equ 0 (
    echo.
    echo SUCCESS: Windows App Runtime installed!
) else if %errorlevel% equ 1 (
    echo.
    echo Windows App Runtime is already installed.
) else (
    echo.
    echo Installation completed with code: %errorlevel%
    echo If it failed, try running as Administrator.
)

echo.
echo Cleaning up...
del "%INSTALLER_PATH%" 2>nul

echo.
echo =====================================
echo Installation Process Complete!
echo =====================================
echo.
echo Now you can run ClipTitle using:
echo   run-cliptitle.cmd
echo.
pause