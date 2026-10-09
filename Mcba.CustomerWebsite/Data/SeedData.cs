using System.Globalization;
using Mcba.CustomerWebsite.Dtos;
using Mcba.Data;
using Mcba.Data.Models;
using Newtonsoft.Json;

namespace Mcba.CustomerWebsite.Data;

public static class SeedData
{
    private const string TimeUtcFormat = "dd/MM/yyyy hh:mm:ss tt";

    public static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        var context = serviceProvider.GetRequiredService<McbaContext>();
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();

    // Only contact the web-service if there is no customer data.
    if (context.Customers.Any())
        return;

    using var client = new HttpClient();
    var customerDtos = await GetAsync<List<CustomerDto>>(client, configuration["WebService:CustomerUrl"]);

        foreach (var customerDto in customerDtos)
        {
            context.Customers.Add(new Customer
            {
                CustomerID = customerDto.Id,
                Name = customerDto.Details.Name,
                Address = customerDto.Details.Address,
                City = customerDto.Details.City,
                Postcode = customerDto.Details.PostCode
            });

            context.Logins.Add(new Login
            {
                LoginID = customerDto.Credentials.Id,
                CustomerID = customerDto.Id,
                PasswordHash = customerDto.Credentials.CredentialsDetails.PasswordHash
            });

            foreach (var accountDto in customerDto.Accounts)
            {
                context.Accounts.Add(new Account
                {
                    AccountNumber = accountDto.Id,
                    AccountType = accountDto.Details.Type,
                    CustomerID = customerDto.Id,
                    // Balance is the sum of the opening deposit transactions.
                    Balance = accountDto.Transactions.Sum(x => x.Amount)
                });

                foreach (var transactionDto in accountDto.Transactions)
                {
                    context.Transactions.Add(new Transaction
                    {
                        TransactionType = TransactionTypes.Deposit,
                        AccountNumber = accountDto.Id,
                        Amount = transactionDto.Amount,
                        Comment = transactionDto.Comment,
                        TransactionTimeUtc = DateTime.ParseExact(transactionDto.TimeUtc, TimeUtcFormat, CultureInfo.InvariantCulture)
                    });
                }
            }
        }

        await context.SaveChangesAsync();
    }

    // Generic JSON deserialisation.
    private static async Task<T> GetAsync<T>(HttpClient client, string url)
    {
        var json = await client.GetStringAsync(url);
        return JsonConvert.DeserializeObject<T>(json);
    }
}