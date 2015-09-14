using Newtonsoft.Json;

namespace qBittorrent.qBittorrentApi
{
    public class GeneralProperties
    {
        [JsonProperty("comment")]
        public string Comment { get; set; }

        [JsonProperty("creation_date")]
        public int CreationDate { get; set; }

        [JsonProperty("dl_limit")]
        public int DlLimit { get; set; }

        [JsonProperty("nb_connections")]
        public int NbConnections { get; set; }

        [JsonProperty("nb_connections_limit")]
        public int NbConnectionsLimit { get; set; }

        [JsonProperty("piece_size")]
        public int PieceSize { get; set; }

        [JsonProperty("save_path")]
        public string SavePath { get; set; }

        [JsonProperty("seeding_time")]
        public int SeedingTime { get; set; }

        [JsonProperty("share_ratio")]
        public double ShareRatio { get; set; }

        [JsonProperty("time_elapsed")]
        public int TimeElapsed { get; set; }

        [JsonProperty("total_downloaded")]
        public int TotalDownloaded { get; set; }

        [JsonProperty("total_downloaded_session")]
        public int TotalDownloadedSession { get; set; }

        [JsonProperty("total_uploaded")]
        public int TotalUploaded { get; set; }

        [JsonProperty("total_uploaded_session")]
        public int TotalUploadedSession { get; set; }

        [JsonProperty("total_wasted")]
        public int TotalWasted { get; set; }

        [JsonProperty("up_limit")]
        public int UpLimit { get; set; }
    }
}