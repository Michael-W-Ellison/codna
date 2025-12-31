# Building the Digital Biochemical Simulator

This guide explains how to build executable distributions of the Digital Biochemical Simulator.

## Prerequisites

### Required

- **.NET 6.0 SDK or later**
  - Download from: https://dotnet.microsoft.com/download
  - Verify installation: `dotnet --version`

### Optional

- **PowerShell 7+** (for Windows builds on non-Windows systems)
- **zip** utility (for creating Windows ZIP packages on Linux/macOS)
- **tar** utility (usually pre-installed on Linux/macOS)

## Quick Start

Choose the appropriate script for your platform:

### Windows

```powershell
cd build
.\publish-windows.ps1
```

### Linux

```bash
cd build
./publish-linux.sh
```

### macOS

```bash
cd build
./publish-macos.sh
```

### All Platforms

```bash
cd build
./publish-all.sh
```

## What Gets Built

Each build creates a **self-contained, single-file executable** that includes:

- ✅ The entire .NET runtime embedded
- ✅ All application dependencies
- ✅ No installation required on target machine
- ✅ Documentation files (README, guides, license)

**File sizes:**
- Windows: ~60-80 MB
- Linux: ~60-70 MB
- macOS: ~60-70 MB

## Build Output

All builds are placed in `build/output/`:

```
build/output/
├── windows-x64/
│   ├── DigitalBiochemicalSimulator.exe
│   ├── README.md
│   ├── README-WINDOWS.txt
│   ├── GettingStarted.md
│   ├── ParameterTuning.md
│   └── LICENSE.txt
├── linux-x64/
│   ├── DigitalBiochemicalSimulator (executable)
│   ├── README.md
│   ├── README-LINUX.txt
│   ├── GettingStarted.md
│   ├── ParameterTuning.md
│   └── LICENSE.txt
├── macos-x64/
│   ├── DigitalBiochemicalSimulator (executable)
│   ├── README.md
│   ├── README-MACOS.txt
│   ├── GettingStarted.md
│   ├── ParameterTuning.md
│   └── LICENSE.txt
└── packages/ (if using publish-all.sh)
    ├── DigitalBiochemicalSimulator-v1.0.0-win-x64.zip
    ├── DigitalBiochemicalSimulator-v1.0.0-linux-x64.tar.gz
    └── DigitalBiochemicalSimulator-v1.0.0-macos-x64.tar.gz
```

## Distribution

### Creating Packages

#### Windows ZIP (PowerShell)

```powershell
cd build
.\publish-windows.ps1
Compress-Archive -Path '.\output\windows-x64\*' -DestinationPath '.\output\DigitalBiochemicalSimulator-v1.0.0-win-x64.zip'
```

#### Windows ZIP (Linux/macOS)

```bash
cd build/output/windows-x64
zip -r ../DigitalBiochemicalSimulator-v1.0.0-win-x64.zip .
```

#### Linux Tarball

```bash
cd build
./publish-linux.sh
tar -czf ./output/DigitalBiochemicalSimulator-v1.0.0-linux-x64.tar.gz -C ./output/linux-x64 .
```

#### macOS Tarball

```bash
cd build
./publish-macos.sh
tar -czf ./output/DigitalBiochemicalSimulator-v1.0.0-macos-x64.tar.gz -C ./output/macos-x64 .
```

### Publishing to GitHub Releases

1. **Create packages:**
   ```bash
   cd build
   ./publish-all.sh
   ```

2. **Create GitHub release:**
   - Go to repository → Releases → Draft new release
   - Create tag (e.g., `v1.0.0`)
   - Upload packages from `build/output/packages/`
   - Write release notes
   - Publish release

3. **Users can download:**
   - Windows: Download ZIP, extract, run `.exe`
   - Linux: Download tarball, extract, `chmod +x`, run
   - macOS: Download tarball, extract, `chmod +x`, run

## Advanced Build Options

### Framework-Dependent Build

Creates smaller executables that require .NET runtime on target machine:

```powershell
# Windows
.\publish-windows.ps1 -SelfContained:$false
```

```bash
# Linux (edit script to remove --self-contained)
./publish-linux.sh
```

**Advantages:**
- Smaller file size (~1-5 MB)
- Faster startup

**Disadvantages:**
- Requires .NET 6.0 runtime on target machine
- More complex deployment

### Trimmed Build (Experimental)

Reduces file size by removing unused code:

```powershell
.\publish-windows.ps1 -Trimmed
```

**Warning:** May cause runtime issues with reflection. Test thoroughly!

### Debug Build

```powershell
.\publish-windows.ps1 -Configuration Debug
```

```bash
./publish-linux.sh Debug
```

## Testing the Build

After building, test the executable:

### Windows

```powershell
cd build/output/windows-x64
.\DigitalBiochemicalSimulator.exe
```

### Linux/macOS

```bash
cd build/output/linux-x64  # or macos-x64
./DigitalBiochemicalSimulator
```

## Continuous Integration

### GitHub Actions

Create `.github/workflows/build.yml`:

```yaml
name: Build Executables

on:
  push:
    tags:
      - 'v*'
  workflow_dispatch:

jobs:
  build-windows:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v3
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '6.0.x'
      - name: Build Windows
        run: |
          cd build
          .\publish-windows.ps1
      - name: Upload artifact
        uses: actions/upload-artifact@v3
        with:
          name: windows-x64
          path: build/output/windows-x64

  build-linux:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '6.0.x'
      - name: Build Linux
        run: |
          cd build
          ./publish-linux.sh
      - name: Upload artifact
        uses: actions/upload-artifact@v3
        with:
          name: linux-x64
          path: build/output/linux-x64

  build-macos:
    runs-on: macos-latest
    steps:
      - uses: actions/checkout@v3
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '6.0.x'
      - name: Build macOS
        run: |
          cd build
          ./publish-macos.sh
      - name: Upload artifact
        uses: actions/upload-artifact@v3
        with:
          name: macos-x64
          path: build/output/macos-x64

  create-release:
    needs: [build-windows, build-linux, build-macos]
    runs-on: ubuntu-latest
    steps:
      - name: Download all artifacts
        uses: actions/download-artifact@v3
      - name: Create packages
        run: |
          zip -r DigitalBiochemicalSimulator-win-x64.zip windows-x64/
          tar -czf DigitalBiochemicalSimulator-linux-x64.tar.gz linux-x64/
          tar -czf DigitalBiochemicalSimulator-macos-x64.tar.gz macos-x64/
      - name: Create Release
        uses: softprops/action-gh-release@v1
        with:
          files: |
            DigitalBiochemicalSimulator-win-x64.zip
            DigitalBiochemicalSimulator-linux-x64.tar.gz
            DigitalBiochemicalSimulator-macos-x64.tar.gz
```

## Troubleshooting

### Common Issues

#### 1. "dotnet: command not found"

**Solution:** Install .NET SDK from https://dotnet.microsoft.com/download

#### 2. "Scripts are disabled on this system"

**Platform:** Windows PowerShell

**Solution:**
```powershell
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
```

#### 3. "Permission denied"

**Platform:** Linux/macOS

**Solution:**
```bash
chmod +x publish-linux.sh publish-macos.sh publish-all.sh
```

#### 4. Large file size

**Reason:** Self-contained builds include the entire .NET runtime

**Solutions:**
- This is normal for self-contained builds
- Use `-Trimmed` flag (may cause issues)
- Build framework-dependent version

#### 5. Missing documentation files

**Solution:** Ensure these files exist in repository:
- `README.md` (root)
- `docs/GettingStarted.md`
- `docs/ParameterTuning.md`
- `LICENSE` (root)

#### 6. Build succeeds but executable won't run

**Windows:** May need Visual C++ Redistributable
**macOS:** Security settings may block unsigned apps
**Linux:** May need to install missing system libraries

## Platform-Specific Notes

### Windows

- Builds create `.exe` files
- Can be run directly by double-clicking
- No additional dependencies required (self-contained)
- Antivirus may flag unsigned executables

### Linux

- Builds create executable binaries (no extension)
- Must mark as executable: `chmod +x`
- No additional dependencies required (self-contained)
- Works on most modern distributions

### macOS

- Builds create executable binaries (no extension)
- Must mark as executable: `chmod +x`
- Requires macOS 10.15 (Catalina) or later
- First run requires security approval (unsigned)
- Right-click → Open to bypass security warning

## Version Management

To update version numbers:

1. **Edit .csproj:**
   ```xml
   <Version>1.0.0</Version>
   ```

2. **Edit build scripts:**
   - PowerShell: `$Version = "1.0.0"`
   - Shell: `VERSION="1.0.0"`

3. **Rebuild:**
   ```bash
   ./publish-all.sh
   ```

## Code Signing (Optional)

### Windows

Requires code signing certificate:

```powershell
signtool sign /f certificate.pfx /p password /tr http://timestamp.digicert.com /td sha256 /fd sha256 DigitalBiochemicalSimulator.exe
```

### macOS

Requires Apple Developer account:

```bash
codesign --sign "Developer ID Application: Your Name (TEAM_ID)" DigitalBiochemicalSimulator
```

### Linux

Code signing is less common on Linux. Use package signing instead.

## Next Steps

After building:

1. **Test the executable** on target platforms
2. **Create distribution packages** (ZIP/tarball)
3. **Upload to GitHub Releases** or other distribution platform
4. **Update documentation** with download links
5. **Create installation instructions** for end users

## Questions?

See the main documentation:
- [README.md](../README.md) - Project overview
- [GettingStarted.md](../docs/GettingStarted.md) - Usage guide
- [Build README](README.md) - Build system documentation
