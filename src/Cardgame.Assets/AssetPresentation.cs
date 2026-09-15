namespace Cardgame.Assets;

using System.Globalization;
using System.Text.Json;

/// <summary>
/// How game04 shows one PolyTools asset (design/asset_presentation.json):
/// constant x/y factors applied to PolyTools's geometry — PolyTools is the
/// source of truth, the scaled result is game04's — and the fill and stroke
/// colours, the manifests carrying none.
/// </summary>
public sealed record AssetPresentation(double ScaleX, double ScaleY, string Fill, string Stroke);

public static class AssetPresentationLoader
{
    public const int SupportedSchemaVersion = 1;

    public static IReadOnlyDictionary<string, AssetPresentation> Parse(string json, string source)
    {
        void Fail(string message) => throw new ManifestException($"{source}: {message}");

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(json);
        }
        catch (JsonException exception)
        {
            throw new ManifestException($"{source}: not readable JSON ({exception.Message})");
        }

        using (document)
        {
            var root = document.RootElement;
            if (!root.TryGetProperty("schema_version", out var version)
                || !version.TryGetInt32(out int schema) || schema != SupportedSchemaVersion)
            {
                Fail($"schema_version must be {SupportedSchemaVersion}");
            }

            if (!root.TryGetProperty("assets", out var assets) || assets.ValueKind != JsonValueKind.Object)
            {
                Fail("assets must be an object keyed by asset key");
            }

            var result = new Dictionary<string, AssetPresentation>();
            foreach (var entry in assets.EnumerateObject())
            {
                string where = $"asset {entry.Name}";
                var value = entry.Value;
                if (!value.TryGetProperty("scale", out var scale) || scale.ValueKind != JsonValueKind.Array
                    || scale.GetArrayLength() != 2
                    || !scale[0].TryGetDouble(out double x) || !scale[1].TryGetDouble(out double y)
                    || !(x > 0) || !(y > 0) || !double.IsFinite(x) || !double.IsFinite(y))
                {
                    Fail($"{where}: scale must be two positive numbers");
                    continue;
                }

                string fill = Colour(value, "fill");
                string stroke = Colour(value, "stroke");
                if (fill.Length == 0 || stroke.Length == 0)
                {
                    Fail($"{where}: fill and stroke must be #RRGGBB colours");
                }

                result[entry.Name] = new AssetPresentation(x, y, fill, stroke);
            }

            return result;
        }
    }

    private static string Colour(JsonElement value, string name)
    {
        if (!value.TryGetProperty(name, out var property) || property.ValueKind != JsonValueKind.String)
        {
            return string.Empty;
        }

        string text = property.GetString() ?? string.Empty;
        return text.Length == 7 && text[0] == '#'
            && int.TryParse(text.AsSpan(1), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out _)
            ? text
            : string.Empty;
    }
}
