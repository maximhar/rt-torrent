using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics.Contracts;
namespace Torrent.Client
{
    public class TrackerClient
    {
        public string AnnounceURL { get; private set; }
        
        public TrackerClient(string announceUrl)
        {
            Contract.Requires(announceUrl != null);

            this.AnnounceURL = announceUrl;
        }

        public TrackerResponse GetResponse(TrackerRequest request)
        {
            throw new NotImplementedException();
        }
    }
}
