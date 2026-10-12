using System.Net;
using System.Text;

namespace Netka.Identity.SDK.Tests;

/// <summary>
/// Simple delegating HTTP message handler for unit testing provider HTTP calls without real network traffic.
/// </summary>
public sealed class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handler;

    public List<HttpRequestMessage> SentRequests { get; } = new();
    public List<string> SentBodies { get; } = new();

    public MockHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
    {
        _handler = (req, _) => Task.FromResult(handler(req));
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        SentRequests.Add(request);
        if (request.Content is not null)
        {
            SentBodies.Add(await request.Content.ReadAsStringAsync(cancellationToken));
        }
        else
        {
            SentBodies.Add(string.Empty);
        }

        return await _handler(request, cancellationToken);
    }

    public static HttpResponseMessage JsonResponse(string json, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        return new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }
}
