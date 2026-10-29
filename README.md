# Unit Converter

A small, always-on-top unit converter widget for Windows 10 and 11.

- **Unit categories:** length, mass, volume, temperature, pressure, flow, density, area, speed, time and data (decimal KB/MB/GB and binary KiB/MiB/GiB).
- **Mass ↔ Volume:** converts kg, lb and so on to L, gal and so on using a density you type, or pick a material preset (water, diesel, glycol and others).
- **Multiplier:** an optional factor applied to every unit result (1 = off). When it isn't 1, the status line says so.
- **Scaling:** linear scaling like a PLC SCP/SCL/NORM_X+SCALE_X. Set input min/max (or pick a preset such as 4–20 mA, 0–10 V or Siemens 0–27648) and output min/max, then type a value on either side. The status line shows the slope and offset (`Out = 6.25 × In − 25`) and warns when the input is outside its range.
- **Two-way:** type in either box and the other updates as you type.
- **Stays out of the way:** lives in the notification area. `Ctrl+Alt+U` shows or hides it from anywhere.
- **Remembers** its position, the last category and the units you picked for each category.
- Optional **Start with Windows** (tray menu).

## Keyboard

| Keys | Action |
| --- | --- |
| `Ctrl+Alt+U` | Show / hide the widget (works from any app) |
| `Ctrl+1` … `Ctrl+9` | Switch mode (1 = Scaling, 2 = Mass ↔ Volume, 3 = Length, …) |
| `Ctrl+R` | Swap units (in Scaling, swaps the input and output ranges) |
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

- `src/UnitConverter.Core` holds units, conversion, mass/volume, linear scaling, input parsing and number formatting. It has no UI dependencies.
- `src/UnitConverter.App` is the WPF widget, tray icon, global hotkey and settings (`%APPDATA%\UnitConverter\settings.json`).
- `tests/UnitConverter.Tests` contains xUnit tests for every category, mass/volume, scaling, round trips, parsing and formatting.

To add a unit, add one line to `Catalog.cs` with its factor relative to the category's base unit.
