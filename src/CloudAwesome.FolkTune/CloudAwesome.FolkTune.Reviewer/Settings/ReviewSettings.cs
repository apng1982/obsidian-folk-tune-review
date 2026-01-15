using System.ComponentModel;
using Spectre.Console.Cli;

namespace CloudAwesome.FolkTune.Reviewer.Settings
{
    public class ReviewSettings : PickSettings
    {
        [CommandOption("--tune <NAME_OR_ID>")]
        [Description("Review a specific tune by its title or ID, bypassing automated selection logic")]
        public string? Tune { get; set; }
    }
}
