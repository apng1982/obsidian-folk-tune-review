using System.ComponentModel;
using Spectre.Console.Cli;

namespace CloudAwesome.FolkTune.Reviewer.Settings
{
    public class IdsInitSettings : BaseSettings
    {
        [CommandOption("-r|--root <subfolder>")]
        [Description("Subfolder within the vault to scan (optional; default scans all)")]
        public string? Root { get; set; }

        [CommandOption("--dry-run")]
        [Description("Show what would change without modifying files")]
        public bool DryRun { get; set; }

        [CommandOption("--limit <n>")]
        [Description("Limit the number of files to update")]
        public int? Limit { get; set; }

        [CommandOption("--include-existing")]
        [Description("Re-generate IDs even for files that already have one (default false)")]
        public bool IncludeExisting { get; set; }
    }
}
