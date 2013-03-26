using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Torrent.Client.Events;

namespace Torrent.Client
{
    public class AnnounceManager:IDisposable
    {
        private bool disposed;
        private List<TrackerInfo> trackers;
        private Timer regularTimer;

        public IEnumerable<TrackerInfo> Trackers { get { return trackers.AsEnumerable(); } }
        public TransferMonitor Monitor { get; private set; }
        public TorrentData Data { get; private set; }

        public AnnounceManager(IEnumerable<string> announceUrls, TransferMonitor monitor, TorrentData data)
        {   //иницализация на списъка с тракери, състоящ се от класове TrackerInfo, които осигуряват
            //връзка с отделни тракери и заявки до тях
            trackers = new List<TrackerInfo>();
            //прибавяне на HTTP тракерите от подадения списък с адреси
            trackers.AddRange(announceUrls.Where(u=>u.StartsWith("http")).Select(u=>new TrackerInfo(u)));
            //прикачане на събитието PeersReceived към всеки от тракерите (TrackerInfo)
            trackers.ForEach(t => t.PeersReceived += (sender, args) => OnPeersReceived(args.Value));
            Monitor = monitor;
            Data = data;
            //инициализация на таймер за проверка на състоянието на всеки тракер
            regularTimer = new Timer(Regular);
            regularTimer.Change(1000, 5000);
        }

        public void Started()
        {
            Task.Factory.StartNew(() =>
            {
                foreach (var tracker in Trackers)
                {
                    tracker.Started(Data.InfoHash, Monitor.BytesReceived,
                                    Monitor.BytesSent, Monitor.TotalBytes - Monitor.BytesReceived);
                }
            });
        }

        public void Completed()
        {
            Task.Factory.StartNew(() =>
            {
                foreach (var tracker in Trackers)
                {
                    tracker.Completed(Data.InfoHash, Monitor.BytesReceived,
                                    Monitor.BytesSent, Monitor.TotalBytes - Monitor.BytesReceived);
                }
            });
        }

        public void Stopped()
        {
            Task.Factory.StartNew(() =>
            {
                foreach (var tracker in Trackers)
                {
                    tracker.Stopped(Data.InfoHash, Monitor.BytesReceived,
                                    Monitor.BytesSent, Monitor.TotalBytes - Monitor.BytesReceived);
                }
            });
        }

        private void Regular(object o)
        {   //стартиране на нова асинхронна задача
            Task.Factory.StartNew(() =>
            {   //итериране през всички налични тракери
                foreach (var tracker in Trackers)
                {   //ако периодът, в който тракерът иска повторно да се свържем с него, е изтекъл, продължаваме
                    if (tracker.LastAnnounced.Add(tracker.Period) < DateTime.Now && tracker.LastState != AnnounceState.None)
                    {   //ако последния статус на тракера не е бил StartFailure, правим обикновена регулярна заявка
                        if (tracker.LastState != AnnounceState.StartFailure)
                        {
                            tracker.Regular(Data.InfoHash, Monitor.BytesReceived,
                                            Monitor.BytesSent, Monitor.TotalBytes - Monitor.BytesReceived);
                        }
                        else //в противен случай, повтаряме заявката Started, докато тя не стане успешна
                        {    //по подразбиране, периодът на повтаряне на 20 секунди
                            tracker.Started(Data.InfoHash, Monitor.BytesReceived,
                                            Monitor.BytesSent, Monitor.TotalBytes - Monitor.BytesReceived);
                        }
                    }
                }
            });
        }

        public event EventHandler<EventArgs<IEnumerable<IPEndPoint>>> PeersReceived;

        public void OnPeersReceived(IEnumerable<IPEndPoint> e)
        {
            if (disposed) return;
            EventHandler<EventArgs<IEnumerable<IPEndPoint>>> handler = PeersReceived;
            if(handler != null) handler(this, new EventArgs<IEnumerable<IPEndPoint>>(e));
        }

        public void Dispose()
        {
            if(!disposed)
            {
                regularTimer.Dispose();
                disposed = true;
            }
        }
    }
}
