using System.Globalization;
using UnitConverter.Core;

namespace UnitConverter.Tests;

public class ConverterTests
{
    private static readonly CultureInfo Us = CultureInfo.GetCultureInfo("en-US");
    private static readonly CultureInfo German = CultureInfo.GetCultureInfo("de-DE");

    private static double Convert(Category category, double value, string from, string to) =>
        Converter.Convert(value, category.Find(from)!, category.Find(to)!);

    [Theory]
    [InlineData(1, "in", "cm", 2.54)]
    [InlineData(1, "mi", "km", 1.609344)]
    [InlineData(1, "ft", "in", 12)]
    [InlineData(1, "nmi", "m", 1852)]
    public void Length(double value, string from, string to, double expected) =>
        Assert.Equal(expected, Convert(Catalog.Length, value, from, to), 9);

    [Theory]
    [InlineData(1, "lb", "oz", 16)]
    [InlineData(1, "st", "lb", 14)]
    [InlineData(1, "kg", "lb", 2.2046226218)]
    [InlineData(1, "ton", "lb", 2000)]
    public void Mass(double value, string from, string to, double expected) =>
        Assert.Equal(expected, Convert(Catalog.Mass, value, from, to), 9);

    [Theory]
    [InlineData(0, "°C", "°F", 32)]
    [InlineData(100, "°C", "°F", 212)]
    [InlineData(-40, "°C", "°F", -40)]
    [InlineData(0, "K", "°C", -273.15)]
    [InlineData(98.6, "°F", "°C", 37)]
    public void Temperature(double value, string from, string to, double expected) =>
        Assert.Equal(expected, Convert(Catalog.Temperature, value, from, to), 9);

    [Theory]
    [InlineData(1, "gal", "L", 3.785411784)]
    [InlineData(1, "gal", "qt", 4)]
    [InlineData(1, "cup", "fl oz", 8)]
    [InlineData(1, "tbsp", "tsp", 3)]
    public void Volume(double value, string from, string to, double expected) =>
        Assert.Equal(expected, Convert(Catalog.Volume, value, from, to), 9);

    [Theory]
    [InlineData(1, "ac", "ft²", 43560)]
    [InlineData(1, "km²", "ha", 100)]
    [InlineData(1, "mi²", "ac", 640)]
    public void Area(double value, string from, string to, double expected) =>
        Assert.Equal(expected, Convert(Catalog.Area, value, from, to), 6);

    [Theory]
    [InlineData(100, "km/h", "m/s", 27.7777777778)]
    [InlineData(60, "mph", "km/h", 96.56064)]
    [InlineData(1, "kn", "km/h", 1.852)]
    public void Speed(double value, string from, string to, double expected) =>
        Assert.Equal(expected, Convert(Catalog.Speed, value, from, to), 9);

    [Theory]
    [InlineData(1, "d", "h", 24)]
    [InlineData(1, "wk", "min", 10080)]
    [InlineData(1, "yr", "d", 365.2425)]
    public void Time(double value, string from, string to, double expected) =>
        Assert.Equal(expected, Convert(Catalog.Time, value, from, to), 9);

    [Theory]
    [InlineData(1, "GiB", "MiB", 1024)]
    [InlineData(1, "GB", "MB", 1000)]
    [InlineData(1, "B", "bit", 8)]
    [InlineData(1, "TB", "GiB", 931.3225746154785)]
    public void Data(double value, string from, string to, double expected) =>
        Assert.Equal(expected, Convert(Catalog.Data, value, from, to), 9);

    [Fact]
    public void EveryUnitRoundTrips()
    {
        foreach (var category in Catalog.All)
        foreach (var a in category.Units)
        foreach (var b in category.Units)
        {
            var back = Converter.Convert(Converter.Convert(123.456, a, b), b, a);
            Assert.Equal(123.456, back, 9);
        }
    }

    [Fact]
    public void CatalogHasUniqueSymbolsPerCategory()
    {
        foreach (var category in Catalog.All)
            Assert.Equal(category.Units.Count, category.Units.Select(u => u.Symbol).Distinct().Count());
    }

    [Theory]
    [InlineData("12.5", 12.5)]
    [InlineData(" 1,234.5 ", 1234.5)]
    [InlineData("+3", 3)]
    [InlineData("-0.25", -0.25)]
    [InlineData("1e3", 1000)]
    public void ParsesUsInput(string text, double expected)
    {
        Assert.True(Converter.TryParse(text, Us, out var value));
        Assert.Equal(expected, value);
    }

    [Fact]
    public void ParsesLocalDecimalComma()
    {
        Assert.True(Converter.TryParse("12,5", German, out var value));
        Assert.Equal(12.5, value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("abc")]
    [InlineData(null)]
    public void RejectsInvalidInput(string? text) =>
        Assert.False(Converter.TryParse(text, Us, out _));

    [Theory]
    [InlineData(2.54, "2.54")]
    [InlineData(0.1 + 0.2, "0.3")]
    [InlineData(1234567.891, "1,234,567.891")]
    [InlineData(0, "0")]
    [InlineData(1e-17, "1E-17")]
    [InlineData(1099511627776d * 1024 * 1024, "1.152921505E+18")]
    [InlineData(-40, "-40")]
    public void FormatsReadably(double value, string expected) =>
        Assert.Equal(expected, Converter.Format(value, Us));
}
