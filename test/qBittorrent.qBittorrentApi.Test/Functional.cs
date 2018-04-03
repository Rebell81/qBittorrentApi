﻿using System;
using System.Linq;
using System.Net.Http;
using System.Security;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace qBittorrent.qBittorrentApi.Test
{
    public class Functional
    {
        private readonly ServerCredential _serverCredential;

        public Functional()
        {
            var builder = new ConfigurationBuilder();
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

        //[Fact]
        //public async Task Shutdown()
        //{
        //    var api = new Api(_serverCredential);
        //    var result = await api.Shutdown();

        //    Assert.True(result);
        //}

        [Fact]
        public async Task GetApiVersion()
        {
            var api = new Api(_serverCredential);
            var apiVersion = await api.GetApiVersion();

            Assert.True(10 <= apiVersion);
        }

        [Fact]
        public async Task GetApiMinVersion()
        {
            var api = new Api(_serverCredential);
            var apiMinVersion = await api.GetApiMinVersion();

            Assert.True(10 <= apiMinVersion);
        }

        [Fact]
        public async Task GetQBittorrentVersion()
        {
            var api = new Api(_serverCredential);
            var qBittorrentVersion = await api.GetQBittorrentVersion();

            Assert.Equal(4, qBittorrentVersion.Major);
            Assert.Equal(0, qBittorrentVersion.Minor);
            Assert.True(4 <= qBittorrentVersion.Patch);
        }

        [Fact]
        public async Task GetTorrents()
        {
            var api = new Api(_serverCredential);

            var torrents = await api.GetTorrents();

            Assert.NotNull(torrents);
            if (torrents.Any())
            {
                Assert.True(!string.IsNullOrWhiteSpace(torrents.First().Hash));
            }
        }

        [Fact]
        public async Task GetTorrentsWithFilter()
        {
            var api = new Api(_serverCredential);

            var torrents = await api.GetTorrents(Filter.Completed);

            Assert.NotNull(torrents);
            if (torrents.Any())
            {
                Assert.True(!string.IsNullOrWhiteSpace(torrents.First().Hash));
            }
        }

        [Fact]
        public async Task GetTorrentsWithCategory()
        {
            var api = new Api(_serverCredential);

            var torrents = await api.GetTorrents(Filter.All, "test");

            Assert.Equal(0, torrents.Count);
        }

        [Fact]
        public async Task TestDownloadFromUrls()
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

            var initialCount = (await api.GetTorrents()).Count;

            await api.DownloadFromUrls(uris);

            foreach(var hash in hashes)
            {
                //await api.WaitForTorrentToBePausedByHash(hash);
                //await api.Resume(hash);
                await api.WaitForTorrentToStartByHash(hash);
            }

            var after2AddCount = (await api.GetTorrents()).Count;
            Assert.Equal(initialCount + 2, after2AddCount);

            await api.DeletePermanently(hashes);
            var after2Delete = (await api.GetTorrents()).Count;

            Assert.Equal(initialCount, after2Delete);
        }

        [Fact]
        public async Task TestUpload()
        {
            var api = new Api(_serverCredential);

            var initialTorrents = await api.GetTorrents();

            var bytes = new[]
            {
                await
                    new HttpClient().GetByteArrayAsync(
                        new Uri("http://releases.ubuntu.com/16.04/ubuntu-16.04.4-server-amd64.iso.torrent")),
                await
                    new HttpClient().GetByteArrayAsync(
                        new Uri("http://releases.ubuntu.com/16.04/ubuntu-16.04.4-desktop-amd64.iso.torrent"))
            };

            await api.Upload(bytes);

            await api.WaitForTorrentToStartByName("ubuntu-16.04.4-server-amd64.iso");
            await api.WaitForTorrentToStartByName("ubuntu-16.04.4-desktop-amd64.iso");

            var afterUploadTorrents = await api.GetTorrents();

            Assert.Equal(initialTorrents.Count + bytes.Length, afterUploadTorrents.Count);

            var hashList =
                afterUploadTorrents.Select(t => t.Hash)
                    .ToList()
                    .Except(initialTorrents.Select(t => t.Hash).ToList())
                    .ToList();
            Assert.Equal(bytes.Length, hashList.Count());

            var setCategoryResult = await api.SetCategory(hashList, "ubuntu");

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
                new Uri(
                    "magnet:?xt=urn:btih:f092b5fa9f01dee17dd40b75f91b85f46a38227c&dn=debian-9.4.0-amd64-DVD-1.iso&tr=http%3a%2f%2fbttracker.debian.org%3a6969%2fannounce")
            };
            var hashes = new[]
            {
                "f092b5fa9f01dee17dd40b75f91b85f46a38227c"
            };

            await api.DownloadFromUrls(uris);

            await api.WaitForTorrentToBePausedByHash(hashes.FirstOrDefault());

            await api.Resume(hashes.FirstOrDefault());

            await api.WaitForTorrentToStartByHash(hashes.FirstOrDefault());

            var generalProperties = await api.GetGeneralProperties(hashes.SingleOrDefault());

            Assert.True(generalProperties.TimeElapsed >= 0);

            await api.DeletePermanently(hashes);
        }

        [Fact]
        public async Task TestGetProperties()
        {
            var api = new Api(_serverCredential);

            var uris = new[]
            {
                new Uri(
                    "magnet:?xt=urn:btih:f092b5fa9f01dee17dd40b75f91b85f46a38227c&dn=debian-9.4.0-amd64-DVD-1.iso&tr=http%3a%2f%2fbttracker.debian.org%3a6969%2fannounce")
            };
            var hashes = new[]
            {
                "f092b5fa9f01dee17dd40b75f91b85f46a38227c"
            };

            await api.DownloadFromUrls(uris);

            //await api.WaitForTorrentToBePausedByHash(hashes.FirstOrDefault());

            //await api.Resume(hashes.FirstOrDefault());

            await api.WaitForTorrentToStartByHash(hashes.FirstOrDefault());

            var trackersPropertieses = await api.GetTrackersProperties(hashes.SingleOrDefault());

            Assert.True(trackersPropertieses.Any(p => p.Url.Contains("http://bttracker.debian.org:6969/announce")));

            var filesPropertieses = await api.GetFilesProperties(hashes.SingleOrDefault());
            var file = filesPropertieses.Single();
            Assert.Equal("debian-9.4.0-amd64-DVD-1.iso", file.Name);
            Assert.Equal(3977379840, file.Size);

            var transferInfo = await api.GetTransferInfo();
            Assert.True(transferInfo.DhtNodes >= 0);

            var setCategoryResult = await api.SetCategory(hashes, "linux-distro");
            Assert.True(transferInfo.DhtNodes >= 0);

            var recheckResult = await api.Recheck(hashes.SingleOrDefault());
            Assert.True(recheckResult);

            var addTrackerResult = await api.AddTrackers(hashes.SingleOrDefault(), new[] { new Uri("http://test1/"), new Uri("http://test2/") });
            Assert.True(addTrackerResult);

            await api.DeletePermanently(hashes);
        }
    }
}