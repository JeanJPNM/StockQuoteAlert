using System.Net.Http;
using System.Text.Json;
using MailKit.Net.Smtp;
using MimeKit;

namespace StockQuoteAlert;

public enum WatcherState
{
    Neutral,
    ShouldSell,
    ShouldBuy,
}

public class Watcher : IDisposable
{
    private readonly HttpClient httpClient = new();
    private readonly SmtpClient smtpClient = new();
    private bool hasSentError = false;
    public WatcherState State { get; set; } = WatcherState.Neutral;

    public async Task Run(
        Config config,
        string stock,
        decimal buyPrice,
        decimal sellPrice,
        CancellationToken cancellation
    )
    {
        httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {config.BrapiApiKey}");
        await smtpClient.ConnectAsync(
            config.ServerSettings.HostName,
            config.ServerSettings.Port,
            useSsl: false,
            cancellation
        );
        await smtpClient.AuthenticateAsync(
            config.ServerSettings.User,
            config.ServerSettings.Password,
            cancellation
        );

        var deserializationOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        try
        {
            while (!cancellation.IsCancellationRequested)
            {
                var response = await RequestStockData(stock, deserializationOptions, cancellation);

                if (response is BrapiErrorResponse errorResponse)
                {
                    if (hasSentError)
                        continue;
                    hasSentError = true;
                    await SendErrorEmail(config.NotificationEmail);
                    Console.Error.WriteLine(
                        "Error response from Brapi API: " + JsonSerializer.Serialize(errorResponse)
                    );
                }
                else if (response is BrapiQuoteResponse quoteResponse)
                {
                    var price = quoteResponse.Results.First().RegularMarketPrice;
                    var nextState = price switch
                    {
                        _ when price <= buyPrice => WatcherState.ShouldBuy,
                        _ when price >= sellPrice => WatcherState.ShouldSell,
                        _ => WatcherState.Neutral,
                    };

                    if (nextState == State)
                        continue;

                    State = nextState;
                    if (State == WatcherState.Neutral)
                        continue;

                    await SendRecommendationEmail(
                        config.NotificationEmail,
                        stock,
                        price,
                        buyPrice,
                        sellPrice
                    );
                }

                await Task.Delay(config.PollInterval, cancellation);
            }
        }
        // no need to bubble up cancellation errors
        catch (OperationCanceledException) { }
        catch
        {
            if (hasSentError)
                return;

            await SendErrorEmail(config.NotificationEmail);

            throw;
        }
        finally
        {
            smtpClient.Disconnect(quit: true);
        }
    }

    private Task<string> SendErrorEmail(string email)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("Stock Quote Alert", "noreply@stockquote.dev"));
        message.To.Add(new MailboxAddress(null, email));
        message.Subject = "Error running StockQuoteAlert";
        message.Body = new TextPart("plain")
        {
            Text =
                "There was an error running StockQuoteAlert, please check the terminal logs and restart the application",
        };

        return smtpClient.SendAsync(message);
    }

    private Task<string> SendRecommendationEmail(
        string email,
        string stock,
        decimal price,
        decimal buyPrice,
        decimal sellPrice
    )
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("Stock Quote Alert", "noreply@stockquote.dev"));
        message.To.Add(new MailboxAddress(null, email));

        bool shouldSell = State == WatcherState.ShouldSell;
        switch (State)
        {
            case WatcherState.Neutral:
                message.Subject = $"Rcommendation for {stock} - Hold";
                message.Body = new TextPart("plain")
                {
                    Text =
                        $"The current price of {stock} is {price}, which is between your buy and sell thresholds",
                };
                break;
            case WatcherState.ShouldBuy:
                message.Subject = $"Recommendation for {stock} - Buy";
                message.Body = new TextPart("plain")
                {
                    Text =
                        $"The current price of {stock} is {price}, which is bellow your buy threshold of {buyPrice}",
                };
                break;
            case WatcherState.ShouldSell:
                message.Subject = $"Recommendation for {stock} - Sell";
                message.Body = new TextPart("plain")
                {
                    Text =
                        $"The current price of {stock} is {price}, which is above your sell threshold of {sellPrice}",
                };

                break;
        }

        return smtpClient.SendAsync(message);
    }

    private async Task<BrapiResponse?> RequestStockData(
        string stock,
        JsonSerializerOptions serializerOptions,
        CancellationToken cancellation
    )
    {
        var response = await httpClient.GetAsync(
            $"https://brapi.dev/api/quote/{stock}",
            cancellation
        );
        var responseBody = await response.Content.ReadAsStringAsync(cancellation);

        return JsonSerializer.Deserialize<BrapiResponse>(responseBody, serializerOptions);
    }

    public void Dispose()
    {
        httpClient.Dispose();
        smtpClient.Dispose();
    }
}
