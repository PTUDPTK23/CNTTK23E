using System.Net;

namespace MediQueue.IntegrationTests.Authentication;

public sealed class AccountAuthorizationTests(MediQueueApiFactory factory)
    : IClassFixture<MediQueueApiFactory>
{
    [Theory]
    [InlineData("/api/account/me")]
    [InlineData("/api/account/logout")]
    [InlineData("/api/account/change-password")]
    public async Task Protected_account_endpoints_reject_missing_token(string path)
    {
        using HttpClient client = factory.CreateClient();
        using HttpResponseMessage response = path.EndsWith("/me", StringComparison.Ordinal)
            ? await client.GetAsync(path)
            : await client.PostAsync(path, content: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
