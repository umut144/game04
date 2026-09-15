namespace Cardgame.Core.Diagnostics;

using System.Text;
using Cardgame.Core;
using Cardgame.Core.Snapshot;
using Cardgame.Core.Zones;

/// <summary>
/// The plain-text dump that is G00's only "view" (ROADMAP.md, G00). A
/// test/debug helper exercised from assertions, not a program a developer
/// runs by hand (see docs/TASKS.md).
/// </summary>
public static class WorldStateDumper
{
    public static string Dump(WorldState world)
    {
        var text = new StringBuilder();
        text.AppendLine($"seed={world.Seed} mirror={world.MirrorMode}");
        AppendPlayer(text, "PlayerA", world.PlayerA);
        AppendPlayer(text, "PlayerB", world.PlayerB);
        return text.ToString();
    }

    public static string Dump(PlayerView view)
    {
        var text = new StringBuilder();
        text.AppendLine($"viewer={view.Viewer}");
        text.AppendLine(
            $"own: hand={view.OwnHandCount} [{string.Join(",", view.OwnHandCards)}] " +
            $"deck={view.OwnDeckCount} destroyed=[{string.Join(",", view.OwnDestroyed)}]");
        text.AppendLine(
            $"opponent: hand={view.OpponentHandCount} deck={view.OpponentDeckCount} " +
            $"destroyed=[{string.Join(",", view.OpponentDestroyed)}]");
        return text.ToString();
    }

    private static void AppendPlayer(StringBuilder text, string label, PlayerZones zones)
    {
        text.AppendLine(
            $"{label}: deck={zones.Deck.Cards.Count} hand={zones.Hand.Cards.Count} destroyed={zones.Destroyed.Cards.Count}");
    }
}
