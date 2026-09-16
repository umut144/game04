namespace Cardgame.Core.Design;

using System.Text.Json;
using System.Text.Json.Serialization;
using Cardgame.Core.Model;

/// <summary>
/// Parses and validates a design/ tree (cards/*.json, abilities/*.json) into
/// a <see cref="CardCatalog"/>. <see cref="LoadFromDirectory"/> is the real
/// entry point once cards exist on disk (G02+); <see cref="LoadFromSources"/>
/// lets tests feed fixture JSON without touching disk.
/// </summary>
public static class DesignCatalogLoader
{
    private const int SupportedCardSchemaVersion = 1;
    private const int SupportedAbilitySchemaVersion = 1;
    private const int RequiredTierCount = 3;
    public const int MaximumCornerValue = 9;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower) },
    };

    public static CardCatalog LoadFromDirectory(string designRootPath)
    {
        var cardSources = ReadJsonFiles(Path.Combine(designRootPath, "cards"));
        var abilitySources = ReadJsonFiles(Path.Combine(designRootPath, "abilities"));

        return LoadFromSources(cardSources, abilitySources);
    }

    public static CardCatalog LoadFromSources(
        IReadOnlyDictionary<string, string> cardJsonBySourceName,
        IReadOnlyDictionary<string, string> abilityJsonBySourceName)
    {
        var errors = new List<string>();

        var abilities = ParseAbilities(abilityJsonBySourceName, errors);
        var cards = ParseCards(cardJsonBySourceName, abilities.Keys, errors);

        if (errors.Count > 0)
        {
            throw new DesignException(string.Join("; ", errors));
        }

        return new CardCatalog(cards, abilities);
    }

    private static IReadOnlyDictionary<string, string> ReadJsonFiles(string directoryPath)
    {
        if (!Directory.Exists(directoryPath))
        {
            return new Dictionary<string, string>();
        }

        var sources = new Dictionary<string, string>();
        foreach (var filePath in Directory.EnumerateFiles(directoryPath, "*.json"))
        {
            sources[Path.GetFileName(filePath)] = File.ReadAllText(filePath);
        }

        return sources;
    }

    private static Dictionary<string, AbilityDefinition> ParseAbilities(
        IReadOnlyDictionary<string, string> abilityJsonBySourceName,
        List<string> errors)
    {
        var abilities = new Dictionary<string, AbilityDefinition>();

        foreach (var (sourceName, json) in abilityJsonBySourceName)
        {
            AbilityDesign? design;
            try
            {
                design = JsonSerializer.Deserialize<AbilityDesign>(json, SerializerOptions);
            }
            catch (JsonException exception)
            {
                errors.Add($"{sourceName}: cannot parse ability design: {exception.Message}");
                continue;
            }

            if (design is null)
            {
                errors.Add($"{sourceName}: ability design is empty");
                continue;
            }

            if (design.SchemaVersion != SupportedAbilitySchemaVersion)
            {
                errors.Add($"{sourceName}: unsupported ability schema_version {design.SchemaVersion}");
                continue;
            }

            if (string.IsNullOrWhiteSpace(design.NameKey))
            {
                errors.Add($"{sourceName}: ability name_key must not be empty");
                continue;
            }

            if (!abilities.TryAdd(design.NameKey, new AbilityDefinition { NameKey = design.NameKey }))
            {
                errors.Add($"{sourceName}: duplicate ability name_key '{design.NameKey}'");
            }
        }

        return abilities;
    }

    private static Dictionary<string, CardDefinition> ParseCards(
        IReadOnlyDictionary<string, string> cardJsonBySourceName,
        IEnumerable<string> knownAbilityNameKeys,
        List<string> errors)
    {
        var knownAbilities = new HashSet<string>(knownAbilityNameKeys);
        var cards = new Dictionary<string, CardDefinition>();

        foreach (var (sourceName, json) in cardJsonBySourceName)
        {
            CardDesign? design;
            try
            {
                design = JsonSerializer.Deserialize<CardDesign>(json, SerializerOptions);
            }
            catch (JsonException exception)
            {
                errors.Add($"{sourceName}: cannot parse card design: {exception.Message}");
                continue;
            }

            if (design is null)
            {
                errors.Add($"{sourceName}: card design is empty");
                continue;
            }

            if (design.SchemaVersion != SupportedCardSchemaVersion)
            {
                errors.Add($"{sourceName}: unsupported card schema_version {design.SchemaVersion}");
                continue;
            }

            if (string.IsNullOrWhiteSpace(design.Id))
            {
                errors.Add($"{sourceName}: card id must not be empty");
                continue;
            }

            if (design.AssetKey is { } assetKey && string.IsNullOrWhiteSpace(assetKey))
            {
                errors.Add($"{sourceName}: card '{design.Id}' has an empty asset_key");
            }

            if (design.Tiers.Count != RequiredTierCount)
            {
                errors.Add(
                    $"{sourceName}: card '{design.Id}' must define exactly {RequiredTierCount} tiers, found {design.Tiers.Count}");
            }

            foreach (var tier in design.Tiers)
            {
                if (new[] { tier.Cost, tier.Bounty, tier.Attack, tier.Health }
                    .Any(value => value < 0 || value > MaximumCornerValue))
                {
                    errors.Add($"{sourceName}: card '{design.Id}' has a corner value outside 0-{MaximumCornerValue}");
                    break;
                }
            }

            if (design.Attack is { } attack)
            {
                bool needsTarget = attack.Pattern != AttackPattern.BothFrontRows;
                if (needsTarget && attack.Range is not (>= 0 and <= 5))
                {
                    errors.Add($"{sourceName}: card '{design.Id}' needs an attack range of 0-5 for {attack.Pattern}");
                }

                if (!needsTarget && attack.Range is not null)
                {
                    errors.Add($"{sourceName}: card '{design.Id}' has a range, but {attack.Pattern} takes no target");
                }
            }

            foreach (var tierKey in design.Tiers.SelectMany(tier => tier.AbilityNameKeys ?? Array.Empty<string>()))
            {
                if (!knownAbilities.Contains(tierKey))
                {
                    errors.Add($"{sourceName}: card '{design.Id}' has a tier with unknown ability '{tierKey}'");
                }
            }

            var seenAbilityKeys = new HashSet<string>();
            foreach (var abilityNameKey in design.AbilityNameKeys)
            {
                if (!knownAbilities.Contains(abilityNameKey))
                {
                    errors.Add($"{sourceName}: card '{design.Id}' references unknown ability '{abilityNameKey}'");
                }

                if (!seenAbilityKeys.Add(abilityNameKey))
                {
                    errors.Add($"{sourceName}: card '{design.Id}' lists ability '{abilityNameKey}' more than once");
                }
            }

            var definition = new CardDefinition
            {
                Id = design.Id,
                Type = design.Type,
                AssetKey = design.AssetKey,
                Attack = design.Attack is { } profile ? new AttackProfile(profile.Range, profile.Pattern) : null,
                AbilityNameKeys = design.AbilityNameKeys,
                Tiers = design.Tiers
                    .Select(tier => new CardTier
                    {
                        Cost = tier.Cost,
                        Bounty = tier.Bounty,
                        Attack = tier.Attack,
                        Health = tier.Health,
                        AbilityNameKeys = tier.AbilityNameKeys ?? Array.Empty<string>(),
                    })
                    .ToArray(),
            };

            if (!cards.TryAdd(definition.Id, definition))
            {
                errors.Add($"{sourceName}: duplicate card id '{definition.Id}'");
            }
        }

        return cards;
    }
}
