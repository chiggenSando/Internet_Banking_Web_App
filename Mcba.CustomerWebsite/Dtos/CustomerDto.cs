using Newtonsoft.Json;

namespace Mcba.CustomerWebsite.Dtos;

public class CustomerDto
{
    [JsonProperty(Required = Required.Always)]
    public int Id { get; set; }
    [JsonProperty(Required = Required.Always)]
    public CustomerDetailsDto Details { get; set; } = new CustomerDetailsDto();
    [JsonProperty(Required = Required.Always)]
    public CredentialsDto Credentials { get; set; } = new CredentialsDto();
    [JsonProperty(Required = Required.Always)]
    public List<AccountDto> Accounts { get; set; } = [];
}
