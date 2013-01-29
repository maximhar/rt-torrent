namespace Torrent.Client.Bencoding
{
    /// <summary>
    /// Represents an interface for the Torrent.Client.Bencoding classes.
    /// </summary>
    public interface IBencodedElement
    {
        string ToBencodedString();
    }
}