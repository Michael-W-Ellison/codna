#!/bin/bash
# Master Build Script - Builds for all platforms
# Builds Windows, Linux, and macOS executables

set -e

CONFIGURATION="${1:-Release}"
VERSION="1.0.0"

echo "========================================"
echo "Digital Biochemical Simulator"
echo "Multi-Platform Build"
echo "========================================"
echo ""
echo "Building for all platforms..."
echo "Configuration: $CONFIGURATION"
echo "Version: $VERSION"
echo ""

# Function to build a platform
build_platform() {
    local platform=$1
    local script=$2

    echo "----------------------------------------"
    echo "Building for $platform..."
    echo "----------------------------------------"

    if [ -f "$script" ]; then
        chmod +x "$script"
        ./"$script" "$CONFIGURATION"
        echo ""
        echo "✓ $platform build complete"
    else
        echo "⚠ Warning: $script not found, skipping $platform"
    fi
    echo ""
}

# Build all platforms
build_platform "Linux" "publish-linux.sh"
build_platform "macOS" "publish-macos.sh"

# Windows build (if on Windows with PowerShell, or using Wine/Mono)
if command -v powershell.exe &> /dev/null; then
    echo "----------------------------------------"
    echo "Building for Windows..."
    echo "----------------------------------------"
    powershell.exe -ExecutionPolicy Bypass -File publish-windows.ps1 -Configuration "$CONFIGURATION"
    echo ""
    echo "✓ Windows build complete"
    echo ""
elif [ -f "./output/windows-x64/DigitalBiochemicalSimulator.exe" ]; then
    echo "⚠ Note: Windows build already exists, or run this script on Windows"
    echo ""
else
    # Try using dotnet publish directly for Windows
    echo "----------------------------------------"
    echo "Building for Windows (cross-compile)..."
    echo "----------------------------------------"

    PROJECT_PATH="../src/DigitalBiochemicalSimulator/DigitalBiochemicalSimulator.csproj"
    OUTPUT_DIR="./output/windows-x64"

    mkdir -p "$OUTPUT_DIR"

    dotnet publish "$PROJECT_PATH" \
        -c "$CONFIGURATION" \
        -r "win-x64" \
        -o "$OUTPUT_DIR" \
        --self-contained true \
        -p:PublishSingleFile=true \
        -p:IncludeNativeLibrariesForSelfExtract=true \
        -p:DebugType=None \
        -p:DebugSymbols=false \
        --nologo

    # Copy documentation
    cp ../README.md "$OUTPUT_DIR/README.md"
    cp ../docs/GettingStarted.md "$OUTPUT_DIR/GettingStarted.md"
    cp ../docs/ParameterTuning.md "$OUTPUT_DIR/ParameterTuning.md"
    cp ../LICENSE "$OUTPUT_DIR/LICENSE.txt" 2>/dev/null || true

    echo ""
    echo "✓ Windows build complete"
    echo ""
fi

# Create distribution packages
echo "========================================"
echo "Creating Distribution Packages"
echo "========================================"
echo ""

mkdir -p ./output/packages

# Linux tarball
if [ -d "./output/linux-x64" ]; then
    echo "Creating Linux tarball..."
    tar -czf "./output/packages/DigitalBiochemicalSimulator-v$VERSION-linux-x64.tar.gz" \
        -C ./output/linux-x64 .
    echo "✓ Created: DigitalBiochemicalSimulator-v$VERSION-linux-x64.tar.gz"
fi

# macOS tarball
if [ -d "./output/macos-x64" ]; then
    echo "Creating macOS tarball..."
    tar -czf "./output/packages/DigitalBiochemicalSimulator-v$VERSION-macos-x64.tar.gz" \
        -C ./output/macos-x64 .
    echo "✓ Created: DigitalBiochemicalSimulator-v$VERSION-macos-x64.tar.gz"
fi

# Windows ZIP
if [ -d "./output/windows-x64" ]; then
    echo "Creating Windows ZIP..."
    if command -v zip &> /dev/null; then
        cd ./output/windows-x64
        zip -r "../packages/DigitalBiochemicalSimulator-v$VERSION-win-x64.zip" .
        cd ../..
        echo "✓ Created: DigitalBiochemicalSimulator-v$VERSION-win-x64.zip"
    else
        echo "⚠ Warning: 'zip' command not found. Windows package not created."
        echo "   Install zip or create package manually on Windows."
    fi
fi

echo ""
echo "========================================"
echo "Build Summary"
echo "========================================"
echo ""

# Count builds
build_count=0
[ -d "./output/linux-x64" ] && ((build_count++))
[ -d "./output/macos-x64" ] && ((build_count++))
[ -d "./output/windows-x64" ] && ((build_count++))

echo "Platforms built: $build_count/3"
echo ""

# List all output directories and packages
if [ -d "./output" ]; then
    echo "Output directories:"
    ls -d ./output/*-x64 2>/dev/null | while read dir; do
        platform=$(basename "$dir")
        size=$(du -sh "$dir" | cut -f1)
        echo "  $platform - $size"
    done
    echo ""
fi

if [ -d "./output/packages" ]; then
    echo "Distribution packages:"
    ls -lh ./output/packages/ | grep -v '^total' | awk '{print "  " $9 " - " $5}'
    echo ""
fi

echo "========================================"
echo "All Builds Complete!"
echo "========================================"
echo ""
echo "Packages are ready for distribution in:"
echo "  ./output/packages/"
echo ""
