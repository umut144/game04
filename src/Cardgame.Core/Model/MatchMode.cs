namespace Cardgame.Core.Model;

/// <summary>
/// How a match is set up (§2): the two mirror modes, and Constructed, where
/// each side brings its own deck (G04-01).
/// </summary>
public enum MatchMode
{
    ShuffledMirror,
    PerfectMirror,
    Constructed,
}

public static class MatchModes
{
    public static bool IsMirror(this MatchMode mode) => mode is MatchMode.ShuffledMirror or MatchMode.PerfectMirror;
}
