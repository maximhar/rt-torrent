using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics.Contracts;
namespace Torrent.Client
{
    /// <summary>
    /// Performs communication with a remote BitTorrent tracker.
    /// </summary>
    public class TrackerClient
    {
        /// <summary>
        /// The announce URL of the tracker.
        /// </summary>
        public string AnnounceURL { get; private set; }
        /// <summary>
        /// The class constructor.
        /// </summary>
        /// <param name="announceUrl">The announce URL of the tracker.</param>
        public TrackerClient(string announceUrl)
        {
            Contract.Requires(announceUrl != null);

            this.AnnounceURL = announceUrl;
        }
        /// <summary>
        /// Sends a HTTP request to the tracker and returns the response.
        /// </summary>
        /// <param name="request">The data for the request that will be sent to the tracker.</param>
        /// <returns>The tracker's response.</returns>
        public TrackerResponse GetResponse(TrackerRequest request)
        {
            throw new NotImplementedException();
        }
    }
}
