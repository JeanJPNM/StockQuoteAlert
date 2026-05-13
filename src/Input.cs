using Spectre.Console;

namespace StockQuoteAlert;

public static class Input
{
    private static T ReadConfigValue<T>(
        string prompt,
        string? hint,
        bool hasDefault,
        Func<string?, T> parse
    )
    {
        string hintSuffix = string.IsNullOrEmpty(hint) ? "" : $" [{hint}]";
        string suffix = hasDefault ? " (unchanged)" : "";
        string? input;

        do
        {
            Console.Write($"{prompt}{hintSuffix}{suffix}: ");
            input = Console.ReadLine();
        } while (!hasDefault && string.IsNullOrWhiteSpace(input));

        if (string.IsNullOrWhiteSpace(input))
            return parse(null);

        return parse(input);
    }

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

        var pollIntervalPrompt = CreatePrompt<TimeSpan>("Poll Interval", "hh:mm:ss");
        pollIntervalPrompt.DefaultValue(existingConfig?.PollInterval ?? new TimeSpan(0, 1, 0));
        var pollInterval = AnsiConsole.Prompt(pollIntervalPrompt);

        var hostNamePrompt = CreatePrompt<string>("SMTP Host Name", "smtp.example.com");
        if (existingConfig != null)
            hostNamePrompt.DefaultValue(existingConfig.ServerSettings.HostName);
        var hostName = AnsiConsole.Prompt(hostNamePrompt);

        var portPrompt = CreatePrompt<int>("SMTP Port");
        if (existingConfig != null)
            portPrompt.DefaultValue(existingConfig.ServerSettings.Port);
        var port = AnsiConsole.Prompt(portPrompt);

        var userPrompt = CreatePrompt<string>("SMTP User");
        if (existingConfig != null)
            userPrompt.DefaultValue(existingConfig.ServerSettings.User);
        var user = AnsiConsole.Prompt(userPrompt);

        var passwordPrompt = new TextPrompt<string>("SMTP Password").Secret();
        if (existingConfig != null)
            passwordPrompt.DefaultValue(existingConfig.ServerSettings.Password);
        var password = AnsiConsole.Prompt(passwordPrompt);

        var apiKeyPrompt = new TextPrompt<string>("Brapi API Key").Secret();
        if (existingConfig != null)
            apiKeyPrompt.DefaultValue(existingConfig.BrapiApiKey);
        var apiKey = AnsiConsole.Prompt(apiKeyPrompt);

        return new Config()
        {
            BrapiApiKey = apiKey,
            NotificationEmail = notificationEmail,
            PollInterval = pollInterval,
            ServerSettings = new SmtpOptions(hostName, port, user, password),
        };
    }
}
