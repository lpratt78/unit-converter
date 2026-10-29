namespace UnitConverter.Core;

/// <summary>
/// A unit of measure. Every unit converts to and from its category's base unit.
/// </summary>
public sealed class Unit
{
    private readonly Func<double, double> _toBase;
    private readonly Func<double, double> _fromBase;

    public Unit(string name, string symbol, Func<double, double> toBase, Func<double, double> fromBase)
    {
        Name = name;
        Symbol = symbol;
        _toBase = toBase;
        _fromBase = fromBase;
    }

    public string Name { get; }
    public string Symbol { get; }

    public double ToBase(double value) => _toBase(value);
    public double FromBase(double value) => _fromBase(value);

    /// <summary>A unit that is a fixed multiple of the base unit.</summary>
    public static Unit Linear(string name, string symbol, double factor) =>
        new(name, symbol, v => v * factor, v => v / factor);

    public override string ToString() => $"{Name} ({Symbol})";
}
