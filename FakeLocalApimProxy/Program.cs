using FakeLocalApimProxy;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient();
builder.Services.AddSingleton<Proxy>();

var app = builder.Build();

app.UseMiddleware<Proxy>();

app.MapGet("/", () => "Hello World! from the fakeLocalApimProxy v1");

app.Run();

Console.WriteLine($"FakeLocalApimProxy is running on {string.Join(",", app.Urls)}");