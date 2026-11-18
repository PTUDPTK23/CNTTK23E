using System.Globalization;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Clinic.Core;

public static class StaffAuth
{
    public static IServiceCollection AddStaffAuth(this IServiceCollection services,IConfiguration config)
    {
        if((config["Auth:SigningKey"]?.Length ?? 0)<32 || (config["Auth:StaffPassword"]?.Length ?? 0)<12)
            throw new InvalidOperationException("Auth:SigningKey must have >=32 characters and Auth:StaffPassword >=12.");
        services.AddAuthentication("Staff").AddScheme<AuthenticationSchemeOptions,StaffHandler>("Staff",_=>{});
        services.AddAuthorization(o=>o.AddPolicy("Staff",p=>p.RequireAuthenticatedUser().RequireRole("Staff")));
        return services;
    }
    private static string Sign(string value,string key)=>Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(key),Encoding.UTF8.GetBytes(value)));
    public static string Issue(IConfiguration config)
    {
        var payload="Staff."+DateTimeOffset.UtcNow.AddHours(4).ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture)+"."+Secrets.NewToken();
        return payload+"."+Sign(payload,config["Auth:SigningKey"]!);
    }
    public static bool Valid(string token,string key)
    {
        var parts=token.Split('.');
        return parts.Length==4 && parts[0]=="Staff" && long.TryParse(parts[1],out var exp) && exp>DateTimeOffset.UtcNow.ToUnixTimeSeconds() && Secrets.Equal(parts[3],Sign(string.Join('.',parts.Take(3)),key));
    }
}
public sealed class StaffHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,ILoggerFactory logger,UrlEncoder encoder,IConfiguration config) : AuthenticationHandler<AuthenticationSchemeOptions>(options,logger,encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var header=Request.Headers.Authorization.ToString();
        if(!header.StartsWith("Bearer ",StringComparison.OrdinalIgnoreCase)) return Task.FromResult(AuthenticateResult.NoResult());
        if(!StaffAuth.Valid(header[7..],config["Auth:SigningKey"]!)) return Task.FromResult(AuthenticateResult.Fail("Invalid staff token"));
        var identity=new ClaimsIdentity([new Claim(ClaimTypes.Name,"Nhân viên"),new Claim(ClaimTypes.Role,"Staff")],Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity),Scheme.Name)));
    }
}
