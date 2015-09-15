using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security;
using System.Threading.Tasks;
using Microsoft.Framework.Configuration;
using Xunit;

namespace qBittorrent.qBittorrentApi.Test
{
    public class Functional
    {
        private readonly ServerCredential _serverCredential;

        public Functional()
        {
            var builder = new ConfigurationBuilder(".");
            builder.AddJsonFile("config.json");
            builder.AddJsonFile("config.private.json", true);
            var configuration = builder.Build();

            var uri = new Uri(configuration["ServerCredentialUri"]);
            var username = configuration["ServerCredentialUsername"];
            var password = configuration["ServerCredentialPassword"];

            _serverCredential = new ServerCredential(uri, username, password);
        }

        [Fact]
        public async Task BadCredential()
        {
            var api = new Api(new ServerCredential(_serverCredential.Uri, _serverCredential.Username, "WrongPassword"));

            await Assert.ThrowsAsync<SecurityException>(async () =>
            {
                try
                {
                    await api.GetTorrents();
                }
                catch (AggregateException aggregateException)
                {
                    throw aggregateException.InnerException;
                }
            });
        }

        [Fact]
        public async Task GetApiVersion()
        {
            var api = new Api(_serverCredential);
            var apiVersion = await api.GetApiVersion();

            Assert.Equal(4, apiVersion);
        }

        [Fact]
        public async Task GetApiMinVersion()
        {
            var api = new Api(_serverCredential);
            var apiMinVersion = await api.GetApiMinVersion();

            Assert.Equal(2, apiMinVersion);
        }

        [Fact]
        public async Task GetQBittorrentVersion()
        {
            var api = new Api(_serverCredential);
            var qBittorrentVersion = await api.GetQBittorrentVersion();

            Assert.Equal("v3.2.3", qBittorrentVersion);
        }

        [Fact]
        public async Task GetTorrents()
        {
            var api = new Api(_serverCredential);

            var torrents = await api.GetTorrents();

            Assert.Equal(0, torrents.Count);
        }

        [Fact]
        public async Task GetTorrentsWithFilter()
        {
            var api = new Api(_serverCredential);

            var torrents = await api.GetTorrents(Filter.Completed);

            Assert.Equal(0, torrents.Count);
        }

        [Fact]
        public async Task GetTorrentsWithLabel()
        {
            var api = new Api(_serverCredential);

            var torrents = await api.GetTorrents(Filter.All, "test");

            Assert.Equal(0, torrents.Count);
        }

        [Fact]
        public async Task TestDeleteAndDownload()
        {
            var api = new Api(_serverCredential);

            var uris = new[]
            {
                new Uri(
                    "magnet:?xt=urn:btih:cd8158937344b2a066446bed7e7a0c45214f1245&dn=debian-8.2.0-amd64-DVD-1.iso&tr=http%3a%2f%2fbttracker.debian.org%3a6969%2fannounce"),
                new Uri(
                    "magnet:?xt=urn:btih:e508e5f1c7ad6650eb41d38f29aa567923b3934f&dn=debian-8.2.0-amd64-DVD-2.iso&tr=http%3a%2f%2fbttracker.debian.org%3a6969%2fannounce")
            };

            var hashes = new[]
            {
                "cd8158937344b2a066446bed7e7a0c45214f1245",
                "e508e5f1c7ad6650eb41d38f29aa567923b3934f"
            };

            // Cleanup
            await api.DeletePermanently(hashes);

            var initialCount = (await api.GetTorrents()).Count;

            await api.DownloadFromUrls(uris);

            var after2AddCount = (await api.GetTorrents()).Count;
            Assert.Equal(initialCount + 2, after2AddCount);

            await api.DeletePermanently(hashes);
            var after2Delete = (await api.GetTorrents()).Count;

            Assert.Equal(initialCount, after2Delete);
        }

        [Fact]
        public async Task TestDeleteAndUpload()
        {
            var api = new Api(_serverCredential);

            var initialTorrents = await api.GetTorrents();

            var streams = new[]
            {
                await
                    new HttpClient().GetStreamAsync(
                        new Uri("http://releases.ubuntu.com/15.04/ubuntu-15.04-desktop-amd64.iso.torrent")),
                await
                    new HttpClient().GetStreamAsync(
                        new Uri("http://releases.ubuntu.com/15.04/ubuntu-15.04-desktop-i386.iso.torrent"))
            };

            await api.Upload(streams);

            var afterUploadTorrents = await api.GetTorrents();

            Assert.Equal(initialTorrents.Count + streams.Length, afterUploadTorrents.Count);

            var hashList =
                afterUploadTorrents.Select(t => t.Hash)
                    .ToList()
                    .Except(initialTorrents.Select(t => t.Hash).ToList())
                    .ToList();
            Assert.Equal(streams.Length, hashList.Count());

            await api.DeletePermanently(hashList);
            var afterDeleteTorrents = await api.GetTorrents();

            Assert.Equal(afterDeleteTorrents.Count, initialTorrents.Count);
        }

        [Fact]
        public async Task TestGetGeneralProperties()
        {
            var api = new Api(_serverCredential);

            var uris = new[]
            {
                new Uri("magnet:?xt=urn:btih:cd8158937344b2a066446bed7e7a0c45214f1245&dn=debian-8.2.0-amd64-DVD-1.iso&tr=http%3a%2f%2fbttracker.debian.org%3a6969%2fannounce")
            };
            var hashes = new[]
            {
                "cd8158937344b2a066446bed7e7a0c45214f1245"
            };

            await api.DownloadFromUrls(uris);

            await api.WaitForTorrentToStart(hashes.FirstOrDefault());
            
            var generalProperties = await api.GetGeneralProperties(hashes.SingleOrDefault());

            Assert.True(generalProperties.TimeElapsed > 0);

            await api.DeletePermanently(hashes);
        }

        [Fact]
        public async Task TestGetTrackersProperties()
        {
            var api = new Api(_serverCredential);

            var uris = new[]
            {
                new Uri("magnet:?xt=urn:btih:cd8158937344b2a066446bed7e7a0c45214f1245&dn=debian-8.2.0-amd64-DVD-1.iso&tr=http%3a%2f%2fbttracker.debian.org%3a6969%2fannounce")
            };
            var hashes = new[]
            {
                "cd8158937344b2a066446bed7e7a0c45214f1245"
            };

            await api.DownloadFromUrls(uris);

            await api.WaitForTorrentToStart(hashes.FirstOrDefault());

            var trackersPropertieses = await api.GetTrackersProperties(hashes.SingleOrDefault());

            Assert.True(trackersPropertieses.Any(p => p.Url.Contains("http://bttracker.debian.org:6969/announce")));

            await api.DeletePermanently(hashes);
        }
    }
}