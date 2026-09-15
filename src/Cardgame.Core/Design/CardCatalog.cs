namespace Cardgame.Core.Design;

using Cardgame.Core.Model;

/// <summary>
/// The result of loading and validating a whole design/ tree: every card and
/// ability, keyed and cross-checked (see <see cref="DesignCatalogLoader"/>).
/// Everything from here on is <see cref="CardDefinition"/>, not the raw
/// <see cref="CardDesign"/>.
/// </summary>
public sealed class CardCatalog
{
    public IReadOnlyDictionary<string, CardDefinition> CardsById { get; }
    public IReadOnlyDictionary<string, AbilityDefinition> AbilitiesByNameKey { get; }

    internal CardCatalog(
        IReadOnlyDictionary<string, CardDefinition> cardsById,
        IReadOnlyDictionary<string, AbilityDefinition> abilitiesByNameKey)
    {
        CardsById = cardsById;
        AbilitiesByNameKey = abilitiesByNameKey;
    }
}
