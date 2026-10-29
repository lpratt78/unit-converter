using System.Globalization;

namespace UnitConverter.Core;

public static class Converter
{
    public static double Convert(double value, Unit from, Unit to) =>
        ReferenceEquals(from, to) ? value : to.FromBase(from.ToBase(value));

    /// <summary>
    /// Parses user input. Accepts the current culture's format, the invariant format,
    /// thousands separators, spaces and a leading '+'.
    /// </summary>
    public static bool TryParse(string? text, CultureInfo culture, out double value)
    {
        value = 0;
        if (string.IsNullOrWhiteSpace(text))
            return false;

        var trimmed = text.Trim().Replace(" ", "").Replace(" ", "");
        const NumberStyles styles = NumberStyles.Float | NumberStyles.AllowThousands;

        return (double.TryParse(trimmed, styles, culture, out value)
                || double.TryParse(trimmed, styles, CultureInfo.InvariantCulture, out value))
               && double.IsFinite(value);
    }

    /// <summary>
    /// Formats a result for display: up to 10 significant digits, no trailing zeros,
    /// and scientific notation only for very large or very small magnitudes.
    /// </summary>
    public static string Format(double value, CultureInfo culture)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
            return "—";

        // Snap floating-point noise like 0.30000000000000004 or 1e-17 after a round trip.
        var rounded = double.Parse(value.ToString("G10", CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);
        if (rounded == 0)
            return "0";

        var magnitude = Math.Abs(rounded);
        if (magnitude >= 1e15 || magnitude < 1e-6)
            return rounded.ToString("0.#########E+0", culture);

        return rounded.ToString("#,0.##########", culture);
    }
}
