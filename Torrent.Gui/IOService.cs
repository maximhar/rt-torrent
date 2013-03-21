using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Torrent.Gui
{
    interface IOService
    {
        string OpenFile(string filter);
        string SaveFile();
        string OpenFolder();
    }
}
