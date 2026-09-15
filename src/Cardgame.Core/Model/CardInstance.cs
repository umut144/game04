namespace Cardgame.Core.Model;

/// <summary>
/// One concrete copy of a <see cref="CardDefinition"/> inside a match
/// (cardgame-ref §17). No mutable runtime state yet (§18 "Runtime State
/// Preservation") - buffs, damage and the like are added by the gate that
/// first needs them.
/// </summary>
public sealed class CardInstance
{
    public CardInstanceId Id { get; }
    public string DefinitionId { get; }

    public CardInstance(CardInstanceId id, string definitionId)
    {
        Id = id;
        DefinitionId = definitionId;
    }
}
