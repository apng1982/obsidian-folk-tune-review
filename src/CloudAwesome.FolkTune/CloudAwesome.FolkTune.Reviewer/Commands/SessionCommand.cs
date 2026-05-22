using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using CloudAwesome.FolkTune.Models;
using CloudAwesome.FolkTune.Reviewer.Helpers;
using CloudAwesome.FolkTune.Reviewer.Settings;
using CloudAwesome.FolkTune.Services;
using Spectre.Console;
using Spectre.Console.Cli;

namespace CloudAwesome.FolkTune.Reviewer.Commands
{
    public class SessionCommand : Command<SessionSettings>
    {
        public sealed record SessionFileEntry(string Query, int? Score, bool MarkSessionMaintained);

        public override int Execute(CommandContext context, SessionSettings settings, CancellationToken cancellationToken)
        {
            try
            {
                var vaultPath = settings.VaultPath ?? Directory.GetCurrentDirectory();
                var storePath = settings.StorePath ?? VaultStructure.GetDefaultReviewStorePath(vaultPath);

                var engine = new ReviewEngine(new VaultScanner(), new ReviewStoreManager(), new SelectionService(), new IdInitializer());
                var loadResult = engine.Load(vaultPath, storePath);

                CommandOutputHelper.WriteReviewStoreCreatedMessage(loadResult);
                
                var date = DateTime.Today;
                if (!string.IsNullOrEmpty(settings.Date))
                {
                    if (!DateTime.TryParse(settings.Date, out date))
                    {
                        AnsiConsole.MarkupLine($"[red]Invalid date format: {settings.Date}[/]");
                        return 1;
                    }
                }

                var tunesToMark = new List<SessionTuneUpdate>();
                
                if (!string.IsNullOrEmpty(settings.FromFile))
                {
                    if (!File.Exists(settings.FromFile))
                    {
                        AnsiConsole.MarkupLine($"[red]File not found: {settings.FromFile}[/]");
                        return 1;
                    }

                    var lines = File.ReadAllLines(settings.FromFile);
                    for (var i = 0; i < lines.Length; i++)
                    {
                        var line = lines[i];
                        if (!TryParseSessionFileLine(line, out var entry, out var error))
                        {
                            if (string.IsNullOrEmpty(error)) continue;

                            AnsiConsole.MarkupLine($"[red]Invalid session file line {i + 1}: {error}[/]");
                            return 1;
                        }

                        if (entry == null) continue;

                        var found = engine.FindTunes(entry.Query);
                        if (found.Any())
                        {
                            tunesToMark.AddRange(found.Select(t => new SessionTuneUpdate(t, entry)));
                        }
                        else
                        {
                            AnsiConsole.MarkupLine($"[yellow]Warning: Could not find tune by ID or Title: {entry.Query}[/]");
                        }
                    }
                }

                if (tunesToMark.Count == 0)
                {
                    AnsiConsole.MarkupLine("[yellow]No tunes selected to mark as played.[/]");
                    return 0;
                }

                tunesToMark = tunesToMark
                    .Where(t => !string.IsNullOrEmpty(t.Tune.Id))
                    .DistinctBy(t => t.Tune.Id)
                    .ToList();

                AnsiConsole.MarkupLine($"Marking [green]{tunesToMark.Count}[/] tunes as played on [blue]{date:yyyy-MM-dd}[/].");
                
                foreach (var update in tunesToMark)
                {
                    var tune = update.Tune;
                    var entry = update.Entry;

                    if (entry.MarkSessionMaintained)
                    {
                        engine.MarkAsPlayed(tune.Id, date);
                        engine.MarkAsSessionMaintained(tune.Id);
                        AnsiConsole.MarkupLine($"- {tune.Title} [yellow](session-maintained)[/]");
                    }
                    else if (entry.Score.HasValue)
                    {
                        engine.MarkAsPlayed(tune.Id, date, entry.Score.Value);
                        AnsiConsole.MarkupLine($"- {tune.Title} [blue](score {entry.Score.Value})[/]");
                    }
                    else
                    {
                        engine.MarkAsPlayed(tune.Id, date);
                        AnsiConsole.MarkupLine($"- {tune.Title}");
                    }
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

        public static bool TryParseSessionFileLine(string line, out SessionFileEntry? entry, out string? error)
        {
            entry = null;
            error = null;

            if (string.IsNullOrWhiteSpace(line))
            {
                return false;
            }

            var trimmed = line.Trim();
            var separatorIndex = trimmed.LastIndexOf(',');
            if (separatorIndex < 0)
            {
                entry = new SessionFileEntry(trimmed, null, false);
                return true;
            }

            var query = trimmed[..separatorIndex].Trim();
            var action = trimmed[(separatorIndex + 1)..].Trim();

            if (string.IsNullOrWhiteSpace(query))
            {
                error = "missing tune title or ID";
                return false;
            }

            if (action.Equals("m", StringComparison.OrdinalIgnoreCase))
            {
                entry = new SessionFileEntry(query, null, true);
                return true;
            }

            if (action.Length == 1 && action[0] >= '0' && action[0] <= '9')
            {
                entry = new SessionFileEntry(query, action[0] - '0', false);
                return true;
            }

            error = $"expected rating 0-9 or m after comma, but found '{action}'";
            return false;
        }

        private sealed record SessionTuneUpdate(TuneNote Tune, SessionFileEntry Entry);
    }
}
