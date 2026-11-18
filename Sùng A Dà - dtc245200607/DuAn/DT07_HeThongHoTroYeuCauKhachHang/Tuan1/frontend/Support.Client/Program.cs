using System.Net.Http.Headers;
using System.Net.Http.Json;

Console.InputEncoding = System.Text.Encoding.UTF8;
Console.OutputEncoding = System.Text.Encoding.UTF8;
var api = Environment.GetEnvironmentVariable("SUPPORT_API_URL") ?? "http://localhost:3000/api";
using var http = new HttpClient { BaseAddress = new Uri(api.TrimEnd('/') + "/") };

Console.Write("Username: ");
var username = Console.ReadLine()?.Trim();
Console.Write("Password: ");
var password = Console.ReadLine();

if (string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(password))
{
    Console.Error.WriteLine("Username and password are required.");
    return;
}

try
{
    var loginResponse = await http.PostAsJsonAsync("auth/login", new { username, password });
    if (!loginResponse.IsSuccessStatusCode)
    {
        Console.Error.WriteLine("Login failed. Check the account and API URL.");
        return;
    }

    var session = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
    if (session is null || string.IsNullOrWhiteSpace(session.Token))
    {
        Console.Error.WriteLine("The API returned an invalid login response.");
        return;
    }

    http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.Token);
    Console.WriteLine($"Signed in as {session.Username} ({session.Role}).");
    Console.Write("1: create ticket  2: list tickets > ");
    var choice = Console.ReadLine();

    HttpResponseMessage response;
    if (choice == "1")
    {
        var requesterId = session.Role == "Requester" ? session.Username : ReadRequired("Requester ID: ");
        var content = ReadRequired("Request content: ");
        Console.Write("Priority (Normal/High): ");
        var priority = Console.ReadLine()?.Trim();
        response = await http.PostAsJsonAsync("tickets", new { requesterId, content, priority });
    }
    else if (choice == "2")
    {
        response = await http.GetAsync("tickets");
    }
    else
    {
        Console.Error.WriteLine("Choose 1 or 2.");
        return;
    }

    var result = await response.Content.ReadAsStringAsync();
    if (!response.IsSuccessStatusCode) Console.Error.WriteLine($"API error ({(int)response.StatusCode}): {result}");
    else Console.WriteLine(result);
}
catch (HttpRequestException ex)
{
    Console.Error.WriteLine($"Could not reach the API at {api}: {ex.Message}");
}

static string ReadRequired(string prompt)
{
    Console.Write(prompt);
    var value = Console.ReadLine()?.Trim();
    while (string.IsNullOrWhiteSpace(value))
    {
        Console.Write("This value is required. Try again: ");
        value = Console.ReadLine()?.Trim();
    }
    return value;
}

record LoginResponse(string Token, string Username, string Role);
