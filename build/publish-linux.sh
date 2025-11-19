#!/bin/bash
# Linux Build and Package Script for Digital Biochemical Simulator
# This script builds a self-contained, single-file Linux executable

set -e

# Configuration
CONFIGURATION="${1:-Release}"
RUNTIME="linux-x64"
PROJECT_PATH="../src/DigitalBiochemicalSimulator/DigitalBiochemicalSimulator.csproj"
OUTPUT_DIR="./output/linux-x64"
VERSION="1.0.0"

echo "========================================"
echo "Digital Biochemical Simulator - Linux Build"
echo "========================================"
echo ""

echo "Configuration:"
echo "  Project: $PROJECT_PATH"
echo "  Output: $OUTPUT_DIR"
echo "  Runtime: $RUNTIME"
echo "  Configuration: $CONFIGURATION"
echo ""

# Clean previous builds
echo "Cleaning previous builds..."
rm -rf "$OUTPUT_DIR"
mkdir -p "$OUTPUT_DIR"

# Build executable
echo "Building executable..."
dotnet publish "$PROJECT_PATH" \
    -c "$CONFIGURATION" \
    -r "$RUNTIME" \
    -o "$OUTPUT_DIR" \
    --self-contained true \
    -p:PublishSingleFile=true \
    -p:IncludeNativeLibrariesForSelfExtract=true \
    -p:DebugType=None \
    -p:DebugSymbols=false \
    --nologo

if [ $? -ne 0 ]; then
    echo "Build failed!"
    exit 1
fi

echo ""
echo "Build successful!"
echo ""

# Copy documentation
echo "Copying documentation..."
cp ../README.md "$OUTPUT_DIR/README.md"
cp ../docs/GettingStarted.md "$OUTPUT_DIR/GettingStarted.md"
cp ../docs/ParameterTuning.md "$OUTPUT_DIR/ParameterTuning.md"
cp ../LICENSE "$OUTPUT_DIR/LICENSE.txt" 2>/dev/null || true

# Create distribution README
cat > "$OUTPUT_DIR/README-LINUX.txt" << EOF
# Digital Biochemical Simulator - Linux Distribution

Version: $VERSION
Build Date: $(date '+%Y-%m-%d %H:%M:%S')

## Quick Start

1. Extract all files to a directory of your choice
2. Make the executable runnable: chmod +x DigitalBiochemicalSimulator
3. Run: ./DigitalBiochemicalSimulator
4. See GettingStarted.md for detailed usage instructions

## System Requirements

- Linux (64-bit)
- No .NET runtime required (self-contained)
- Minimum 4 GB RAM
- Recommended 8 GB RAM for large simulations

## Files Included

- DigitalBiochemicalSimulator - Main executable
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
EOF

# Make executable
chmod +x "$OUTPUT_DIR/DigitalBiochemicalSimulator"

# Display build information
echo "========================================"
echo "Build Complete"
echo "========================================"
echo ""
echo "Output directory: $OUTPUT_DIR"
echo ""

echo "Files created:"
ls -lh "$OUTPUT_DIR" | awk '{if (NR>1) print "  " $9 " - " $5}'

echo ""
echo "The executable is ready for distribution!"
echo ""
echo "To create a tarball, run:"
echo "  tar -czf ./output/DigitalBiochemicalSimulator-v$VERSION-linux-x64.tar.gz -C $OUTPUT_DIR ."
echo ""
