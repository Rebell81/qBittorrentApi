using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace qBittorrent.qBittorrentApi.Test
{
    public static class TestExtension
    {
        public static async Task WaitForTorrentToStart(this Api api, string hash, int numberOfTry = 20, int delay = 400)
        {
            var torrentStarted = false;
            for (var i = 0; i < numberOfTry; i++)
            {
                var torrents = await api.GetTorrents();
                if (torrents.SingleOrDefault(t => t.Hash == hash)?.State != "stalledDL")
                {
                    torrentStarted = true;
                    break;
                }

                Thread.Sleep(delay);
            }
            if (!torrentStarted)
            {
                throw new TimeoutException();
            }
        }
    }
}