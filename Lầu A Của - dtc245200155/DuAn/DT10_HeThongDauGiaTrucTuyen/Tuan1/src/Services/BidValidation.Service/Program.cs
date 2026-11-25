using BidValidation.Service.Services;
using Microsoft.AspNetCore.Server.Kestrel.Core;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel to support HTTP/1 and HTTP/2
builder.WebHost.ConfigureKestrel(options =>
{
    var port = builder.Configuration.GetValue<int>("PORT", 5002);
    options.Listen(System.Net.IPAddress.Any, port, listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http2;
    });
});

builder.Services.AddGrpc();
builder.Services.AddHealthChecks();

var app = builder.Build();

app.MapGrpcService<BidValidationServiceImpl>();
app.MapHealthChecks("/health");

app.MapGet("/", () => "Bid Validation gRPC Service is running. Use a gRPC client to communicate.");

app.Run();
