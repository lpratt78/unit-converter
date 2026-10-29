using UnitConverter.Core;

namespace UnitConverter.Tests;

public class PlcTests
{
    private static Unit U(string symbol) => MassVolume.Find(symbol)!;

    [Theory]
    [InlineData(1, "kg", "L", 1.0, 1)]
    [InlineData(1000, "kg", "L", 0.832, 1201.923076923)]
    [InlineData(1, "gal", "lb", 1.0, 8.345404452)]
    [InlineData(500, "L", "kg", 1.84, 920)]
    [InlineData(2, "kg", "lb", 0.5, 4.409245244)] // mass to mass ignores density
    [InlineData(1, "gal", "L", 9.9, 3.785411784)] // volume to volume ignores density
    public void MassVolumeConversion(double value, string from, string to, double density, double expected) =>
        Assert.Equal(expected, MassVolume.Convert(value, U(from), U(to), density), 6);

    [Fact]
    public void MassVolumeRejectsNonPositiveDensity() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => MassVolume.Convert(1, U("kg"), U("L"), 0));

    [Fact]
    public void MassVolumeSymbolsAreUnique() =>
        Assert.Equal(MassVolume.Units.Count, MassVolume.Units.Select(u => u.Symbol).Distinct().Count());

    [Theory]
    [InlineData(4, 0)]
    [InlineData(12, 50)]
    [InlineData(20, 100)]
    [InlineData(0, -25)] // extrapolates below range
    public void Scales4To20mA(double mA, double expected)
    {
        var scale = new LinearScale(4, 20, 0, 100);
        Assert.Equal(expected, scale.Scale(mA), 9);
        Assert.Equal(mA, scale.Unscale(expected), 9);
    }

    [Fact]
    public void SlopeAndOffset()
    {
        var scale = new LinearScale(4, 20, 0, 100);
        Assert.Equal(6.25, scale.Slope, 12);
        Assert.Equal(-25, scale.Offset, 12);
    }

    [Fact]
    public void ScalesSiemensRawCounts()
    {
        var scale = new LinearScale(0, 27648, 0, 150);
        Assert.Equal(75, scale.Scale(13824), 9);
    }

    [Fact]
    public void InvertedOutputRange()
    {
        var scale = new LinearScale(0, 10, 100, 0);
        Assert.Equal(75, scale.Scale(2.5), 9);
    }

    [Fact]
    public void DetectsDegenerateRanges()
    {
        Assert.False(new LinearScale(5, 5, 0, 1).CanScale);
        Assert.False(new LinearScale(0, 1, 3, 3).CanUnscale);
    }

    [Theory]
    [InlineData(4, true)]
    [InlineData(20, true)]
    [InlineData(3.9, false)]
    [InlineData(20.1, false)]
    public void InputRangeCheck(double input, bool inRange) =>
        Assert.Equal(inRange, new LinearScale(4, 20, 0, 100).IsInputInRange(input));

    [Theory]
    [InlineData(1, "psi", "kPa", 6.894757293)]
    [InlineData(1, "bar", "psi", 14.503773773)]
    [InlineData(1, "atm", "mmHg", 759.999892)] // conventional mmHg, not torr
    public void Pressure(double value, string from, string to, double expected) =>
        Assert.Equal(expected, Converter.Convert(value, Catalog.Pressure.Find(from)!, Catalog.Pressure.Find(to)!), 6);

    [Theory]
    [InlineData(1, "GPM", "L/min", 3.785411784)]
    [InlineData(1, "m³/h", "L/min", 16.666666667)]
    public void Flow(double value, string from, string to, double expected) =>
        Assert.Equal(expected, Converter.Convert(value, Catalog.Flow.Find(from)!, Catalog.Flow.Find(to)!), 6);

    [Fact]
    public void WaterIsAbout8Point345PoundsPerGallon() =>
        Assert.Equal(8.345404452, Converter.Convert(1, Catalog.Density.Find("kg/L")!, Catalog.Density.Find("lb/gal")!), 6);

    [Theory]
    [InlineData("Water", 8.34)]
    [InlineData("Liquid egg", 8.64)]
    public void PresetsUsePoundsPerGallon(string name, double lbPerGal)
    {
        var material = Material.Common.Single(m => m.Name == name);
        Assert.Equal(lbPerGal, material.LbPerGal, 9);
        // 10 gal of it weighs 10 × the factor in pounds.
        Assert.Equal(lbPerGal * 10, MassVolume.Convert(10, U("gal"), U("lb"), material.KgPerL), 9);
    }
}
