using Newtonsoft.Json;

namespace Mcba.CustomerWebsite.Dtos;

public class CredentialsDetailsDto
{
    [JsonProperty(Required = Required.Always)]
    public string PasswordHash { get; set; } = string.Empty;
}
