using System;
using System.Globalization;
using Spectre.Console.Cli;
using StockQuoteAlert;

var app = new CommandApp();
app.Configure(config =>
{
    config.AddCommand<WatchCommand>("watch");
    config.AddCommand<ConfigureCommand>("configure");
});

return app.Run(args);

internal class WatchCommand : Command<WatchCommand.Settings>
{
    public class Settings : CommandSettings
    {
        [CommandArgument(0, "<stock>")]
        public string Stock { get; set; } = string.Empty;

        [CommandArgument(1, "<sellPrice>")]
        public decimal SellPrice { get; set; }

        [CommandArgument(2, "<buyPrice>")]
        public decimal BuyPrice { get; set; }
    }

    protected override int Execute(
        CommandContext context,
        Settings settings,
        CancellationToken cancellation
    )
    {
        Config? config = Config.Load();
        if (config == null)
        {
            Console.Error.WriteLine(
                "No configuration found. Please run 'configure' command first."
            );
            return 1;
        }

        using Watcher watcher = new();
        watcher
            .Run(config, settings.Stock, settings.BuyPrice, settings.SellPrice, cancellation)
            .Wait();
        return 0;
    }
}

internal class ConfigureCommand : Command<ConfigureCommand.Settings>
{
    public class Settings : CommandSettings { }

    protected override int Execute(
        CommandContext context,
        Settings settings,
        CancellationToken cancellation
    )
    {
        Config config = Input.PromptConfigValues(Config.Load());
        Config.Write(config);
        return 0;
    }
}
