@echo off
setlocal

echo ClipTitle Build Script
echo ======================
echo.

REM Check if dotnet is installed
where dotnet >nul 2>&1
if %errorlevel% neq 0 (
    echo ERROR: .NET SDK not found. Please install .NET 9 SDK from:
    echo        https://dotnet.microsoft.com/download/dotnet/9.0
    pause
    exit /b 1
)

echo Found .NET SDK:
dotnet --version
echo.

REM Clean previous builds
echo Cleaning previous builds...
if exist "ClipTitle\bin" rmdir /s /q "ClipTitle\bin"
if exist "ClipTitle\obj" rmdir /s /q "ClipTitle\obj"

REM Restore packages
echo.
echo Restoring NuGet packages...
dotnet restore ClipTitle.sln
if %errorlevel% neq 0 (
    echo ERROR: Failed to restore packages
    pause
    exit /b 1
)

REM Build the solution
echo.
echo Building ClipTitle...
dotnet build ClipTitle.sln --configuration Release --no-restore
if %errorlevel% neq 0 (
    echo ERROR: Build failed
    pause
    exit /b 1
)

echo.
echo ========================================
echo BUILD SUCCESSFUL!
echo ========================================
echo.
echo Output location:
echo   %~dp0ClipTitle\bin\Release\net9.0-windows10.0.19041.0\
echo.
echo To run the application:
echo   run.bat
echo.
pause