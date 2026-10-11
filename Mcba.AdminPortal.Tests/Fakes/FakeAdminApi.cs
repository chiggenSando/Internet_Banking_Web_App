using System.Net;
using System.Net.Http.Json;

namespace Mcba.AdminPortal.Tests.Fakes;

// Stands in for the Admin Web API: returns a canned response and records every request the portal sends.
public class FakeAdminApi(HttpStatusCode status = HttpStatusCode.OK, object? body = null) : HttpMessageHandler, IHttpClientFactory
{
    public record SentRequest(HttpMethod Method, string PathAndQuery, string? Body);

    public List<SentRequest> Requests { get; } = new();

    public SentRequest Request => Assert.Single(Requests);

    // IHttpClientFactory: the portal controllers ask for the "AdminApi" client.
    public HttpClient CreateClient(string name)
    {
        Assert.Equal("AdminApi", name);
        return new HttpClient(this, disposeHandler: false) { BaseAddress = new Uri("http://localhost:5040") };
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // Read the body now, while the request content is still available.
        var requestBody = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
        Requests.Add(new SentRequest(request.Method, request.RequestUri!.PathAndQuery, requestBody));

        var response = new HttpResponseMessage(status);
        if (body is not null)
            response.Content = JsonContent.Create(body);
        return response;
    }
}
