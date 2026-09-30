using Newtonsoft.Json;

namespace Mcba.CustomerWebsite.Dtos;

public class CredentialsDto
{
    [JsonProperty(Required = Required.Always)]
    public string Id { get; set; } = string.Empty;
    [JsonProperty("Details", Required = Required.Always)]
    public CredentialsDetailsDto CredentialsDetails { get; set; } = new CredentialsDetailsDto();
}
