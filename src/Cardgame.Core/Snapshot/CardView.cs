namespace Cardgame.Core.Snapshot;

using Cardgame.Core.Model;

/// <summary>A card the viewer may see: what it is and, once played, at which tier.</summary>
public sealed record CardView(CardInstanceId Id, string DefinitionId, int? Tier);
