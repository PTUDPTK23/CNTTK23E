var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var assignments = new[]
{
    new Assignment(1,"Môn Toán", "Bài Toán Hôm Nay"),
    new Assignment(2,"Môn Hóa", "Bài Hóa Hôm Nay")
};


app.MapGet("/", () => Results.Ok(new
{
    service = "Submission.Api",
    status = "Running"
}));


app.Run();
record Assignment(int id, string tilte, string description);