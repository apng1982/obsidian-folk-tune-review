using System.Threading;
using CloudAwesome.FolkTune.Reviewer.Settings;
using CloudAwesome.FolkTune.Services;
using Spectre.Console;
using Spectre.Console.Cli;

namespace CloudAwesome.FolkTune.Reviewer.Commands
{
    public class IdsInitCommand : Command<IdsInitSettings>
    {
        public override int Execute(CommandContext context, IdsInitSettings settings, CancellationToken cancellationToken)
        {
            try
            {
                var vaultPath = settings.VaultPath ?? Directory.GetCurrentDirectory();
                var engine = new ReviewEngine(new VaultScanner(), new ReviewStoreManager(), new SelectionService(), new IdInitializer());

                var initOptions = new IdInitializer.InitOptions
                {
                    VaultPath = vaultPath,
                    SubFolder = settings.Root ?? string.Empty,
                    DryRun = settings.DryRun,
                    Limit = settings.Limit,
                    IncludeExisting = settings.IncludeExisting
                };

                AnsiConsole.MarkupLine($"[blue]Scanning vault at {vaultPath}...[/]");
                if (!string.IsNullOrEmpty(settings.Root))
                {
                    AnsiConsole.MarkupLine($"[blue]Restricted to subfolder: {settings.Root}[/]");
                }

                var result = engine.InitializeIds(initOptions);

                if (result.Warnings.Any())
                {
                    AnsiConsole.MarkupLine("[yellow]Warnings:[/]");
                    foreach (var warning in result.Warnings)
                    {
                        AnsiConsole.MarkupLine($"  [yellow]- {warning}[/]");
                    }
                }

                if (result.Duplicates.Any())
                {
                    AnsiConsole.MarkupLine("[red]Errors (Duplicates detected):[/]");
                    foreach (var dup in result.Duplicates)
                    {
                        AnsiConsole.MarkupLine($"  [red]- {dup}[/]");
                    }
                }

                if (!result.Success)
                {
                    AnsiConsole.MarkupLine("[red]Initialization failed due to duplicate IDs. Please resolve duplicates and try again.[/]");
                    return 1;
                }

                if (settings.DryRun)
                {
                    AnsiConsole.MarkupLine($"[green]Dry run complete. {result.UpdatedFiles.Count} files would be updated:[/]");
                }
                else
                {
                    AnsiConsole.MarkupLine($"[green]Initialization complete. {result.UpdatedFiles.Count} files updated:[/]");
                }

                foreach (var file in result.UpdatedFiles)
                {
                    AnsiConsole.MarkupLine($"  [grey]- {Path.GetFileName(file)}[/]");
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
