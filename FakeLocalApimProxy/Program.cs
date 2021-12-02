using FakeLocalApimProxy;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();

builder.Services.AddHttpClient();
builder.Services.AddSingleton<Proxy>();

var app = builder.Build();

app.UseMiddleware<Proxy>();

app.MapGet("/", () => "Hello World! from the fakeLocalApimProxy v1");

Console.WriteLine("FakeLocalApimProxy is running");

app.Run();

