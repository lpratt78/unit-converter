@echo off
rem Double-click to build UnitConverter.exe into the "publish" folder and start it.
cd /d "%~dp0"

echo Closing any running Unit Converter...
taskkill /im UnitConverter.exe /f >nul 2>&1

echo Building... (the first build takes a minute)
dotnet publish src\UnitConverter.App -c Release -r win-x64 --self-contained true ^
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish
if errorlevel 1 (
  echo.
  echo Build failed. Copy the messages above and send them to Claude.
  pause
  exit /b 1
)

echo.
echo Done: %~dp0publish\UnitConverter.exe
start "" "publish\UnitConverter.exe"
explorer "publish"
timeout /t 5
