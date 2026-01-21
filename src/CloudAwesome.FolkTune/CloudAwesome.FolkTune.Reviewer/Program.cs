using CloudAwesome.FolkTune.Reviewer.Commands;
using Spectre.Console.Cli;

var app = new CommandApp();

app.Configure(config =>
{
    config.SetApplicationName("tune-review");
    
    config.AddCommand<ReviewCommand>("review")
        .WithDescription("Start an interactive review session");
        
    config.AddBranch("admin", ids => {
        ids.SetDescription("Automate administrative tasks with your vault required to use and maintain this CLI");
        
        ids.AddCommand<IdsInitCommand>("id-init")
            .WithDescription("Initialize missing tune IDs in markdown files");
    });
        
    config.AddCommand<PickCommand>("pick")
        .WithDescription("Show which tunes would be selected for review");
        
    config.AddCommand<StatsCommand>("stats")
        .WithDescription("Show repertoire health statistics");
        
    config.AddCommand<SessionCommand>("session")
        .WithDescription("Mark tunes as played at a session");
});

return app.Run(args);