using System;
using System.ComponentModel;
using Spectre.Console.Cli;

namespace CloudAwesome.FolkTune.Reviewer.Settings
{
    public class SessionSettings : BaseSettings
    {
        [CommandOption("--date <YYYY-MM-DD>")]
        [Description("Date the tunes were played (default today)")]
        public string? Date { get; set; }

        [CommandOption("--origin <TEXT>")]
        [Description("Select tunes by origin for bulk marking")]
        public string? Origin { get; set; }

        [CommandOption("--count <N>")]
        [Description("Number of tunes to mark (used with --origin)")]
        public int? Count { get; set; }

        [CommandOption("--from-file <PATH>")]
        [Description("Path to a file containing ids or titles (one per line)")]
        public string? FromFile { get; set; }
    }
}
