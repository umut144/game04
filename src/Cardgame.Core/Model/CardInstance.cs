namespace Cardgame.Core.Model;

/// <summary>
/// One concrete copy of a <see cref="CardDefinition"/> inside a match
/// (cardgame-ref §17). Runtime state is added by the gate that first needs
/// it: G02 adds the tier chosen when the card was played.
/// </summary>
public sealed class CardInstance
{
    public CardInstanceId Id { get; }
    public string DefinitionId { get; }

    /// <summary>1-3 once the card has been played (§7.3); null in deck and hand.</summary>
    public int? Tier { get; internal set; }

    public CardInstance(CardInstanceId id, string definitionId)
    {
        Id = id;
        DefinitionId = definitionId;
    }
}
