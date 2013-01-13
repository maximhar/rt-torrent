using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Torrent.Client.Bencoding;
using MoreLinq;

namespace Torrent.Client
{
    /// <summary>
    /// Provides a container class for the BitTorrent tracker response data.
    /// </summary>
    public class TrackerResponse
    {
        /// <summary>
        /// The value is a human-readable error message as to why the request failed.
        /// </summary>
        public string FailureReason { get; private set; }

        /// <summary>
        /// Similar to failure reason, but the response still gets processed normally. 
        /// <para>The warning message is shown just like an error.</para>
        /// </summary>
        public string WarningMessage { get; private set; }

        /// <summary>
        /// Interval in seconds that the client should wait between sending regular requests to the tracker.
        /// </summary>
        public int Interval { get; private set; }

        /// <summary>
        /// Minimum announce interval in seconds.
        /// <para>If present clients must not reannounce more frequently than this.</para>
        /// </summary>
        public int MinInterval { get; private set; }

        /// <summary>
        /// A string that the client should send back on its next announcements. 
        /// <para>If absent and a previous announce sent a tracker id, do not discard the old value; keep using it.</para>
        /// </summary>
        public string TrackerID { get; private set; }

        /// <summary>
        /// The number of peers with the entire file, i.e. seeders.
        /// </summary>
        public int Complete { get; private set; }

        /// <summary>
        /// The number of non-seeder peers, aka "leechers".
        /// </summary>
        public int Incomplete { get; private set; }

        /// <summary>
        /// A list of PeerEndoints containing all peers.
        /// </summary>
        public List<PeerEndpoint> PeerEndpoints { get; private set; }

        /// <summary>
        /// Initializes a new instance of the Torrent.Client.TrackerResponse class with the reponse specified as a string.
        /// </summary>
        /// <exception cref="Torrent.Client.TorrentException">Thrown when the string containing the tracker response is invalid.</exception>
        /// <param name="bencoded">A bencoded string containing the tracker response.</param>
        public TrackerResponse(string bencoded)
        {
            var parser = new BencodedStreamParser(bencoded);
            var response = (BencodedDictionary)parser.Parse();

            if (response.ContainsKey("failure reason"))
            {
                FailureReason = (BencodedString)response["failure reason"];
            }
            else
            {
                if (response.ContainsKey("warning message"))
                    WarningMessage = (BencodedString)response["warning message"];
                if (response.ContainsKey("min interval"))
                    MinInterval = (BencodedInteger)response["min interval"];
                if (response.ContainsKey("tracker id"))
                    TrackerID = (BencodedString)response["tracker id"];

                if (!response.ContainsKey("interval"))
                    throw new TorrentException("Tracker response does not contain interval value.");
                if (!response.ContainsKey("complete"))
                    throw new TorrentException("Tracker response does not contain complete value.");
                if (!response.ContainsKey("incomplete"))
                    throw new TorrentException("Tracker response does not contain incomplete value.");
                if (!response.ContainsKey("peers"))
                    throw new TorrentException("Tracker response does not contain peers list.");

                Interval = (BencodedInteger)response["interval"];
                Complete = (BencodedInteger)response["complete"];
                Incomplete = (BencodedInteger)response["incomplete"];

                if (response["peers"] is BencodedList)
                {
                    if (((BencodedList)response["peers"]).Count() == 0)
                        throw new TorrentException("Peers list is empty.");

                    foreach (BencodedDictionary peer in (BencodedList)response["peers"])
                    {
                        PeerEndpoints.Add(new PeerEndpoint(peer));
                    }
                }
                else if (response["peers"] is BencodedString)
                {
                    if (!((BencodedString)response["peers"]).Any())
                        throw new TorrentException("Peers list is empty.");

                    var byteData = Encoding.ASCII.GetBytes((BencodedString)response["peers"]);
                    PeerEndpoints = byteData.Batch(6).Select(p => new PeerEndpoint(p.ToArray())).ToList();
                }
                else
                {
                    throw new TorrentException("Cannot read peers list.");
                }
            }
        }
    }
}
