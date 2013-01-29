using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using MoreLinq;

namespace Torrent.Client
{
    /// <summary>
    /// Performs communication with a remote BitTorrent tracker.
    /// </summary>
    public class TrackerClient
    {
        /// <summary>
        /// The class constructor.
        /// </summary>
        /// <param name="announces">The announce URLs of the tracker.</param>
        public TrackerClient(IEnumerable<string> announces)
        {
            Contract.Requires(announces != null);
            Announces = announces;
        }

        /// <summary>
        /// The announce URL of the tracker.
        /// </summary>
        public IEnumerable<string> Announces { get; private set; }

        public string PreferredAnnounce { get; private set; }

        private string UrlEncode(byte[] source)
        {
            var builder = new StringBuilder();
            string hex = BitConverter.ToString(source).Replace("-", string.Empty);
            hex.Batch(2).ForEach(h => builder.Append("%" + new string(h.ToArray()).ToLower()));
            return builder.ToString();
        }

        public TrackerResponse AnnounceStart(byte[] infoHash, string peerId, ushort port, long downloaded, long uploaded,
                                             long left)
        {
            var request = new TrackerRequest(infoHash, peerId, port, uploaded, downloaded, left, true, true,
                                             EventType.Started, 100);
            return GetResponse(request);
        }

        /// <summary>
        /// Sends a HTTP request to the tracker and returns the response.
        /// </summary>
        /// <param name="request">The data for the request that will be sent to the tracker.</param>
        /// <returns>The tracker's response.</returns>
        public TrackerResponse GetResponse(TrackerRequest requestData)
        {
            Contract.Requires(requestData != null);

            TrackerResponse response = null;

            if (PreferredAnnounce != null)
                response = AttemptGet(requestData, PreferredAnnounce);

            if (response == null)
            {
                foreach (string url in Announces)
                {
                    Debug.WriteLine("Trying to connect to " + url);
                    if ((response = AttemptGet(requestData, url)) != null)
                    {
                        PreferredAnnounce = url;
                        break;
                    }
                }
            }

            if (response == null)
                throw new TorrentException("Unable to connect to tracker.");

            return response;
            Debug.WriteLine("Connected to tracker!");
        }

        private TrackerResponse AttemptGet(TrackerRequest requestData, string announceURL)
        {
            var parameters = new Dictionary<string, string>
                                 {
                                     {"info_hash", UrlEncode(requestData.InfoHash)},
                                     {"peer_id", Uri.EscapeDataString(requestData.PeerId)},
                                     {"port", requestData.Port.ToString()},
                                     {"uploaded", requestData.Uploaded.ToString()},
                                     {"downloaded", requestData.Downloaded.ToString()},
                                     {"left", requestData.Left.ToString()},
                                     {"compact", requestData.Compact ? "1" : "0"},
                                     {"no_peer_id", requestData.OmitPeerIds ? "1" : "0"}
                                 };
            if (requestData.Event != EventType.None)
                parameters.Add("event", requestData.Event.ToString().ToLower());
            if (requestData.NumWant.HasValue)
                parameters.Add("numwant", requestData.NumWant.ToString());
            var urlBuilder = new StringBuilder();
            urlBuilder.Append(parameters.Select(kv =>
                                                    {
                                                        if (kv.Value == null)
                                                            return string.Empty;
                                                        return kv.Key + "=" + kv.Value;
                                                    }).ToDelimitedString("&"));

            if (!announceURL.Contains("?")) announceURL += "?";
            else announceURL += "&";

            var request = (HttpWebRequest) WebRequest.Create(announceURL + urlBuilder);
            request.KeepAlive = false;
            request.Method = "GET";

            try
            {
                WebResponse response = request.GetResponse();
                byte[] trackerResponse;
                using (var reader = new BinaryReader(response.GetResponseStream()))
                {
                    using (var ms = new MemoryStream())
                    {
                        var buffer = new byte[1024];
                        int len = 0;
                        while ((len = reader.Read(buffer, 0, buffer.Length)) != 0)
                        {
                            ms.Write(buffer, 0, len);
                        }
                        trackerResponse = ms.ToArray();
                    }
                }
                response.Close();
                return new TrackerResponse(trackerResponse);
            }
            catch (Exception e)
            {
                return null;
            }
        }
    }
}