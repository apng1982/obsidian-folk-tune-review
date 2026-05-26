using CloudAwesome.FolkTune.Reviewer.Settings;
using CloudAwesome.FolkTune.Services;
using CloudAwesome.FolkTune.Helpers;
using CloudAwesome.FolkTune.Models;
using CloudAwesome.FolkTune.Reviewer.Helpers;
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
                var sessionTunes = candidates
                    .Select(tune => new ReviewSessionTune(tune))
                    .ToList();

                for (var tuneIndex = 0; tuneIndex < sessionTunes.Count; tuneIndex++)
                {
                    var sessionTune = sessionTunes[tuneIndex];
                    var tune = sessionTune.Tune;
                    AnsiConsole.Clear();
                    AnsiConsole.Write(BuildReviewGrid(sessionTunes, tuneIndex, vaultName));
                    AnsiConsole.WriteLine();

                    var prompt = new TextPrompt<string>("Type: 0-9, s, x, m, n")
                        .Validate(input => 
                        {
                            return input.ToLower() switch
                            {
                                "0" or "1" or "2" or "3" or "4" or "5" or "6" or "7" or "8" or "9" or "s" or "x" or "m" or "n" => ValidationResult.Success(),
                                _ => ValidationResult.Error("[red]Invalid action[/]")
                            };
                        });

                    var action = AnsiConsole.Prompt(prompt).ToLower();

                    if (action == "s")
                    {
                        sessionTune.Outcome = new ReviewOutcome(ReviewAction.Skipped);
                        skippedCount++;
                        continue;
                    }

                    if (action == "x")
                    {
                        engine.ExcludeTune(tune.Id);
                        sessionTune.Outcome = new ReviewOutcome(ReviewAction.Excluded);
                        AnsiConsole.MarkupLine("[yellow]Tune excluded.[/]");
                    }
                    else if (action == "m")
                    {
                        engine.MarkAsSessionMaintained(tune.Id);
                        sessionTune.Outcome = new ReviewOutcome(ReviewAction.SessionMaintained);
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
                        var score = int.Parse(scoreStr);
                        engine.SubmitReview(tune.Id, score, notes);
                        sessionTune.Outcome = new ReviewOutcome(ReviewAction.ReviewedWithNotes, score);
                        reviewedCount++;
                    }
                    else if (int.TryParse(action, out var score))
                    {
                        engine.SubmitReview(tune.Id, score);
                        sessionTune.Outcome = new ReviewOutcome(ReviewAction.Reviewed, score);
                        reviewedCount++;
                    }

                    AnsiConsole.WriteLine("Press any key for next tune...");
                    Console.ReadKey(true);
                }

                AnsiConsole.Clear();
                AnsiConsole.Write(new Rule("[bold green]Session Summary[/]").LeftJustified());
                AnsiConsole.MarkupLine($"Reviewed: [green]{reviewedCount}[/]");
                AnsiConsole.MarkupLine($"Skipped: [yellow]{skippedCount}[/]");
                AnsiConsole.WriteLine();
                AnsiConsole.Write(BuildSelectedTunesTable(sessionTunes, null, vaultName));
                AnsiConsole.WriteLine();

                if (!settings.DryRun && candidates.Count > 0)
                {
                    engine.Save();
                    AnsiConsole.MarkupLine($"[green]Review store updated.[/] ({storePath}) ");
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

        private static Grid BuildReviewGrid(IReadOnlyList<ReviewSessionTune> sessionTunes, int currentIndex, string vaultName)
        {
            var leftPanel =
                new Panel(
                    new Rows(
                        BuildSelectedTunesTable(sessionTunes, currentIndex, vaultName),
                        new Markup("\n[bold blue]Inputs[/]\n"),
                        BuildInputInstructions()))
                    .BorderColor(Color.Blue3);

            var rightPanel =
                new Panel(
                        BuildTuneDetails(sessionTunes[currentIndex].Tune, vaultName))
                .BorderColor(Color.Green);

            var grid = new Grid
            {
                Expand = false
            };
            grid.AddColumn(new GridColumn { Padding = new Padding(0, 0, 2, 0) });
            grid.AddColumn(new GridColumn { Padding = new Padding(0) });
            grid.AddRow(leftPanel, rightPanel);

            return grid;
        }

        private static Table BuildSelectedTunesTable(IReadOnlyList<ReviewSessionTune> sessionTunes, int? currentIndex, string vaultName)
        {
            var table = new Table()
                .RoundedBorder()
                .ShowRowSeparators();
            table.AddColumn("Result");
            table.AddColumn("Selected Tunes");
            table.AddColumn("Origin");

            for (var index = 0; index < sessionTunes.Count; index++)
            {
                var sessionTune = sessionTunes[index];
                var tune = sessionTune.Tune;
                var titleMarkup = BuildTuneLink(tune, vaultName);
                var origin = Markup.Escape(WikiLinkHelper.ExtractDisplayText(tune.Origin) ?? string.Empty);

                if (index == currentIndex)
                {
                    table.AddRow("[bold blue]>[/]", $"[bold]{titleMarkup}[/]", origin);
                }
                else if (sessionTune.Outcome is not null)
                {
                    table.AddRow(BuildOutcomeMarkup(sessionTune.Outcome), titleMarkup, origin);
                }
                else
                {
                    table.AddRow(string.Empty, titleMarkup, origin);
                }
            }

            return table;
        }

        private static string BuildOutcomeMarkup(ReviewOutcome outcome)
        {
            return outcome.Action switch
            {
                ReviewAction.Reviewed => $"[green]{outcome.Score}[/]",
                ReviewAction.ReviewedWithNotes => $"[green]{outcome.Score}[/] [gray]+ notes[/]",
                ReviewAction.Skipped => "[yellow]skipped[/]",
                ReviewAction.Excluded => "[red]excluded[/]",
                ReviewAction.SessionMaintained => "[blue]session[/]",
                _ => string.Empty
            };
        }

        private static Rows BuildTuneDetails(TuneNote tune, string vaultName)
        {
            var table = new Table();
            table.AddColumn("Field");
            table.AddColumn("Value");

            table.AddRow("Type", EscapeField(tune.Type));
            table.AddRow("Key", EscapeField(tune.Key));
            table.AddRow("Whistle", EscapeField(tune.Whistle));
            table.AddRow("Origin", EscapeField(tune.Origin));
            table.AddRow("Composer", EscapeField(tune.Composer));

            return new Rows(
                new Markup($"[bold blue]{BuildTuneLink(tune, vaultName)}[/]\n"),
                new Markup("[gray]Ctrl + Click on the link above to open this tune in Obsidian.[/]\n"),
                table);
        }

        private static Markup BuildInputInstructions()
        {
            return new Markup(
                "[gray]'s' to Skip this tune[/]\n" +
                "[gray]'x' to eXclude this tune[/]\n" +
                "[gray]'m' to mark this tune as Maintained by regular sessions[/]\n" +
                "[gray]'n' to add Notes to this tune's review[/]\n\n" +
                "[gray]or a number from 0 (Very poor) to 9 (Excellent)[/]\n" +
                "[gray]    0 = 1 day     5 = 60 days[/]\n" +
                "[gray]    1 = 3 days    6 = 120 days[/]\n" +
                "[gray]    2 = 7 days    7 = 180 days[/]\n" +
                "[gray]    3 = 14 days   8 = 270 days[/]\n" +
                "[gray]    4 = 30 days   9 = 365 days[/]");
        }

        private static string BuildTuneLink(TuneNote tune, string vaultName)
        {
            var encodedTitle = Uri.EscapeDataString(tune.Title ?? string.Empty);
            var obsidianUrl = $"obsidian://open?vault={Uri.EscapeDataString(vaultName)}&file={encodedTitle}";
            var title = Markup.Escape(tune.Title ?? string.Empty);

            return $"[link={obsidianUrl}]{title}[/]";
        }

        private static string EscapeField(object? value)
        {
            return value is null
                ? string.Empty
                : Markup.Escape(WikiLinkHelper.ExtractDisplayText(value) ?? string.Empty);
        }

        private sealed class ReviewSessionTune
        {
            public ReviewSessionTune(TuneNote tune)
            {
                Tune = tune;
            }

            public TuneNote Tune { get; }
            public ReviewOutcome? Outcome { get; set; }
        }

        private sealed record ReviewOutcome(ReviewAction Action, int? Score = null);

        private enum ReviewAction
        {
            Reviewed,
            ReviewedWithNotes,
            Skipped,
            Excluded,
            SessionMaintained
        }
    }
}
