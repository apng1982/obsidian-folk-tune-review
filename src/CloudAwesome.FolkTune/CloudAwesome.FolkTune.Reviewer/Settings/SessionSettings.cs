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

        [CommandOption("--from-file <PATH>")]
        [Description("Path to a file containing ids or titles (one per line)")]
        public string? FromFile { get; set; }
    }
}
