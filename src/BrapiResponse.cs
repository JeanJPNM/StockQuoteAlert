using System.Text.Json;
using System.Text.Json.Serialization;

namespace StockQuoteAlert;

[JsonConverter(typeof(BrapiResponseConverter))]
public abstract class BrapiResponse { }

public class BrapiQuoteData
{
    // other fields exist, but we are only interested on the price
    public required decimal RegularMarketPrice { get; set; }
}

public class BrapiQuoteResponse : BrapiResponse
{
    public required string Symbol { get; set; }
    public required decimal Price { get; set; }

    public required List<BrapiQuoteData> Results { get; set; }
}

public class BrapiErrorResponse : BrapiResponse
{
    public required string Message { get; set; }
    public required string Code { get; set; }
}

// conversor de json gerado com auxílio de IA
public class BrapiResponseConverter : JsonConverter<BrapiResponse>
{
    public override BrapiResponse? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        if (root.TryGetProperty("error", out _))
        {
            return doc.Deserialize<BrapiErrorResponse>(options);
        }
        return doc.Deserialize<BrapiQuoteResponse>(options);
    }

    public override void Write(
        Utf8JsonWriter writer,
        BrapiResponse value,
        JsonSerializerOptions options
    )
    {
        JsonSerializer.Serialize(writer, (object)value, options);
    }
}
