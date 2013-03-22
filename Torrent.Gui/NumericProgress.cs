using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Torrent.Gui
{
    struct NumericProgress
    {
        public long BytesTotal { get; set; }
        public long BytesComplete { get; set; }
    }
}
