using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace Torrent.Gui
{

    enum Mode
    {
        Seed,
        Download,
        Hash,
        Stopped,
        Completed,
        Error,
        Idle
    }
}
