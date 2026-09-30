using Newtonsoft.Json;

namespace Mcba.CustomerWebsite.Dtos;

public class AccountDetailsDto
{
    [JsonProperty(Required = Required.Always)]
    public char Type { get; set; }
}
