using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace qBittorrent.qBittorrentApi.Test
{
    public static class TestExtension
    {
        public static async Task WaitForTorrentToStartByHash(this Api api, string hash, int numberOfTry = 20,
            int delay = 400)
        {
            var torrentStarted = false;
            for (var i = 0; i < numberOfTry; i++)
            {
                var torrents = await api.GetTorrents();
                if (torrents.SingleOrDefault(t => t.Hash == hash)?.State == "downloading")
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

        public static async Task WaitForTorrentToBePausedByHash(this Api api, string hash, int numberOfTry = 20, int delay = 400)
        {
            var torrentPaused = false;
            for (var i = 0; i < numberOfTry; i++)
            {
                var torrents = await api.GetTorrents();
                if (torrents.SingleOrDefault(t => t.Hash == hash)?.State == "pausedDL")
                {
                    torrentPaused = true;
                    break;
                }

                Thread.Sleep(delay);
            }
            if (!torrentPaused)
            {
                throw new TimeoutException();
            }
        }

        public static async Task WaitForTorrentToStartByName(this Api api, string name, int numberOfTry = 20,
            int delay = 400)
        {
            var torrentStarted = false;
            for (var i = 0; i < numberOfTry; i++)
            {
                var torrents = await api.GetTorrents();
                if (torrents.SingleOrDefault(t => t.Name == name)?.State != "stalledDL")
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