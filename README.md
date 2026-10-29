# Unit Converter

A small, always-on-top unit converter widget for Windows 10 and 11.

- **8 categories:** length, mass, temperature, volume, area, speed, time and data (decimal KB/MB/GB and binary KiB/MiB/GiB).
- **Two-way:** type in either box and the other updates as you type.
- **Stays out of the way:** lives in the notification area. `Ctrl+Alt+U` shows or hides it from anywhere.
- **Remembers** its position, the last category and the units you picked for each category.
- Optional **Start with Windows** (tray menu).

## Keyboard

| Keys | Action |
| --- | --- |
| `Ctrl+Alt+U` | Show / hide the widget (works from any app) |
| `Ctrl+1` … `Ctrl+8` | Switch category |
| `Ctrl+R` | Swap units |
| `Ctrl+Shift+C` | Copy the converted value |
| `Esc` | Hide to tray |

Drag the widget by any empty area. The pin button toggles always-on-top. To quit, right-click the tray icon and choose **Exit**.

## Download

Every push to `main` builds `UnitConverter.exe` in GitHub Actions (see the run's **Artifacts**). Pushing a tag such as `v1.0.0` also attaches the exe to a GitHub release. It is a single self-contained file, so no .NET install is needed.

## Build it yourself

Requires the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) on Windows.

```powershell
dotnet test tests/UnitConverter.Tests
dotnet run --project src/UnitConverter.App

# Single-file exe in .\publish
dotnet publish src/UnitConverter.App -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish
```

## Project layout

- `src/UnitConverter.Core` holds units, conversion, input parsing and number formatting. It has no UI dependencies.
- `src/UnitConverter.App` is the WPF widget, tray icon, global hotkey and settings (`%APPDATA%\UnitConverter\settings.json`).
- `tests/UnitConverter.Tests` contains xUnit tests for every category, round trips, parsing and formatting.

To add a unit, add one line to `Catalog.cs` with its factor relative to the category's base unit.
