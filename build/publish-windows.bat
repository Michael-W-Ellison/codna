@echo off
REM Simple batch wrapper for PowerShell build script

echo ========================================
echo Digital Biochemical Simulator - Build
echo ========================================
echo.

powershell.exe -ExecutionPolicy Bypass -File "%~dp0publish-windows.ps1" %*

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo Build failed! See errors above.
    pause
    exit /b %ERRORLEVEL%
)

echo.
echo Build completed successfully!
pause
