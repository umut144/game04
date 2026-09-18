namespace Cardgame.Core.Diagnostics;

using System.Text;
using Cardgame.Core;
using Cardgame.Core.Board;
using Cardgame.Core.Model;
using Cardgame.Core.Snapshot;
using Cardgame.Core.Zones;

/// <summary>
/// A plain-text dump of the world or of one player's view. A test/debug
/// helper exercised from assertions, not a program run by hand (CORE-11).
/// </summary>
public static class WorldStateDumper
{
    public static string Dump(WorldState world)
    {
        var text = new StringBuilder();
        text.AppendLine($"seed={world.Seed} mode={world.MatchMode} round={world.Turn.Round} active={world.Turn.ActivePlayer}");
        AppendPlayer(text, "PlayerA", world.PlayerA);
        AppendPlayer(text, "PlayerB", world.PlayerB);
        AppendBoard(text, "PlayerA board", world.Board.PlayerA.Totems, world.Board.PlayerA.UnitSlots);
        AppendBoard(text, "PlayerB board", world.Board.PlayerB.Totems, world.Board.PlayerB.UnitSlots);
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
        AppendBoard(text, "own board", view.OwnBoard.Totems, view.OwnBoard.UnitSlots);
        AppendBoard(text, "opponent board", view.OpponentBoard.Totems, view.OpponentBoard.UnitSlots);
        return text.ToString();
    }

    private static void AppendPlayer(StringBuilder text, string label, PlayerZones zones)
    {
        text.AppendLine(
            $"{label}: deck={zones.Deck.Cards.Count} hand={zones.Hand.Cards.Count} destroyed={zones.Destroyed.Cards.Count} mana={zones.Mana.Current}/{zones.Mana.Maximum} debt={zones.Mana.Debt} " +
            $"life={zones.Life.Health}/{zones.Life.Maximum} time={zones.Time.Seconds}s coins={zones.Coins}");
    }

    // e.g. "PlayerA board: totems=A:Life B:Time C:Mana slots=[card#3,-,-,-,-,-]"
    private static void AppendBoard(
        StringBuilder text,
        string label,
        IReadOnlyList<TotemPlacement> totems,
        IReadOnlyList<CardInstanceId?> slots)
    {
        string totemText = string.Join(" ", totems.Select(t => $"{t.Position}:{t.Type}"));
        string slotText = string.Join(",", slots.Select(s => s?.ToString() ?? "-"));
        text.AppendLine($"{label}: totems={totemText} slots=[{slotText}]");
    }
}
