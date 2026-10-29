namespace UnitConverter.Core;

/// <summary>
/// All supported categories and units. Factors are exact definitions where one exists
/// (e.g. 1 in = 0.0254 m, 1 lb = 0.45359237 kg).
/// </summary>
public static class Catalog
{
    public static Category Length { get; } = new("Length",
    [
        Unit.Linear("Micrometre", "µm", 1e-6),
        Unit.Linear("Millimetre", "mm", 1e-3),
        Unit.Linear("Centimetre", "cm", 1e-2),
        Unit.Linear("Metre", "m", 1),
        Unit.Linear("Kilometre", "km", 1e3),
        Unit.Linear("Inch", "in", 0.0254),
        Unit.Linear("Foot", "ft", 0.3048),
        Unit.Linear("Yard", "yd", 0.9144),
        Unit.Linear("Mile", "mi", 1609.344),
        Unit.Linear("Nautical mile", "nmi", 1852),
    ], "cm", "in");

    public static Category Mass { get; } = new("Mass",
    [
        Unit.Linear("Milligram", "mg", 1e-6),
        Unit.Linear("Gram", "g", 1e-3),
        Unit.Linear("Kilogram", "kg", 1),
        Unit.Linear("Tonne", "t", 1e3),
        Unit.Linear("Ounce", "oz", 0.028349523125),
        Unit.Linear("Pound", "lb", 0.45359237),
        Unit.Linear("Stone", "st", 6.35029318),
        Unit.Linear("US ton", "ton", 907.18474),
    ], "kg", "lb");

    // Base unit: kelvin.
    public static Category Temperature { get; } = new("Temperature",
    [
        new Unit("Celsius", "°C", c => c + 273.15, k => k - 273.15),
        new Unit("Fahrenheit", "°F", f => (f - 32) * 5 / 9 + 273.15, k => (k - 273.15) * 9 / 5 + 32),
        new Unit("Kelvin", "K", k => k, k => k),
    ], "°C", "°F");

    // Base unit: litre.
    public static Category Volume { get; } = new("Volume",
    [
        Unit.Linear("Millilitre", "mL", 1e-3),
        Unit.Linear("Litre", "L", 1),
        Unit.Linear("Cubic metre", "m³", 1e3),
        Unit.Linear("Teaspoon (US)", "tsp", 0.00492892159375),
        Unit.Linear("Tablespoon (US)", "tbsp", 0.01478676478125),
        Unit.Linear("Fluid ounce (US)", "fl oz", 0.0295735295625),
        Unit.Linear("Cup (US)", "cup", 0.2365882365),
        Unit.Linear("Pint (US)", "pt", 0.473176473),
        Unit.Linear("Quart (US)", "qt", 0.946352946),
        Unit.Linear("Gallon (US)", "gal", 3.785411784),
        Unit.Linear("Gallon (UK)", "gal UK", 4.54609),
        Unit.Linear("Cubic inch", "in³", 0.016387064),
        Unit.Linear("Cubic foot", "ft³", 28.316846592),
    ], "L", "gal");

    // Base unit: square metre.
    public static Category Area { get; } = new("Area",
    [
        Unit.Linear("Square centimetre", "cm²", 1e-4),
        Unit.Linear("Square metre", "m²", 1),
        Unit.Linear("Hectare", "ha", 1e4),
        Unit.Linear("Square kilometre", "km²", 1e6),
        Unit.Linear("Square inch", "in²", 0.00064516),
        Unit.Linear("Square foot", "ft²", 0.09290304),
        Unit.Linear("Square yard", "yd²", 0.83612736),
        Unit.Linear("Acre", "ac", 4046.8564224),
        Unit.Linear("Square mile", "mi²", 2589988.110336),
    ], "m²", "ft²");

    // Base unit: metre per second.
    public static Category Speed { get; } = new("Speed",
    [
        Unit.Linear("Metre per second", "m/s", 1),
        Unit.Linear("Kilometre per hour", "km/h", 1 / 3.6),
        Unit.Linear("Mile per hour", "mph", 0.44704),
        Unit.Linear("Knot", "kn", 1852.0 / 3600),
        Unit.Linear("Foot per second", "ft/s", 0.3048),
    ], "km/h", "mph");

    // Base unit: second. Month and year use the Gregorian average (365.2425 days).
    public static Category Time { get; } = new("Time",
    [
        Unit.Linear("Millisecond", "ms", 1e-3),
        Unit.Linear("Second", "s", 1),
        Unit.Linear("Minute", "min", 60),
        Unit.Linear("Hour", "h", 3600),
        Unit.Linear("Day", "d", 86400),
        Unit.Linear("Week", "wk", 604800),
        Unit.Linear("Month (avg)", "mo", 2629746),
        Unit.Linear("Year (avg)", "yr", 31556952),
    ], "h", "min");

    // Base unit: byte.
    public static Category Data { get; } = new("Data",
    [
        Unit.Linear("Bit", "bit", 0.125),
        Unit.Linear("Byte", "B", 1),
        Unit.Linear("Kilobyte", "KB", 1e3),
        Unit.Linear("Megabyte", "MB", 1e6),
        Unit.Linear("Gigabyte", "GB", 1e9),
        Unit.Linear("Terabyte", "TB", 1e12),
        Unit.Linear("Kibibyte", "KiB", 1024),
        Unit.Linear("Mebibyte", "MiB", 1048576),
        Unit.Linear("Gibibyte", "GiB", 1073741824),
        Unit.Linear("Tebibyte", "TiB", 1099511627776),
    ], "GB", "GiB");

    public static IReadOnlyList<Category> All { get; } =
        [Length, Mass, Temperature, Volume, Area, Speed, Time, Data];

    public static Category? Find(string name) =>
        All.FirstOrDefault(c => string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase));
}
