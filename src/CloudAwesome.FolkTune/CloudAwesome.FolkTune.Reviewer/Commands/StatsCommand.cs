using System;
using System.IO;
using System.Threading;
using CloudAwesome.FolkTune.Reviewer.Settings;
using CloudAwesome.FolkTune.Services;
using Spectre.Console;
using Spectre.Console.Cli;

namespace CloudAwesome.FolkTune.Reviewer.Commands
{
    public class StatsCommand : Command<BaseSettings>
    {
        public override int Execute(CommandContext context, BaseSettings settings, CancellationToken cancellationToken)
        {
            try
            {
                var vaultPath = settings.VaultPath ?? Directory.GetCurrentDirectory();
                var storePath = settings.StorePath ?? Path.Combine(vaultPath, ".tune-review", "reviews.json");

                var engine = new ReviewEngine(new VaultScanner(), new ReviewStoreManager(), new SelectionService());
                engine.Load(vaultPath, storePath);

                var options = new SelectionService.SelectionOptions
                {
                    Today = DateTime.Today
                };

                var stats = engine.GetStats(options);

                var table = new Table().Centered();
                table.Title("Tune Review Statistics");
                table.AddColumn("Metric");
                table.AddColumn("Value");

                table.AddRow("Total Learned Tunes", stats.TotalLearned.ToString());
                table.AddRow("Total Eligible for Review", stats.TotalEligible.ToString());
                table.AddRow("Excluded Count", stats.ExcludedCount.ToString());
                table.AddRow("Session Maintained Count", stats.SessionMaintainedCount.ToString());
                table.AddRow("Never Reviewed Count", stats.NeverReviewedCount.ToString());
                table.AddRow("Overdue Count", $"[red]{stats.OverdueCount}[/]");

                AnsiConsole.Write(table);

                if (stats.MostOverdue.Count > 0)
                {
                    AnsiConsole.MarkupLine("\n[bold]Top 10 Most Overdue / Neglected:[/]");
                    foreach (var item in stats.MostOverdue)
                    {
                        AnsiConsole.MarkupLine($"- {item}");
                    }
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
