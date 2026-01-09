using System;
using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace CloudAwesome.FolkTune.Models
{
    public class TuneNote
    {
        [YamlIgnore]
        public string FilePath { get; set; }

        [YamlIgnore]
        public string Title { get; set; }

        [YamlMember(Alias = "tuneId")]
        public string Id { get; set; }

        [YamlMember(Alias = "learn")]
        public bool? Learn { get; set; }

        [YamlMember(Alias = "origin")]
        public object Origin { get; set; }

        [YamlMember(Alias = "type")]
        public object Type { get; set; }

        [YamlMember(Alias = "key")]
        public object Key { get; set; }

        [YamlMember(Alias = "mode")]
        public object Mode { get; set; }

        [YamlMember(Alias = "whistle")]
        public object Whistle { get; set; }

        [YamlMember(Alias = "composer")]
        public object Composer { get; set; }

        [YamlIgnore]
        public bool IsLearned => Learn != true;
    }
}
