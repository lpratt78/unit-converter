namespace UnitConverter.Core;

/// <summary>
/// Linear scaling between a raw input range and an engineering output range, as done by
/// PLC scaling instructions (SCP, SCL, NORM_X/SCALE_X): out = slope × in + offset.
/// Values outside the input range are extrapolated, not clamped.
/// </summary>
public readonly record struct LinearScale(double InMin, double InMax, double OutMin, double OutMax)
{
    public bool CanScale => InMax != InMin;
    public bool CanUnscale => OutMax != OutMin;

    public double Slope => (OutMax - OutMin) / (InMax - InMin);
    public double Offset => OutMin - Slope * InMin;

    public double Scale(double input) => OutMin + (input - InMin) * (OutMax - OutMin) / (InMax - InMin);

    public double Unscale(double output) => InMin + (output - OutMin) * (InMax - InMin) / (OutMax - OutMin);

    public bool IsInputInRange(double input) =>
        input >= Math.Min(InMin, InMax) && input <= Math.Max(InMin, InMax);

    public LinearScale Reversed() => new(OutMin, OutMax, InMin, InMax);
}

/// <summary>Common analog signal and raw-count ranges.</summary>
public sealed record ScalePreset(string Name, double Min, double Max)
{
    public static IReadOnlyList<ScalePreset> Common { get; } =
    [
        new("4–20 mA", 4, 20),
        new("0–20 mA", 0, 20),
        new("0–10 V", 0, 10),
        new("±10 V", -10, 10),
        new("1–5 V", 1, 5),
        new("0–100 %", 0, 100),
        new("Siemens 0–27648", 0, 27648),
        new("Siemens ±27648", -27648, 27648),
        new("0–32767", 0, 32767),
        new("12-bit 0–4095", 0, 4095),
        new("16-bit 0–65535", 0, 65535),
    ];
}
