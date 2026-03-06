using CloudAwesome.FolkTune.Services;
using Spectre.Console;

namespace CloudAwesome.FolkTune.Reviewer.Helpers
{
    public static class CommandOutputHelper
    {
        public static void WriteReviewStoreCreatedMessage(ReviewEngineLoadResult loadResult)
        {
            if (!loadResult.ReviewStoreCreated)
            {
                return;
            }

            AnsiConsole.MarkupLine("[bold yellow]A new review store was created.[/]");
            AnsiConsole.MarkupLine($"[yellow]Path:[/] {loadResult.StorePath}");
            AnsiConsole.MarkupLine("[yellow]If you were not expecting this, check that you are running the CLI from the root of your Obsidian vault, or pass --vault <PATH> explicitly.[/]");
            AnsiConsole.WriteLine();
        }
    }
}
