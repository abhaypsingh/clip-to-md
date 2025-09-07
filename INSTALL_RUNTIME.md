# Install Windows App Runtime - Required for ClipTitle

ClipTitle requires the Windows App Runtime to be installed on your system. Without it, the application won't launch.

## Quick Installation

### Option 1: Direct Download Link (Recommended)
1. **Download the installer:**
   - [Click here to download Windows App Runtime 1.6](https://aka.ms/windowsappsdk/1.6/latest/windowsappruntimeinstall-x64.exe)
   - Or copy this link: `https://aka.ms/windowsappsdk/1.6/latest/windowsappruntimeinstall-x64.exe`

2. **Run the downloaded installer:**
   - Double-click `windowsappruntimeinstall-x64.exe`
   - If prompted by Windows security, click "Run anyway"
   - The installation will complete automatically

3. **Launch ClipTitle:**
   - Run `run-cliptitle.cmd` from this folder
   - Or navigate to: `ClipTitle\bin\Release\net9.0-windows10.0.19041.0\win-x64\ClipTitle.exe`

### Option 2: Using the Provided Script
Run the `install-runtime.cmd` file in this folder (may require administrator privileges)

## Verification

After installation, you can verify it's installed by:
1. Opening PowerShell
2. Running: `Get-AppxPackage | Where-Object {$_.Name -like "*WindowsAppRuntime*"}`

## If ClipTitle Still Doesn't Launch

You may also need:
- **Visual C++ Redistributables**: [Download here](https://aka.ms/vs/17/release/vc_redist.x64.exe)
- **.NET 9 Desktop Runtime** (should already be included): [Download here](https://dotnet.microsoft.com/download/dotnet/9.0)

## System Requirements
- Windows 10 version 1809 or later
- Windows 11 (any version)

## Troubleshooting

If the app still doesn't launch after installing the runtime:
1. Restart your computer
2. Run ClipTitle as administrator
3. Check Windows Event Viewer for error messages
4. Ensure Windows is up to date

## Manual Installation Status Check

Run this PowerShell command to check if the runtime is installed:
```powershell
Get-AppxPackage -Name Microsoft.WindowsAppRuntime.* | Select Name, Version
```

If installed, you should see something like:
```
Name                              Version
----                              -------
Microsoft.WindowsAppRuntime.1.6   6001.xxx.xxx.0
```