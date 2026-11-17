using MediQueue.Contracts.Health;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();
app.MapGet("/health", () =>
    Results.Ok(new ServiceStatusResponse("MediQueue.Web", "Healthy")));
app.MapRazorPages()
   .WithStaticAssets();

app.Run();
