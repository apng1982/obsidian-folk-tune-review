using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace CloudAwesome.FolkTune.Models
{
    public class ReviewStore
    {
        [JsonProperty("schemaVersion")]
        public int SchemaVersion { get; set; } = 1;

        [JsonProperty("updatedUtc")]
        public DateTime UpdatedUtc { get; set; }

        [JsonProperty("tunes")]
        public Dictionary<string, TuneReviewRecord> Tunes { get; set; } = new Dictionary<string, TuneReviewRecord>();
    }

    public class TuneReviewRecord
    {
        [JsonProperty("exclude")]
        public bool Exclude { get; set; }

        [JsonProperty("maintenance")]
        public string Maintenance { get; set; } = "self";

        [JsonProperty("last")]
        public string Last { get; set; }

        [JsonProperty("sessionLast")]
        public string SessionLast { get; set; }

        [JsonProperty("intervalDays")]
        public int? IntervalDays { get; set; }

        [JsonProperty("score")]
        public int? Score { get; set; }

        [JsonProperty("notes")]
        public string Notes { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }
    }
}
