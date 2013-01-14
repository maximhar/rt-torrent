using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics.Contracts;
using System.Net;
using System.Web;
using MoreLinq;
using System.Net.Sockets;
using System.IO;
using System.Threading.Tasks;
using System.Diagnostics;
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

        private string UrlEncode(byte[] source)
        {
            StringBuilder builder = new StringBuilder();
            string hex = BitConverter.ToString(source).Replace("-", string.Empty);
            hex.Batch(2).ForEach(h => builder.Append("%" + new string(h.ToArray()).ToLower()));
            return builder.ToString();
        }
        /// <summary>
        /// Sends a HTTP request to the tracker and returns the response.
        /// </summary>
        /// <param name="request">The data for the request that will be sent to the tracker.</param>
        /// <returns>The tracker's response.</returns>
        async public Task<TrackerResponse> GetResponseAsync(TrackerRequest requestData)
        {
            Contract.Requires(requestData != null);

            string announceURL = AnnounceURL;
            
            var parameters = new Dictionary<string, string>
            {
                {"info_hash", UrlEncode(requestData.InfoHash)},
                {"peer_id", Uri.EscapeDataString(Encoding.ASCII.GetString(requestData.PeerId))},
                {"port", requestData.Port.ToString()},
                {"uploaded", requestData.Uploaded.ToString()},
                {"downloaded", requestData.Downloaded.ToString()},
                {"left", requestData.Left.ToString()},
                {"compact", requestData.Compact?"1":"0"},
                {"no_peer_id", requestData.OmitPeerIds?"1":"0"}
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

            var request = (HttpWebRequest)WebRequest.Create(announceURL + urlBuilder.ToString());
            request.KeepAlive = false;
            request.Method = "GET";
            
            try
            {
                var response = await request.GetResponseAsync();
                byte[] trackerResponse;
                using (var reader = new BinaryReader(response.GetResponseStream()))
                {
                    using (MemoryStream ms = new MemoryStream())
                    {
                        byte[] buffer = new byte[1024];
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
                throw new TorrentException(string.Format("Announce URL: {0}", announceURL), e);
            }
        }
    }
}
