using var client = new HttpClient();
var response = await client.GetStringAsync("http://localhost:5100/");
Console.WriteLine($"Server trả về: {response}");
