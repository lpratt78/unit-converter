# Unit Converter

A small, always-on-top unit converter widget for Windows 10 and 11.

- **Unit categories:** length, mass, volume, temperature, pressure, flow, density, area, speed, time and data (decimal KB/MB/GB and binary KiB/MiB/GiB).
- **Mass ↔ Volume:** converts gallons, litres and so on to pounds, kilograms and so on using a weight multiplier (default 8.34 lb/gal for water). Type your own, change its unit (lb/gal, kg/L, SG…), or pick a preset such as liquid egg (8.64 lb/gal), milk or diesel.
- **Scaling:** linear scaling like a PLC SCP/SCL/NORM_X+SCALE_X. Set the Input 1 range (or pick a preset such as 4–20 mA, 0–10 V or Siemens 0–27648) and the Input 2 range, then type Input 1 or drag the slider to get the scaled Input 2 value (you can also type Input 2 to go the other way). The slider shows the percent of span, and the status line shows the formula (`Input 2 = 6.25 × Input 1 − 25`) and warns when Input 1 is outside its range.
- **Two-way:** type in either box and the other updates as you type.
- **Stays out of the way:** lives in the notification area. `Ctrl+Alt+U` shows or hides it from anywhere.
- **Remembers** its position, the last category and the units you picked for each category.
- Optional **Start with Windows** (tray menu).

## Web app (PWA)

`pwa/` holds a browser version with the same Scaling, Mass ↔ Volume and unit conversions. It works offline once loaded and can be installed from Chrome or Edge ("Install as an app"), on Windows or a phone. It is published to GitHub Pages at https://lpratt78.github.io/unit-converter/ on every push to `main` that changes `pwa/`. Bump `VERSION` in `pwa/sw.js` whenever you change a file so installed copies update.

## Keyboard

| Keys | Action |
| --- | --- |
| `Ctrl+Alt+U` | Show / hide the widget (works from any app) |
| `Ctrl+1` … `Ctrl+9` | Switch mode (1 = Scaling, 2 = Mass ↔ Volume, 3 = Length, …) |
| `Ctrl+R` | Swap units (in Scaling, swaps the Input 1 and Input 2 ranges) |
| `Ctrl+Shift+C` | Copy the bottom line (e.g. `1 gal = 8.34 lb` or the scaling formula) |
| `Esc` | Hide to tray |

Drag the widget by any empty area. The pin button toggles always-on-top. To quit, right-click the tray icon and choose **Exit**.

## Download

Every push to `main` builds `UnitConverter.exe` in GitHub Actions (see the run's **Artifacts**). Pushing a tag such as `v1.0.0` also attaches the exe to a GitHub release. It is a single self-contained file, so no .NET install is needed.

## Build it yourself

Requires the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) on Windows.

**Easiest:** double-click `Build UnitConverter.exe.bat`. It closes any running copy, builds `publish\UnitConverter.exe`, starts it and opens the folder. Run it again after each update.

```powershell
dotnet test tests/UnitConverter.Tests
dotnet run --project src/UnitConverter.App

# Single-file exe in .\publish
dotnet publish src/UnitConverter.App -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish
```

## Project layout

- `src/UnitConverter.Core` holds units, conversion, mass/volume, linear scaling, input parsing and number formatting. It has no UI dependencies.
- `src/UnitConverter.App` is the WPF widget, tray icon, global hotkey and settings (`%APPDATA%\UnitConverter\settings.json`).
- `tests/UnitConverter.Tests` contains xUnit tests for every category, mass/volume, scaling, round trips, parsing and formatting.

To add a unit, add one line to `Catalog.cs` with its factor relative to the category's base unit.
