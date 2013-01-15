using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
