using System.Collections.Generic;
using System.Linq;
using UnitConverter.Core;

namespace UnitConverter.App;

public enum ModeKind { Units, MassVolume, Scaling }

/// <summary>One entry in the category drop-down.</summary>
public sealed class Mode
{
    private Mode(string name, ModeKind kind, Category? category = null)
    {
        Name = name;
        Kind = kind;
        Category = category;
    }

    public string Name { get; }
    public ModeKind Kind { get; }

    /// <summary>The unit category, for <see cref="ModeKind.Units"/> only.</summary>
    public Category? Category { get; }

    public IReadOnlyList<Unit> Units => Kind == ModeKind.MassVolume ? MassVolume.Units : Category?.Units ?? [];
    public Unit? DefaultFrom => Kind == ModeKind.MassVolume ? MassVolume.DefaultFrom : Category?.DefaultFrom;
    public Unit? DefaultTo => Kind == ModeKind.MassVolume ? MassVolume.DefaultTo : Category?.DefaultTo;

    public Unit? FindUnit(string symbol) => Units.FirstOrDefault(u => u.Symbol == symbol);

    public static IReadOnlyList<Mode> All { get; } =
    [
        new("Scaling", ModeKind.Scaling),
        new("Mass ↔ Volume", ModeKind.MassVolume),
        .. Catalog.All.Select(c => new Mode(c.Name, ModeKind.Units, c)),
    ];

    public static Mode? FindByName(string name) => All.FirstOrDefault(m => m.Name == name);

    public override string ToString() => Name;
}
