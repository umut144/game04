namespace Cardgame.Core.Snapshot;

using Cardgame.Core.Model;

/// <summary>
/// A card the viewer may see: what it is, and once played its tier, the
/// damage it has taken and whether it may still attack this turn.
/// </summary>
public sealed record CardView(
    CardInstanceId Id,
    string DefinitionId,
    int? Tier,
    int Damage = 0,
    bool CanAttackNow = false);
