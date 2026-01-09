using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using CloudAwesome.FolkTune.Models;
using CloudAwesome.FolkTune.Reviewer.Settings;
using CloudAwesome.FolkTune.Services;
using Spectre.Console;
using Spectre.Console.Cli;

namespace CloudAwesome.FolkTune.Reviewer.Commands
{
    public class SessionCommand : Command<SessionSettings>
    {
        public override int Execute(CommandContext context, SessionSettings settings, CancellationToken cancellationToken)
        {
            try
            {
                var vaultPath = settings.VaultPath ?? Directory.GetCurrentDirectory();
                var storePath = settings.StorePath ?? Path.Combine(vaultPath, ".tune-review", "reviews.json");

                var engine = new ReviewEngine(new VaultScanner(), new ReviewStoreManager(), new SelectionService(), new IdInitializer());
                engine.Load(vaultPath, storePath);

                var date = DateTime.Today;
                if (!string.IsNullOrEmpty(settings.Date))
                {
                    if (!DateTime.TryParse(settings.Date, out date))
                    {
                        AnsiConsole.MarkupLine($"[red]Invalid date format: {settings.Date}[/]");
                        return 1;
                    }
                }

                var tunesToMark = new List<TuneNote>();

                if (!string.IsNullOrEmpty(settings.Origin))
                {
                    var options = new SelectionService.SelectionOptions
                    {
                        Count = settings.Count ?? int.MaxValue,
                        OriginFilter = settings.Origin,
                        Today = date
                    };
                    tunesToMark.AddRange(engine.GetReviewCandidates(options));
                }

                if (!string.IsNullOrEmpty(settings.FromFile))
                {
                    if (!File.Exists(settings.FromFile))
                    {
                        AnsiConsole.MarkupLine($"[red]File not found: {settings.FromFile}[/]");
                        return 1;
                    }

                    var lines = File.ReadAllLines(settings.FromFile);
                    foreach (var line in lines)
                    {
                        if (string.IsNullOrWhiteSpace(line)) continue;
                        var found = engine.FindTunes(line.Trim());
                        if (found.Any())
                        {
                            tunesToMark.AddRange(found);
                        }
                        else
                        {
                            AnsiConsole.MarkupLine($"[yellow]Warning: Could not find tune by ID or Title: {line}[/]");
                        }
                    }
                }

                if (tunesToMark.Count == 0)
                {
                    AnsiConsole.MarkupLine("[yellow]No tunes selected to mark as played.[/]");
                    return 0;
                }

                tunesToMark = tunesToMark.DistinctBy(t => t.Id).ToList();

                AnsiConsole.MarkupLine($"Marking [green]{tunesToMark.Count}[/] tunes as played on [blue]{date:yyyy-MM-dd}[/].");
                
                foreach (var tune in tunesToMark)
                {
                    if (string.IsNullOrEmpty(tune.Id)) continue;
                    engine.MarkAsPlayed(tune.Id, date);
                    AnsiConsole.MarkupLine($"- {tune.Title}");
                }

                if (!settings.DryRun)
                {
                    engine.Save();
                    AnsiConsole.MarkupLine("[green]Changes saved.[/]");
                }
                else
                {
                    AnsiConsole.MarkupLine("[yellow]Dry run: No changes saved.[/]");
                }

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
