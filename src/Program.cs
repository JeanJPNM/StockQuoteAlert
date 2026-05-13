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
        Console.WriteLine($"Stock: {settings.Stock}");
        Console.WriteLine($"Sell price: {settings.SellPrice:F}");
        Console.WriteLine($"Buy price: {settings.BuyPrice:F}");
        Console.Write(System.Text.Json.JsonSerializer.Serialize(Config.Load()));
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
        // Console.WriteLine($"Email de notificação: {config.NotificationEmail}");
        // Console.WriteLine($"Intervalo de consulta: {config.PollInterval}");
        // Console.WriteLine($"SMTP Host: {config.ServerSettings.HostName}");
        // Console.WriteLine($"SMTP Port: {config.ServerSettings.Port}");
        // Console.WriteLine($"SMTP User: {config.ServerSettings.User}");
        return 0;
    }
}
