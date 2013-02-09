using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Torrent.Client
{
    public interface IMode
    {
        void HandleMessage();
    }
}
