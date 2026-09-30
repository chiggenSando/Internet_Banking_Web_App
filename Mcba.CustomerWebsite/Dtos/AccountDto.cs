using Newtonsoft.Json;

namespace Mcba.CustomerWebsite.Dtos;

public class AccountDto
{
    [JsonProperty(Required = Required.Always)]
    public int Id { get; set; }
    [JsonProperty(Required = Required.Always)]
    public AccountDetailsDto Details { get; set; } = new AccountDetailsDto();
    [JsonProperty("OpeningDepositTransactions", Required = Required.Always)]
    public List<TransactionDto> Transactions { get; set; } = [];
}
