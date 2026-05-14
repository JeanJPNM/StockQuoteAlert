using Spectre.Console;

namespace StockQuoteAlert;

public static class Input
{
    private static TextPrompt<T> CreatePrompt<T>(string prompt, string? format = null)
    {
        if (string.IsNullOrEmpty(format))
            return new TextPrompt<T>(prompt);
        return new TextPrompt<T>($"{prompt} [gray]{Markup.Escape("[" + format + "]")}[/]");
    }

    public static Config PromptConfigValues(Config? existingConfig)
    {
        Console.WriteLine("Enter configuration values:");
        var emailPromt = CreatePrompt<string>("Notification Email");
        if (existingConfig != null)
            emailPromt.DefaultValue(existingConfig.NotificationEmail);
        var notificationEmail = AnsiConsole.Prompt(emailPromt);

        AnsiConsole.MarkupLine(
            "[yellow]Note:[/] The default poll interval is set to 3 minutes to avoid hitting the Brapi API rate limit on the free tier."
        );
        var pollIntervalPrompt = CreatePrompt<TimeSpan>("Poll Interval", "hh:mm:ss");
        // the Brapi API has a rate limit of 15000 requests per month on the free tier
        // which amounts to around one request every 179 seconds for a 31 day month
        // so we set the default poll interval to 3 minutes to avoid hitting the rate limit
        pollIntervalPrompt.DefaultValue(existingConfig?.PollInterval ?? new TimeSpan(0, 3, 0));
        var pollInterval = AnsiConsole.Prompt(pollIntervalPrompt);

        var hostNamePrompt = CreatePrompt<string>("SMTP Host Name", "smtp.example.com");
        if (existingConfig != null)
            hostNamePrompt.DefaultValue(existingConfig.ServerSettings.HostName);
        var hostName = AnsiConsole.Prompt(hostNamePrompt);

        var portPrompt = CreatePrompt<int>("SMTP Port");
        if (existingConfig != null)
            portPrompt.DefaultValue(existingConfig.ServerSettings.Port);
        var port = AnsiConsole.Prompt(portPrompt);

        var useSsl = AnsiConsole.Confirm("Use SSL for SMTP connection?");

        var user = "";
        var password = "";
        if (AnsiConsole.Confirm("Use SMTP authentication?"))
        {
            var userPrompt = CreatePrompt<string>("SMTP User").AllowEmpty();
            if (existingConfig != null)
                userPrompt.DefaultValue(existingConfig.ServerSettings.User);
            user = AnsiConsole.Prompt(userPrompt);

            var passwordPrompt = new TextPrompt<string>("SMTP Password").Secret().AllowEmpty();
            if (existingConfig != null)
                passwordPrompt.DefaultValue(existingConfig.ServerSettings.Password);
            password = AnsiConsole.Prompt(passwordPrompt);
        }

        // IA usada para gerar mensagem de aviso sobre a chave de API do Brapi
        if (existingConfig == null)
            AnsiConsole.MarkupLine(
                "[yellow]Note:[/] The Brapi API key is required to fetch stock data. You can obtain a free API key by registering at [blue link=https://brapi.dev/]Brapi's website[/]."
            );
        var apiKeyPrompt = new TextPrompt<string>("Brapi API Key").Secret();
        if (existingConfig != null)
            apiKeyPrompt.DefaultValue(existingConfig.BrapiApiKey);
        var apiKey = AnsiConsole.Prompt(apiKeyPrompt);

        return new Config()
        {
            BrapiApiKey = apiKey,
            NotificationEmail = notificationEmail,
            PollInterval = pollInterval,
            ServerSettings = new SmtpOptions(hostName, port, useSsl, user, password),
        };
    }
}
