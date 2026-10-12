using System.Text.RegularExpressions;

namespace DeliveryTracking.Api.Validation;

/// <summary>
/// A waybill is WB followed by 8 digits. Input is forgiving: it is trimmed
/// and uppercased before the check.
/// </summary>
public static partial class WaybillFormat
{
    public static string Normalize(string input)
    {
        ArgumentNullException.ThrowIfNull(input);
        return input.Trim().ToUpperInvariant();
    }

    public static bool IsValid(string? input) =>
        input is not null && WaybillPattern().IsMatch(Normalize(input));

    // [0-9] rather than \d, because .NET's \d also matches non-ASCII digits
    // such as Arabic-Indic ones.
    [GeneratedRegex("^WB[0-9]{8}$")]
    private static partial Regex WaybillPattern();
}
