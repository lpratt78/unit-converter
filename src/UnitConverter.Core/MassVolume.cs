namespace UnitConverter.Core;

/// <summary>
/// Converts between mass and volume units using a density. Mass-to-mass and
/// volume-to-volume conversions ignore the density.
/// </summary>
public static class MassVolume
{
    public static IReadOnlyList<Unit> Units { get; } = [.. Catalog.Mass.Units, .. Catalog.Volume.Units];

    public static Unit DefaultFrom => Catalog.Mass.Find("kg")!;
    public static Unit DefaultTo => Catalog.Volume.Find("L")!;

    public static Unit? Find(string symbol) =>
        Units.FirstOrDefault(u => string.Equals(u.Symbol, symbol, StringComparison.Ordinal));

    public static bool IsMass(Unit unit) => Catalog.Mass.Units.Contains(unit);

    /// <param name="densityKgPerL">Density in kg/L; must be positive.</param>
    public static double Convert(double value, Unit from, Unit to, double densityKgPerL)
    {
        if (!(densityKgPerL > 0))
            throw new ArgumentOutOfRangeException(nameof(densityKgPerL), "Density must be positive.");

        var fromMass = IsMass(from);
        var toMass = IsMass(to);
        if (fromMass == toMass)
            return Converter.Convert(value, from, to);

        // Mass base is kg and volume base is L, so density in kg/L bridges them directly.
        var baseValue = from.ToBase(value);
        var bridged = fromMass ? baseValue / densityKgPerL : baseValue * densityKgPerL;
        return to.FromBase(bridged);
    }
}

/// <summary>A material with a typical density, for quick mass/volume conversions.</summary>
public sealed record Material(string Name, double KgPerL)
{
    /// <summary>Typical densities near room temperature. Real fluids vary with temperature and grade.</summary>
    public static IReadOnlyList<Material> Common { get; } =
    [
        new("Water", 1.0),
        new("Seawater", 1.025),
        new("Milk", 1.03),
        new("Ethanol", 0.789),
        new("Gasoline", 0.745),
        new("Diesel", 0.832),
        new("Hydraulic oil", 0.87),
        new("Glycol 50% / water", 1.07),
        new("Sulfuric acid 98%", 1.84),
        new("Sodium hydroxide 50%", 1.52),
    ];
}
