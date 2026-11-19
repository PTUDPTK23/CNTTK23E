using System.Net;
using MediQueue.Web.Clients;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace MediQueue.Web.Authentication;

public sealed class MediQueueCookieEvents(
    IAuthenticationSessionStore sessionStore,
    IAccountApiClient accountApiClient) : CookieAuthenticationEvents
{
    public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
    {
        string? sessionId = context.Principal?.GetSessionId();
        if (context.Principal is null ||
            sessionId is null ||
            !context.Principal.TryGetUserId(out Guid userId))
        {
            await RejectAsync(context, sessionId);
            return;
        }

        AuthenticationSession? session = await sessionStore.GetAsync(
            sessionId,
            userId,
            context.HttpContext.RequestAborted);
        if (session is null)
        {
            await RejectAsync(context, sessionId);
            return;
        }

        ApiClientResult<MediQueue.Contracts.Authentication.AccountProfileResponse> result =
            await accountApiClient.GetProfileAsync(
                session.AccessToken,
                context.HttpContext.RequestAborted);
        if (result.Problem?.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            await RejectAsync(context, sessionId);
        }
    }

    private async Task RejectAsync(CookieValidatePrincipalContext context, string? sessionId)
    {
        if (sessionId is not null)
        {
            await sessionStore.RemoveAsync(sessionId, context.HttpContext.RequestAborted);
        }

        context.RejectPrincipal();
        await context.HttpContext.SignOutAsync(MediQueueAuthenticationDefaults.Scheme);
    }
}
