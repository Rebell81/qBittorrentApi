using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Authentication;
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

            await Assert.ThrowsAsync<AuthenticationException>(async () =>
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

            var uris = new List<Uri>
            {
                new Uri(
                    "magnet:?xt=urn:btih:7FF6F24A0197E040524513C633BF476FD2565D04&dn=udemy+the+comprehensive+guide+to+c&tr=udp%3A%2F%2Fopen.demonii.com%3A1337%2Fannounce"),
                new Uri(
                    "magnet:?xt=urn:btih:A8D0846144C3E7A6945EEBE2D7F9DFBE8C839A86&dn=continuum+s04e01+lost+hours+web+dl+x264+fum+ettv&tr=udp%3A%2F%2Ftracker.publicbt.com%2Fannounce&tr=udp%3A%2F%2Fopen.demonii.com%3A1337")
            };

            var hashes = new List<string>
            {
                "7FF6F24A0197E040524513C633BF476FD2565D04",
                "A8D0846144C3E7A6945EEBE2D7F9DFBE8C839A86"
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

            var streams = new List<Stream>
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

            Assert.Equal(initialTorrents.Count + streams.Count, afterUploadTorrents.Count);

            var hashList =
                afterUploadTorrents.Select(t => t.Hash)
                    .ToList()
                    .Except(initialTorrents.Select(t => t.Hash).ToList())
                    .ToList();
            Assert.Equal(streams.Count, hashList.Count());

            await api.DeletePermanently(hashList);
            var afterDeleteTorrents = await api.GetTorrents();

            Assert.Equal(afterDeleteTorrents.Count, initialTorrents.Count);
        }
    }
}