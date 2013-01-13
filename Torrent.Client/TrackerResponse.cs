using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Torrent.Client.Bencoding;
using MoreLinq;

namespace Torrent.Client
{
    public class TrackerResponse
    {
        public string FailureReason { get; private set; }
        public string WarningMessage { get; private set; }
        public int Interval { get; private set; } // seconds
        public int MinInterval { get; private set; } // seconds
        public string TrackerID { get; private set; }
        public int Complete { get; private set; } // seeders
        public int Incomplete { get; private set; } // leechers
        public List<PeerEndpoint> PeerEndpoints { get; private set; }

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
                {
                    WarningMessage = (BencodedString)response["warning message"];
                }

                if (!response.ContainsKey("interval"))
                {
                    throw new TorrentException("Tracker response does not contain interval value.");
                }

                Interval = (BencodedInteger)response["interval"];
                
                if (response.ContainsKey("min interval"))
                {
                    MinInterval = (BencodedInteger)response["min interval"];
                }

                if (response.ContainsKey("tracker id"))
                {
                    TrackerID = (BencodedString)response["tracker id"];
                }

                if (!response.ContainsKey("complete"))
                {
                    throw new TorrentException("Tracker response does not contain complete value.");
                }

                Complete = (BencodedInteger)response["complete"];

                if (!response.ContainsKey("incomplete"))
                {
                    throw new TorrentException("Tracker response does not contain incomplete value.");
                }

                Incomplete = (BencodedInteger)response["incomplete"];

                if (!response.ContainsKey("peers"))
                {
                    throw new TorrentException("Tracker response does not contain peers list.");
                }

                if (response["peers"] is BencodedList)
                {
                    if (((BencodedList)response["peers"]).Count() == 0)
                    {
                        throw new TorrentException("Peers list is empty.");
                    }
                    foreach (BencodedDictionary peer in (BencodedList)response["peers"])
                    {
                        PeerEndpoints.Add(new PeerEndpoint(peer));
                    }
                }
                else if (response["peers"] is BencodedString)
                {
                    if (((BencodedString)response["peers"]).Length == 0)
                    {
                        throw new TorrentException("Peers list is empty.");
                    }
                    var byteData = System.Text.Encoding.ASCII.GetBytes((BencodedString)response["peers"]);
                    int i=0;
                    while (i+6<=byteData.Length)
                    {
                        PeerEndpoints.Add(new PeerEndpoint(byteData.Skip(i).Take(6).ToArray()));
                        i+=6;
                    }
                }
                else
                {
                    throw new TorrentException("Cannot read peers list.");
                }
            }
        }
    }
}
