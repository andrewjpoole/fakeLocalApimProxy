var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/", async (context) =>
{
    context.Response.StatusCode = StatusCodes.Status202Accepted;
    context.Response.Headers.Add("x-test-header", Guid.NewGuid().ToString());
    await context.Response.WriteAsync($"Hello World! {DateTime.Now:s}");
    await context.Response.CompleteAsync();
});

app.Run();
