using Newtonsoft.Json;

namespace Mcba.CustomerWebsite.Dtos;

public class CustomerDetailsDto
{
    [JsonProperty(Required = Required.Always)]
    public string Name { get; set; } = string.Empty;
    [JsonProperty(Required = Required.AllowNull)]
    public string? Address { get; set; }
    [JsonProperty(Required = Required.AllowNull)]
    public string? City { get; set; }
    [JsonProperty(Required = Required.AllowNull)]
    public string? PostCode { get; set; }
}
