using System;
using System.IO;
using System.Linq;
using System.Threading;
using CloudAwesome.FolkTune.Reviewer.Settings;
using CloudAwesome.FolkTune.Services;
using CloudAwesome.FolkTune.Helpers;
using CloudAwesome.FolkTune.Models;
using Spectre.Console;
using Spectre.Console.Cli;

namespace CloudAwesome.FolkTune.Reviewer.Commands
{
    public class ReviewCommand : Command<ReviewSettings>
    {
        public override int Execute(CommandContext context, ReviewSettings settings, CancellationToken cancellationToken)
        {
            try
            {
                var vaultPath = settings.VaultPath ?? Directory.GetCurrentDirectory();
                var vaultName = Path.GetFileName(vaultPath.TrimEnd(Path.DirectorySeparatorChar));
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
                
                List<TuneNote> candidates;
                if (!string.IsNullOrEmpty(settings.Tune))
                {
                    candidates = engine.FindTunes(settings.Tune);
                    if (candidates.Count == 0)
                    {
                        AnsiConsole.MarkupLine($"[red]Could not find tune matching:[/] [yellow]{settings.Tune}[/]");
                        return 1;
                    }
                    if (candidates.Count > 1)
                    {
                        AnsiConsole.MarkupLine($"[yellow]Found multiple matches for '{settings.Tune}'. Please be more specific.[/]");
                        foreach(var c in candidates) AnsiConsole.MarkupLine($" - {c.Title}");
                        return 1;
                    }
                }
                else
                {
                    candidates = engine.GetReviewCandidates(options);
                }
                
                if (candidates.Count == 0)
                {
                    AnsiConsole.MarkupLine("[yellow]No tunes found matching the criteria.[/]");
                    return 0;
                }

                int reviewedCount = 0;
                int skippedCount = 0;

                foreach (var tune in candidates)
                {
                    AnsiConsole.Clear();
                    
                    var encodedTitle = Uri.EscapeDataString(tune.Title ?? string.Empty);
                    var obsidianUrl = $"obsidian://open?vault={Uri.EscapeDataString(vaultName)}&file={encodedTitle}";
                        
                    AnsiConsole.Write(new Rule($"[bold blue]Review: [link={obsidianUrl}]{tune.Title}[/][/]").LeftJustified());
                    
                    var table = new Table().NoBorder();
                    table.AddColumn("Field");
                    table.AddColumn("Value");
                    
                    table.AddRow("Origin", WikiLinkHelper.ExtractDisplayText(tune.Origin));
                    table.AddRow("Type", WikiLinkHelper.ExtractDisplayText(tune.Type));
                    table.AddRow("Key", WikiLinkHelper.ExtractDisplayText(tune.Key));
                    
                    AnsiConsole.Write(table);
                    AnsiConsole.WriteLine();

                    var prompt = new TextPrompt<string>("Action [0-4, s, x, m, n]:")
                        .Validate(input => 
                        {
                            return input.ToLower() switch
                            {
                                "0" or "1" or "2" or "3" or "4" or "s" or "x" or "m" or "n" => ValidationResult.Success(),
                                _ => ValidationResult.Error("[red]Invalid action[/]")
                            };
                        });

                    var action = AnsiConsole.Prompt(prompt).ToLower();

                    if (action == "s")
                    {
                        skippedCount++;
                        continue;
                    }

                    if (action == "x")
                    {
                        engine.ExcludeTune(tune.Id);
                        AnsiConsole.MarkupLine("[yellow]Tune excluded.[/]");
                    }
                    else if (action == "m")
                    {
                        engine.MarkAsSessionMaintained(tune.Id);
                        AnsiConsole.MarkupLine("[yellow]Marked as session-maintained.[/]");
                    }
                    else if (action == "n")
                    {
                        var notes = AnsiConsole.Ask<string>("Enter notes:");
                        // We still need a score if we want to record the review, 
                        // or we just attach notes. Requirements say "n -> prompt for a short notes string saved to review record"
                        // I'll assume it doesn't finish the review for this tune yet, OR it asks for score after notes.
                        // Let's ask for score after notes.
                        var scoreStr = AnsiConsole.Prompt(new TextPrompt<string>("Score [0-4]:").Validate(s => "01234".Contains(s) ? ValidationResult.Success() : ValidationResult.Error("0-4 only")));
                        engine.SubmitReview(tune.Id, int.Parse(scoreStr), notes);
                        reviewedCount++;
                    }
                    else if (int.TryParse(action, out var score))
                    {
                        engine.SubmitReview(tune.Id, score);
                        reviewedCount++;
                    }

                    AnsiConsole.WriteLine("Press any key for next tune...");
                    Console.ReadKey(true);
                }

                AnsiConsole.Clear();
                AnsiConsole.Write(new Rule("[bold green]Session Summary[/]").LeftJustified());
                AnsiConsole.MarkupLine($"Reviewed: [green]{reviewedCount}[/]");
                AnsiConsole.MarkupLine($"Skipped: [yellow]{skippedCount}[/]");

                if (!settings.DryRun && candidates.Count > 0)
                {
                    engine.Save();
                    AnsiConsole.MarkupLine("[green]Review store updated.[/]");
                }
                else if (settings.DryRun)
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
