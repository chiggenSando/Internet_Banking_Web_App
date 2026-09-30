using Newtonsoft.Json;

namespace Mcba.CustomerWebsite.Dtos;

public class TransactionDto
{
    [JsonProperty(Required = Required.Always)]
    public decimal Amount { get; set; }
    [JsonProperty(Required = Required.AllowNull)]
    public string? Comment { get; set; }
    [JsonProperty(Required = Required.Always)]
    public string TimeUtc { get; set; } = string.Empty; // Keeps the string format to be converted later
}
