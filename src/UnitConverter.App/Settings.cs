using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace UnitConverter.App;

/// <summary>User preferences, stored as JSON in %APPDATA%\UnitConverter\settings.json.</summary>
public sealed class Settings
{
    private static readonly string FilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "UnitConverter", "settings.json");

    public double? Left { get; set; }
    public double? Top { get; set; }
    public bool Topmost { get; set; } = true;
    public string Category { get; set; } = "Length";

    /// <summary>Last chosen [from, to] unit symbols, keyed by category name.</summary>
    public Dictionary<string, string[]> Units { get; set; } = new();

    /// <summary>Applied to every unit conversion result; 1 means off.</summary>
    public double Multiplier { get; set; } = 1;

    /// <summary>Density for mass/volume conversions, always stored in kg/L.</summary>
    public double DensityKgPerL { get; set; } = 1;
    public string DensityUnit { get; set; } = "kg/L";

    public double ScaleInMin { get; set; } = 4;
    public double ScaleInMax { get; set; } = 20;
    public double ScaleOutMin { get; set; } = 0;
    public double ScaleOutMax { get; set; } = 100;

    public bool TrayHintShown { get; set; }

    public static Settings Load()
    {
        try
        {
            if (File.Exists(FilePath))
                return JsonSerializer.Deserialize<Settings>(File.ReadAllText(FilePath)) ?? new Settings();
        }
        catch (Exception)
        {
            // A corrupt or unreadable settings file should never stop the app from starting.
        }
        return new Settings();
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception)
        {
            // Losing preferences is better than crashing on exit.
        }
    }
}
