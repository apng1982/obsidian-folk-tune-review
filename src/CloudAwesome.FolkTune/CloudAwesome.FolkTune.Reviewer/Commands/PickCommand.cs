using CloudAwesome.FolkTune.Reviewer.Settings;
using CloudAwesome.FolkTune.Services;
using CloudAwesome.FolkTune.Helpers;
using CloudAwesome.FolkTune.Reviewer.Helpers;
using Spectre.Console;
using Spectre.Console.Cli;

namespace CloudAwesome.FolkTune.Reviewer.Commands
{
    public class PickCommand : Command<PickSettings>
    {
        public override int Execute(CommandContext context, PickSettings settings, CancellationToken cancellationToken)
        {
            try
            {
                var vaultPath = settings.VaultPath ?? Directory.GetCurrentDirectory();
                var vaultName = Path.GetFileName(vaultPath.TrimEnd(Path.DirectorySeparatorChar));
                var storePath = settings.StorePath ?? VaultStructure.GetDefaultReviewStorePath(vaultPath);

                var engine = new ReviewEngine(new VaultScanner(), new ReviewStoreManager(), new SelectionService(), new IdInitializer());
                var loadResult = engine.Load(vaultPath, storePath);

                CommandOutputHelper.WriteReviewStoreCreatedMessage(loadResult);
                
                var options = new SelectionService.SelectionOptions
                {
                    Count = settings.Count,
                    OriginFilter = settings.Origin,
                    IncludeExcluded = settings.IncludeExcluded,
                    IncludeSession = settings.IncludeSession,
                    Today = DateTime.Today
                };

                var candidates = engine.GetReviewCandidates(options);

                if (candidates.Count == 0)
                {
                    AnsiConsole.MarkupLine("[yellow]No tunes found matching the criteria.[/]");
                    return 0;
                }

                var table = new Table();
                table.AddColumn("Title");
                table.AddColumn("Origin");

                foreach (var tune in candidates)
                {
                    var origin = WikiLinkHelper.ExtractDisplayText(tune.Origin);
                    var encodedTitle = Uri.EscapeDataString(tune.Title ?? string.Empty);
                    var obsidianUrl = $"obsidian://open?vault={Uri.EscapeDataString(vaultName)}&file={encodedTitle}";
                        
                    var titleMarkup = $"[link={obsidianUrl}]{tune.Title}[/]";
                    
                    table.AddRow(titleMarkup, origin ?? string.Empty);
                }

                AnsiConsole.Write(table);
                AnsiConsole.MarkupLine($"\n[green]Selected {candidates.Count} tunes.[/]");

                return 0;
            }
            catch (Exception ex)
            {
                AnsiConsole.WriteException(ex);
                return 1;
            }
        }
    }
}
