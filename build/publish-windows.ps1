# Windows Build and Package Script for Digital Biochemical Simulator
# This script builds a self-contained, single-file Windows executable

param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [switch]$SelfContained = $true,
    [switch]$SingleFile = $true,
    [switch]$Trimmed = $false
)

$ErrorActionPreference = "Stop"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Digital Biochemical Simulator - Windows Build" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Configuration
$ProjectPath = "../src/DigitalBiochemicalSimulator/DigitalBiochemicalSimulator.csproj"
$OutputDir = "./output/windows-x64"
$Version = "1.0.0"

Write-Host "Configuration:" -ForegroundColor Yellow
Write-Host "  Project: $ProjectPath" -ForegroundColor Gray
Write-Host "  Output: $OutputDir" -ForegroundColor Gray
Write-Host "  Runtime: $Runtime" -ForegroundColor Gray
Write-Host "  Configuration: $Configuration" -ForegroundColor Gray
Write-Host "  Self-Contained: $SelfContained" -ForegroundColor Gray
Write-Host "  Single File: $SingleFile" -ForegroundColor Gray
Write-Host "  Trimmed: $Trimmed" -ForegroundColor Gray
Write-Host ""

# Clean previous builds
Write-Host "Cleaning previous builds..." -ForegroundColor Yellow
if (Test-Path $OutputDir) {
    Remove-Item -Path $OutputDir -Recurse -Force
}
New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null

# Build arguments
$publishArgs = @(
    "publish",
    $ProjectPath,
    "-c", $Configuration,
    "-r", $Runtime,
    "-o", $OutputDir,
    "--nologo"
)

if ($SelfContained) {
    $publishArgs += "--self-contained", "true"
} else {
    $publishArgs += "--self-contained", "false"
}

if ($SingleFile) {
    $publishArgs += "-p:PublishSingleFile=true"
    $publishArgs += "-p:IncludeNativeLibrariesForSelfExtract=true"
}

if ($Trimmed) {
    $publishArgs += "-p:PublishTrimmed=true"
    $publishArgs += "-p:TrimMode=link"
}

# Additional optimizations
$publishArgs += "-p:DebugType=None"
$publishArgs += "-p:DebugSymbols=false"

Write-Host "Building executable..." -ForegroundColor Yellow
Write-Host "Command: dotnet $($publishArgs -join ' ')" -ForegroundColor Gray
Write-Host ""

& dotnet $publishArgs

if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Build successful!" -ForegroundColor Green
Write-Host ""

# Copy documentation
Write-Host "Copying documentation..." -ForegroundColor Yellow
Copy-Item "../README.md" "$OutputDir/README.md" -Force
Copy-Item "../docs/GettingStarted.md" "$OutputDir/GettingStarted.md" -Force
Copy-Item "../docs/ParameterTuning.md" "$OutputDir/ParameterTuning.md" -Force
Copy-Item "../LICENSE" "$OutputDir/LICENSE.txt" -Force -ErrorAction SilentlyContinue

# Create distribution README
$distReadme = @"
# Digital Biochemical Simulator - Windows Distribution

Version: $Version
Build Date: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")

## Quick Start

1. Extract all files to a directory of your choice
2. Run DigitalBiochemicalSimulator.exe
3. See GettingStarted.md for detailed usage instructions

## System Requirements

- Windows 10 or later (64-bit)
- No .NET runtime required (self-contained)
- Minimum 4 GB RAM
- Recommended 8 GB RAM for large simulations

## Files Included

- DigitalBiochemicalSimulator.exe - Main executable
- README.md - Project overview
- GettingStarted.md - User guide
- ParameterTuning.md - Configuration guide
- LICENSE.txt - License information

## Documentation

Full documentation is available in the included markdown files:

- **GettingStarted.md** - Complete setup and usage guide
- **ParameterTuning.md** - How to configure simulations

## Support

- GitHub Issues: https://github.com/yourusername/codna/issues
- GitHub Discussions: https://github.com/yourusername/codna/discussions

## License

See LICENSE.txt for license information.

---

This is a self-contained executable with the .NET runtime embedded.
No additional dependencies are required.
"@

Set-Content -Path "$OutputDir/README-WINDOWS.txt" -Value $distReadme

# Display build information
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Build Complete" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Output directory: $OutputDir" -ForegroundColor Green
Write-Host ""

# List output files with sizes
Write-Host "Files created:" -ForegroundColor Yellow
Get-ChildItem -Path $OutputDir -Recurse -File | ForEach-Object {
    $size = if ($_.Length -gt 1MB) {
        "{0:N2} MB" -f ($_.Length / 1MB)
    } elseif ($_.Length -gt 1KB) {
        "{0:N2} KB" -f ($_.Length / 1KB)
    } else {
        "{0} bytes" -f $_.Length
    }
    Write-Host "  $($_.Name) - $size" -ForegroundColor Gray
}

Write-Host ""
Write-Host "The executable is ready for distribution!" -ForegroundColor Green
Write-Host ""
Write-Host "To create a ZIP package, run:" -ForegroundColor Yellow
Write-Host "  Compress-Archive -Path '$OutputDir\*' -DestinationPath './output/DigitalBiochemicalSimulator-v$Version-win-x64.zip'" -ForegroundColor Cyan
Write-Host ""
