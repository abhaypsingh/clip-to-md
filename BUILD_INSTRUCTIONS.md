# ClipTitle Build Instructions

## Prerequisites

Before building ClipTitle, ensure you have the following installed:

1. **.NET 9 SDK** (required)
   - Download from: https://dotnet.microsoft.com/download/dotnet/9.0
   - Verify installation: `dotnet --version`

2. **Ollama** (required for runtime)
   - Download from: https://ollama.ai/
   - Install and run a model: `ollama run llama3:8b`

3. **Visual Studio 2022** (optional but recommended)
   - Community edition is free
   - Include ".NET desktop development" workload
   - Include "Windows application development" workload

## Quick Build & Run

### Option 1: Using Batch Files (Simplest)

```batch
# Build the application
build.bat

# Run the application
run.bat
```

### Option 2: Using PowerShell

```powershell
# Build the application
.\build.ps1

# Run the application
.\run.ps1
```

### Option 3: Using .NET CLI Directly

```bash
# Restore dependencies
dotnet restore

# Build the solution
dotnet build ClipTitle.sln -c Release

# Run the application
dotnet run --project ClipTitle/ClipTitle.csproj
```

### Option 4: Using Visual Studio 2022

1. Open `ClipTitle.sln` in Visual Studio
2. Press `Ctrl+Shift+B` to build
3. Press `F5` to run with debugging

## Build Output

After a successful build, the executable will be located at:
```
ClipTitle\bin\Release\net9.0-windows10.0.19041.0\ClipTitle.exe
```

## Troubleshooting Build Issues

### Missing .NET SDK

**Error:** `'dotnet' is not recognized as an internal or external command`

**Solution:** Install .NET 9 SDK from the link above

### NuGet Package Restore Failures

**Error:** `Unable to load the service index for source https://api.nuget.org/v3/index.json`

**Solution:** 
- Check internet connection
- Clear NuGet cache: `dotnet nuget locals all --clear`
- Retry: `dotnet restore --force`

### Missing Windows SDK

**Error:** `The Windows SDK version X was not found`

**Solution:**
- Install Visual Studio 2022 with Windows development workload
- Or install Windows SDK separately from: https://developer.microsoft.com/windows/downloads/windows-sdk/

### Asset Files Missing

**Warning:** `Assets\ClipTitle.ico not found`

**Solution:** 
- The app will build without icons
- To add icons, place image files in `ClipTitle\Assets\` folder
- See `ClipTitle\Assets\README.md` for required files

## Publishing a Standalone Executable

To create a self-contained executable that doesn't require .NET runtime:

```bash
# For 64-bit Windows
dotnet publish ClipTitle/ClipTitle.csproj -c Release -r win-x64 --self-contained -p:PublishSingleFile=true

# For ARM64 Windows
dotnet publish ClipTitle/ClipTitle.csproj -c Release -r win-arm64 --self-contained -p:PublishSingleFile=true
```

Published executable will be in:
```
ClipTitle\bin\Release\net9.0-windows10.0.19041.0\win-x64\publish\
```

## Creating an Installer (Optional)

For distribution, you can create an MSIX package:

1. In Visual Studio, right-click the project
2. Select "Publish" → "Create App Packages"
3. Choose "Sideloading" for direct distribution
4. Follow the wizard to create the package

## Verification

After building, verify the application:

1. **Check executable exists:**
   ```
   ClipTitle\bin\Release\net9.0-windows10.0.19041.0\ClipTitle.exe
   ```

2. **Run and check system tray:**
   - Application should minimize to system tray
   - Right-click tray icon for menu

3. **Test clipboard capture:**
   - Copy some text
   - Should see a notification

4. **Check settings:**
   - Right-click tray → Settings
   - Verify Ollama connection

## Development Tips

- Use Debug configuration for development: `dotnet build -c Debug`
- Enable detailed logging in `appsettings.json`
- Check logs in: `%LOCALAPPDATA%\ClipTitle\logs\`
- Use Visual Studio debugger for troubleshooting

## Need Help?

If you encounter issues not covered here:
1. Check the error message carefully
2. Ensure all prerequisites are installed
3. Try building in Debug mode for more information
4. Check if antivirus is blocking the build