using Newtonsoft.Json;

namespace qBittorrent.qBittorrentApi
{
    public class TransferInfo
    {

        [JsonProperty("connection_status")]
        public string ConnectionStatus { get; set; }

        [JsonProperty("dht_nodes")]
        public int DhtNodes { get; set; }

        [JsonProperty("dl_info_data")]
        public int DlInfoData { get; set; }

        [JsonProperty("dl_info_speed")]
        public int DlInfoSpeed { get; set; }

        [JsonProperty("dl_rate_limit")]
        public int DlRateLimit { get; set; }

        [JsonProperty("up_info_data")]
        public long UpInfoData { get; set; }

        [JsonProperty("up_info_speed")]
        public int UpInfoSpeed { get; set; }

        [JsonProperty("up_rate_limit")]
        public int UpRateLimit { get; set; }
    }

}
