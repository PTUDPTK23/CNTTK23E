using MediQueue.Infrastructure.Identity;

namespace MediQueue.Appointment.Api.Bootstrap;

public static class BootstrapCommandRunner
{
    private const string RolesCommand = "--bootstrap-roles";
    private const string AdminCommand = "--bootstrap-admin";

    public static bool IsBootstrapCommand(string[] args) =>
        args.Contains(RolesCommand, StringComparer.Ordinal) ||
        args.Contains(AdminCommand, StringComparer.Ordinal);

    public static async Task<int> RunAsync(IServiceProvider services, string[] args)
    {
        using IServiceScope scope = services.CreateScope();
        IIdentityBootstrapper bootstrapper =
            scope.ServiceProvider.GetRequiredService<IIdentityBootstrapper>();

        IdentityBootstrapResult roleResult = await bootstrapper.InitializeRolesAsync();
        Console.WriteLine(roleResult.Message);
        if (!roleResult.Succeeded || !args.Contains(AdminCommand, StringComparer.Ordinal))
        {
            return roleResult.Succeeded ? 0 : 1;
        }

        string? email = GetOption(args, "--email");
        if (string.IsNullOrWhiteSpace(email) || Console.IsInputRedirected)
        {
            Console.Error.WriteLine(
                "Bootstrap Admin yêu cầu --email và terminal tương tác để nhập mật khẩu ẩn.");
            return 1;
        }

        Console.Write("Mật khẩu Admin: ");
        string password = ReadMaskedPassword();
        Console.WriteLine();
        Console.Write("Nhập lại mật khẩu: ");
        string confirmation = ReadMaskedPassword();
        Console.WriteLine();

        if (!string.Equals(password, confirmation, StringComparison.Ordinal))
        {
            Console.Error.WriteLine("Mật khẩu xác nhận không khớp.");
            return 1;
        }

        IdentityBootstrapResult adminResult =
            await bootstrapper.CreateInitialAdminAsync(email.Trim(), password);
        Console.WriteLine(adminResult.Message);
        return adminResult.Succeeded ? 0 : 1;
    }

    private static string? GetOption(string[] args, string name)
    {
        int index = Array.FindIndex(args, item => string.Equals(item, name, StringComparison.Ordinal));
        return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
    }

    private static string ReadMaskedPassword()
    {
        var password = new System.Text.StringBuilder();
        while (true)
        {
            ConsoleKeyInfo key = Console.ReadKey(intercept: true);
            if (key.Key == ConsoleKey.Enter)
            {
                return password.ToString();
            }

            if (key.Key == ConsoleKey.Backspace && password.Length > 0)
            {
                password.Length--;
            }
            else if (!char.IsControl(key.KeyChar))
            {
                password.Append(key.KeyChar);
            }
        }
    }
}
