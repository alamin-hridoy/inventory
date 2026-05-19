using InventoryPilot.Models;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace InventoryPilot.Services;

public class CustomIdComposer
{
    public string BuildPreview(IEnumerable<InventoryCustomIdElement> elements, int sequence = 13)
    {
        if (!elements.Any())
        {
            return DefaultManualId(sequence);
        }

        var now = new DateTimeOffset(2025, 1, 15, 9, 30, 0, TimeSpan.Zero);
        return string.Concat(elements.OrderBy(x => x.SortOrder).Select(x => RenderPart(x, now, sequence)));
    }

    public string Generate(IEnumerable<InventoryCustomIdElement> elements, int sequence)
    {
        if (!elements.Any())
        {
            return DefaultManualId(sequence);
        }

        var now = DateTimeOffset.UtcNow;
        return string.Concat(elements.OrderBy(x => x.SortOrder).Select(x => RenderPart(x, now, sequence)));
    }

    public bool IsValid(string customId, IEnumerable<InventoryCustomIdElement> elements)
    {
        var ordered = elements.OrderBy(x => x.SortOrder).ToList();
        if (ordered.Count == 0)
        {
            return true;
        }

        var pattern = string.Concat(ordered.Select(RenderValidationPart));
        return Regex.IsMatch(customId, $"^{pattern}$", RegexOptions.CultureInvariant);
    }

    private static string RenderPart(InventoryCustomIdElement element, DateTimeOffset now, int sequence)
    {
        return element.ElementType switch
        {
            nameof(CustomIdElementTypes.Fixed) or "Fixed" => element.FixedText ?? string.Empty,
            nameof(CustomIdElementTypes.Random20Bit) or "Random20Bit" => FormatNumber(Random.Shared.Next(0, 1 << 20), element.Format, 1048575),
            nameof(CustomIdElementTypes.Random32Bit) or "Random32Bit" => FormatNumber(Random.Shared.NextInt64(0, uint.MaxValue), element.Format, uint.MaxValue),
            nameof(CustomIdElementTypes.Random6Digit) or "Random6Digit" => FormatNumber(Random.Shared.Next(0, 1_000_000), element.Format, 999999),
            nameof(CustomIdElementTypes.Random9Digit) or "Random9Digit" => FormatNumber(Random.Shared.NextInt64(0, 1_000_000_000), element.Format, 999999999),
            nameof(CustomIdElementTypes.Guid) or "Guid" => Guid.NewGuid().ToString(element.Format ?? "N", CultureInfo.InvariantCulture),
            nameof(CustomIdElementTypes.DateTime) or "DateTime" => now.ToString(string.IsNullOrWhiteSpace(element.Format) ? "yyyyMMdd" : element.Format, CultureInfo.InvariantCulture),
            nameof(CustomIdElementTypes.Sequence) or "Sequence" => FormatNumber(sequence, element.Format, sequence),
            _ => string.Empty
        };
    }

    private static string DefaultManualId(int sequence) => $"ITEM-{sequence.ToString("D6", CultureInfo.InvariantCulture)}";

    private static string RenderValidationPart(InventoryCustomIdElement element)
    {
        return element.ElementType switch
        {
            nameof(CustomIdElementTypes.Fixed) or "Fixed" => Regex.Escape(element.FixedText ?? string.Empty),
            nameof(CustomIdElementTypes.Random20Bit) or "Random20Bit" => RenderNumberPattern(element.Format, 1, 7),
            nameof(CustomIdElementTypes.Random32Bit) or "Random32Bit" => RenderNumberPattern(element.Format, 1, 10),
            nameof(CustomIdElementTypes.Random6Digit) or "Random6Digit" => RenderNumberPattern(element.Format, 6, 6),
            nameof(CustomIdElementTypes.Random9Digit) or "Random9Digit" => RenderNumberPattern(element.Format, 9, 9),
            nameof(CustomIdElementTypes.Guid) or "Guid" => RenderGuidPattern(element.Format),
            nameof(CustomIdElementTypes.DateTime) or "DateTime" => RenderDatePattern(element.Format),
            nameof(CustomIdElementTypes.Sequence) or "Sequence" => RenderNumberPattern(element.Format, 1, 12),
            _ => ".*"
        };
    }

    private static string RenderNumberPattern(string? format, int minDigits, int maxDigits)
    {
        if (!string.IsNullOrWhiteSpace(format)
            && format.Length > 1
            && int.TryParse(format[1..], NumberStyles.None, CultureInfo.InvariantCulture, out var width))
        {
            if (format.StartsWith("X", StringComparison.OrdinalIgnoreCase))
            {
                return $"[0-9A-Fa-f]{{{width},}}";
            }

            if (format.StartsWith("D", StringComparison.OrdinalIgnoreCase))
            {
                return $"\\d{{{width},}}";
            }
        }

        return $"\\d{{{minDigits},{maxDigits}}}";
    }

    private static string RenderGuidPattern(string? format)
    {
        return (format ?? "N").ToUpperInvariant() switch
        {
            "D" => "[0-9A-Fa-f]{8}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{12}",
            "B" => "\\{[0-9A-Fa-f]{8}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{12}\\}",
            "P" => "\\([0-9A-Fa-f]{8}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{12}\\)",
            _ => "[0-9A-Fa-f]{32}"
        };
    }

    private static string RenderDatePattern(string? format)
    {
        var source = string.IsNullOrWhiteSpace(format) ? "yyyyMMdd" : format;
        var result = new StringBuilder();
        for (var i = 0; i < source.Length; i++)
        {
            var current = source[i];
            if ("yMdHhmsfF".Contains(current))
            {
                result.Append("\\d");
            }
            else
            {
                result.Append(Regex.Escape(current.ToString()));
            }
        }

        return result.ToString();
    }

    private static string FormatNumber(long value, string? format, long fallback)
    {
        if (string.IsNullOrWhiteSpace(format))
        {
            return fallback == uint.MaxValue ? value.ToString("X", CultureInfo.InvariantCulture) : value.ToString(CultureInfo.InvariantCulture);
        }

        if (format.StartsWith("X", StringComparison.OrdinalIgnoreCase) || format.StartsWith("D", StringComparison.OrdinalIgnoreCase))
        {
            return value.ToString(format, CultureInfo.InvariantCulture);
        }

        return value.ToString(CultureInfo.InvariantCulture);
    }
}
