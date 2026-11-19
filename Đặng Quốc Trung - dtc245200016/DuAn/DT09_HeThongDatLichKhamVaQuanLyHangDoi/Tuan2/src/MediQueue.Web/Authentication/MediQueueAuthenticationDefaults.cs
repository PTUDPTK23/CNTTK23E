namespace MediQueue.Web.Authentication;

public static class MediQueueAuthenticationDefaults
{
    public const string Scheme = "MediQueueCookie";
    public const string CookieName = ".MediQueue.Auth";
    public const string SessionIdClaim = "mediqueue_session_id";
    public const string DisplayNameClaim = "mediqueue_display_name";
}
