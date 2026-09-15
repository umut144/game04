namespace Cardgame.Core.Design;

/// <summary>
/// A design/ file failed to parse or failed validation (bad schema_version,
/// an unknown ability reference, a duplicate id, ...). Carries every problem
/// found in one load, not just the first, so a game designer sees the whole
/// list at once.
/// </summary>
public sealed class DesignException : Exception
{
    public DesignException(string message)
        : base(message)
    {
    }
}
