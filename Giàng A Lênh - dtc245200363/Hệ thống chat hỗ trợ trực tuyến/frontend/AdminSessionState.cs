namespace ChatSupport.Frontend;

public sealed class AdminSessionState
{
    public string? AccessToken { get; private set; }
    public string? DisplayName { get; private set; }
    public string? Role { get; private set; }
    public bool IsAdmin => Role == "Admin" && !string.IsNullOrWhiteSpace(AccessToken);

    public void SignIn(string token, string displayName, string role)
    {
        AccessToken = token;
        DisplayName = displayName;
        Role = role;
    }

    public void SignOut()
    {
        AccessToken = null;
        DisplayName = null;
        Role = null;
    }
}
