using System;
using System.IO;
using System.Text.Json;

namespace StockQuoteAlert;

public class Config
{
    public required string NotificationEmail { get; set; }
    public required string BrapiApiKey { get; set; }
    public required TimeSpan PollInterval { get; set; }
    public required SmtpOptions ServerSettings { get; set; }

    private static string GetConfigPath()
    {
        return Path.Join(Directory.GetCurrentDirectory(), "config.json");
    }

    public static Config? Load()
    {
        string path = GetConfigPath();
        if (!File.Exists(path))
            return null;

        string json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<Config>(json);
    }

    public static void Write(Config config)
    {
        string json = JsonSerializer.Serialize(
            config,
            new JsonSerializerOptions { WriteIndented = true }
        );

        File.WriteAllText(GetConfigPath(), json);
    }
}

public record SmtpOptions(string HostName, int Port, string User, string Password);
