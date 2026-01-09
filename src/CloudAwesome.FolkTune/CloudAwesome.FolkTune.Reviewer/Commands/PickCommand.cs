using System;
using System.IO;
using System.Threading;
using CloudAwesome.FolkTune.Reviewer.Settings;
using CloudAwesome.FolkTune.Services;
using CloudAwesome.FolkTune.Helpers;
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
                var storePath = settings.StorePath ?? Path.Combine(vaultPath, ".tune-review", "reviews.json");

                var engine = new ReviewEngine(new VaultScanner(), new ReviewStoreManager(), new SelectionService(), new IdInitializer());
                engine.Load(vaultPath, storePath);

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
                if (settings.PrintPaths)
                {
                    table.AddColumn("Path");
                }

                foreach (var tune in candidates)
                {
                    var origin = WikiLinkHelper.ExtractDisplayText(tune.Origin);
                    if (settings.PrintPaths)
                    {
                        table.AddRow(tune.Title ?? string.Empty, origin ?? string.Empty, tune.FilePath ?? string.Empty);
                    }
                    else
                    {
                        table.AddRow(tune.Title ?? string.Empty, origin ?? string.Empty);
                    }
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
