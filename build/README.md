# Build Instructions

This directory contains scripts to build and package the Digital Biochemical Simulator for distribution.

## Quick Start

### Windows

**Option 1: PowerShell (Recommended)**
```powershell
cd build
.\publish-windows.ps1
```

**Option 2: Batch File**
```cmd
cd build
publish-windows.bat
```

### Linux

```bash
cd build
chmod +x publish-linux.sh
./publish-linux.sh
```

### macOS

```bash
cd build
chmod +x publish-macos.sh
./publish-macos.sh
```

## Output

All builds are created in the `build/output/` directory:
- `output/windows-x64/` - Windows executable and documentation
- `output/linux-x64/` - Linux executable and documentation
- `output/macos-x64/` - macOS executable and documentation

## Build Options

### Windows PowerShell Script

```powershell
.\publish-windows.ps1 [options]

Options:
  -Configuration <string>  Build configuration (default: Release)
  -Runtime <string>        Target runtime (default: win-x64)
  -SelfContained          Include .NET runtime (default: true)
  -SingleFile             Create single-file executable (default: true)
  -Trimmed                Enable assembly trimming (default: false)
```

**Examples:**

```powershell
# Standard release build (recommended)
.\publish-windows.ps1

# Debug build
.\publish-windows.ps1 -Configuration Debug

# Trimmed build (smaller size, may have compatibility issues)
.\publish-windows.ps1 -Trimmed

# Framework-dependent build (requires .NET runtime)
.\publish-windows.ps1 -SelfContained:$false
```

### Linux/macOS Shell Scripts

```bash
./publish-linux.sh [configuration]
./publish-macos.sh [configuration]

Arguments:
  configuration   Build configuration (default: Release)
```

**Examples:**

```bash
# Standard release build
./publish-linux.sh

# Debug build
./publish-linux.sh Debug
```

## Build Types

### Self-Contained Single-File (Default)

**Characteristics:**
- ✅ No .NET runtime required on target machine
- ✅ Single executable file
- ✅ Easy to distribute
- ❌ Larger file size (~60-80 MB)
- ✅ Recommended for end users

**Use when:**
- Distributing to users who may not have .NET installed
- Simplicity is important
- File size is not a concern

### Framework-Dependent

**Characteristics:**
- ❌ Requires .NET 6.0 runtime on target machine
- ✅ Smaller file size (~1-5 MB)
- ✅ Faster startup time
- ❌ More complex deployment

**Use when:**
- Distributing to developers who have .NET installed
- File size is critical
- Performance is critical

### Trimmed (Experimental)

**Characteristics:**
- ✅ Smaller file size (~30-50 MB)
- ⚠️ May have runtime issues with reflection
- ⚠️ Requires thorough testing

**Use when:**
- You've tested thoroughly
- File size is critical
- You understand the risks

## Creating Distribution Packages

### Windows ZIP

```powershell
cd build
.\publish-windows.ps1
Compress-Archive -Path '.\output\windows-x64\*' -DestinationPath '.\output\DigitalBiochemicalSimulator-v1.0.0-win-x64.zip'
```

### Linux Tarball

```bash
cd build
./publish-linux.sh
tar -czf ./output/DigitalBiochemicalSimulator-v1.0.0-linux-x64.tar.gz -C ./output/linux-x64 .
```

### macOS Tarball

```bash
cd build
./publish-macos.sh
tar -czf ./output/DigitalBiochemicalSimulator-v1.0.0-macos-x64.tar.gz -C ./output/macos-x64 .
```

## Build All Platforms

To build for all platforms at once (requires appropriate SDKs):

```bash
cd build
./publish-all.sh
```

This will create builds for Windows, Linux, and macOS in the output directory.

## System Requirements

### Build Machine

**Minimum:**
- .NET 6.0 SDK or later
- 2 GB free disk space
- 4 GB RAM

**For Cross-Platform Builds:**
- Windows: Can build for all platforms
- Linux: Can build for Linux, Windows (with Mono)
- macOS: Can build for all platforms

### Target Machine

See the README files in each platform's output directory for target system requirements.

## Output Structure

After building, each platform directory contains:

```
output/
├── windows-x64/
│   ├── DigitalBiochemicalSimulator.exe
│   ├── README.md
│   ├── README-WINDOWS.txt
│   ├── GettingStarted.md
│   ├── ParameterTuning.md
│   └── LICENSE.txt
├── linux-x64/
│   ├── DigitalBiochemicalSimulator
│   ├── README.md
│   ├── README-LINUX.txt
│   ├── GettingStarted.md
│   ├── ParameterTuning.md
│   └── LICENSE.txt
└── macos-x64/
    ├── DigitalBiochemicalSimulator
    ├── README.md
    ├── README-MACOS.txt
    ├── GettingStarted.md
    ├── ParameterTuning.md
    └── LICENSE.txt
```

## Troubleshooting

### Build Fails with "SDK Not Found"

**Solution:** Install .NET 6.0 SDK or later from https://dotnet.microsoft.com/download

### PowerShell Script Won't Run

**Error:** "running scripts is disabled on this system"

**Solution:**
```powershell
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
```

### Linux/macOS Script Won't Run

**Error:** "Permission denied"

**Solution:**
```bash
chmod +x publish-linux.sh
chmod +x publish-macos.sh
```

### Large File Size

**Problem:** Executable is larger than expected

**Solutions:**
1. Use `-Trimmed` flag (Windows PowerShell)
2. Build framework-dependent instead of self-contained
3. This is normal for self-contained builds (~60-80 MB)

### macOS Security Warning

**Problem:** "App cannot be opened because it is from an unidentified developer"

**Solutions:**
1. Right-click > Open (allows bypass)
2. System Preferences > Security & Privacy > "Open Anyway"
3. Sign the executable with Apple Developer certificate (for distribution)

### Missing Documentation Files

**Problem:** README or docs not included in output

**Solution:** Ensure all documentation exists in the repository:
- README.md (root)
- docs/GettingStarted.md
- docs/ParameterTuning.md
- LICENSE (root)

## Advanced Topics

### Custom Version Numbers

Edit the version in the build script:

**Windows PowerShell:**
```powershell
$Version = "1.0.0"  # Change this line
```

**Linux/macOS:**
```bash
VERSION="1.0.0"  # Change this line
```

Or edit the .csproj file:
```xml
<Version>1.0.0</Version>
```

### Code Signing

#### Windows

```powershell
signtool sign /f certificate.pfx /p password /t http://timestamp.digicert.com DigitalBiochemicalSimulator.exe
```

#### macOS

```bash
codesign --sign "Developer ID Application: Your Name" DigitalBiochemicalSimulator
```

### Creating Installers

#### Windows (NSIS)

1. Install NSIS: https://nsis.sourceforge.io/
2. Create installer script (see examples online)
3. Compile with `makensis installer.nsi`

#### macOS (DMG)

```bash
hdiutil create -volname "Digital Biochemical Simulator" -srcfolder ./output/macos-x64 -ov -format UDZO DigitalBiochemicalSimulator-v1.0.0.dmg
```

#### Linux (AppImage)

See https://appimage.org/ for creating AppImage packages.

## Continuous Integration

### GitHub Actions Example

```yaml
name: Build and Release

on:
  push:
    tags:
      - 'v*'

jobs:
  build:
    runs-on: ${{ matrix.os }}
    strategy:
      matrix:
        os: [windows-latest, ubuntu-latest, macos-latest]

    steps:
    - uses: actions/checkout@v2
    - name: Setup .NET
      uses: actions/setup-dotnet@v1
      with:
        dotnet-version: 6.0.x

    - name: Build
      run: |
        cd build
        ./publish-${{ matrix.os }}.sh
```

## Questions?

See the main README.md or open an issue on GitHub.
