using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace qBittorrent.qBittorrentApi
{
    public class Api
    {
        private readonly HttpClient _httpClient;
        private readonly HttpClientHandler _httpClientHandler;
        private readonly ServerCredential _serverCredential;

        public Api(ServerCredential argServerCredential)
        {
            _serverCredential = argServerCredential;
            _httpClientHandler = new HttpClientHandler();
            _httpClient = new HttpClient(_httpClientHandler) {BaseAddress = _serverCredential.Uri};
        }

        private bool IsAuthenticated()
        {
            return _httpClientHandler.CookieContainer.GetCookies(_httpClient.BaseAddress)["SID"] != null;
        }

        private async Task CheckAuthentification()
        {
            if (!IsAuthenticated())
            {
                if (!await Login())
                {
                    throw new SecurityException();
                }
            }
        }

        private async Task<bool> Login()
        {
            HttpContent bodyContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("username", _serverCredential.Username),
                new KeyValuePair<string, string>("password", _serverCredential.Password)
            });

            var uri = new Uri("/login", UriKind.Relative);

            await _httpClient.PostAsync(uri, bodyContent);

            return IsAuthenticated();
        }

        public async Task<int> GetApiVersion()
        {
            return int.Parse(await _httpClient.GetStringAsync(new Uri("/version/api", UriKind.Relative)));
        }

        public async Task<int> GetApiMinVersion()
        {
            return int.Parse(await _httpClient.GetStringAsync(new Uri("/version/api_min", UriKind.Relative)));
        }

        public async Task<string> GetQBittorrentVersion()
        {
            return await _httpClient.GetStringAsync(new Uri("/version/qbittorrent", UriKind.Relative));
        }

        public async Task<IList<Torrent>> GetTorrents(Filter filter = Filter.All, string label = null)
        {
            await CheckAuthentification();

            var keyValuePairs = new KeyValuePair<string, string>[2];
            keyValuePairs.SetValue(new KeyValuePair<string, string>("filter", filter.ToString().ToLower()), 0);


            if (label != null)
            {
                keyValuePairs.SetValue(new KeyValuePair<string, string>("label", label), 1);
            }

            HttpContent content = new FormUrlEncodedContent(keyValuePairs);

            var uri = new Uri("/query/torrents?" + await content.ReadAsStringAsync(), UriKind.Relative);
            var response = await _httpClient.GetAsync(uri);
            var jsonStr = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<IList<Torrent>>(jsonStr);
        }

        public async Task<GeneralProperties> GetGeneralProperties(string hash)
        {
            var jsonStr =
                await _httpClient.GetStringAsync(new Uri("/query/propertiesGeneral/" + hash, UriKind.Relative));
            return JsonConvert.DeserializeObject<GeneralProperties>(jsonStr);
        }

        public async Task<IList<TrackersProperties>> GetTrackersProperties(string hash)
        {
            var jsonStr =
                await _httpClient.GetStringAsync(new Uri("/query/propertiesTrackers/" + hash, UriKind.Relative));
            return JsonConvert.DeserializeObject<IList<TrackersProperties>>(jsonStr);
        }

        public async Task<bool> DownloadFromUrls(IList<Uri> uris)
        {
            await CheckAuthentification();

            var stringBuilder = new StringBuilder();
            foreach (var uri in uris)
            {
                stringBuilder.Append(uri);
                stringBuilder.Append('\n');
            }

            HttpContent bodyContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("urls", stringBuilder.ToString())
            });

            var uriDownload = new Uri("/command/download", UriKind.Relative);
            var httpResponseMessage = await _httpClient.PostAsync(uriDownload, bodyContent);

            return httpResponseMessage.IsSuccessStatusCode;
        }

        public async Task<bool> Upload(IList<Stream> streams)
        {
            await CheckAuthentification();

            using (var content = new MultipartFormDataContent("Upload----" + DateTime.Now))
            {
                foreach (var stream in streams)
                {
                    var guid = Guid.NewGuid().ToString();
                    content.Add(new StreamContent(stream), guid, guid);
                }

                var uriUpload = new Uri("/command/upload", UriKind.Relative);
                var message = await _httpClient.PostAsync(uriUpload, content);
                return message.IsSuccessStatusCode;
            }
        }

        public async Task<bool> DeletePermanently(IList<string> hashes)
        {
            await CheckAuthentification();

            var stringBuilder = new StringBuilder();
            foreach (var hash in hashes)
            {
                stringBuilder.Append(hash);
                stringBuilder.Append('|');
            }

            HttpContent bodyContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("hashes", stringBuilder.ToString())
            });

            var uriDownload = new Uri("/command/deletePerm", UriKind.Relative);
            var httpResponseMessage = await _httpClient.PostAsync(uriDownload, bodyContent);

            return httpResponseMessage.IsSuccessStatusCode;
        }
    }
}