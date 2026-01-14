using System.ComponentModel;
using Spectre.Console.Cli;

namespace CloudAwesome.FolkTune.Reviewer.Settings
{
    public class PickSettings : BaseSettings
    {
        [CommandOption("--count <N>")]
        [Description("Number of tunes to select")]
        [DefaultValue(10)]
        public int Count { get; set; }

        [CommandOption("--origin <TEXT>")]
        [Description("Filter by origin")]
        public string? Origin { get; set; }

        [CommandOption("--include-session")]
        [Description("Include tunes marked as session-maintained")]
        public bool IncludeSession { get; set; }

        [CommandOption("--include-excluded")]
        [Description("Include tunes marked as excluded")]
        public bool IncludeExcluded { get; set; }

    }
}
