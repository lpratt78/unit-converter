namespace UnitConverter.Core;

public sealed class Category
{
    public Category(string name, IReadOnlyList<Unit> units, string defaultFrom, string defaultTo)
    {
        Name = name;
        Units = units;
        DefaultFrom = Find(defaultFrom) ?? throw new ArgumentException($"Unknown unit '{defaultFrom}'", nameof(defaultFrom));
        DefaultTo = Find(defaultTo) ?? throw new ArgumentException($"Unknown unit '{defaultTo}'", nameof(defaultTo));
    }

    public string Name { get; }
    public IReadOnlyList<Unit> Units { get; }
    public Unit DefaultFrom { get; }
    public Unit DefaultTo { get; }

    public Unit? Find(string symbol) =>
        Units.FirstOrDefault(u => string.Equals(u.Symbol, symbol, StringComparison.Ordinal));

    public override string ToString() => Name;
}
