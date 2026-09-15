namespace Cardgame.Assets;

/// <summary>A PolyTools manifest or a game04 asset design file game04 cannot use.</summary>
public sealed class ManifestException : Exception
{
    public ManifestException(string message)
        : base(message)
    {
    }
}
