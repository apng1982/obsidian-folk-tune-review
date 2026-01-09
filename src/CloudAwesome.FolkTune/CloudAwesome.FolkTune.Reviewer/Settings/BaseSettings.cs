using System.ComponentModel;
using Spectre.Console.Cli;

namespace CloudAwesome.FolkTune.Reviewer.Settings
{
    public class BaseSettings : CommandSettings
    {
        [CommandOption("--vault <PATH>")]
        [Description("Path to the Obsidian vault root")]
        public string? VaultPath { get; set; }

        [CommandOption("--store <PATH>")]
        [Description("Path to the review store JSON file")]
        public string? StorePath { get; set; }

        [CommandOption("--dry-run")]
        [Description("Do not save any changes")]
        public bool DryRun { get; set; }
    }
}
