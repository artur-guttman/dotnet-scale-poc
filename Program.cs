using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;

int desiredReplicas = 1;

// ✅ FOARTE IMPORTANT: args
var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

app.MapGet("/set", (int value) =>
{
    if (value < 1)
        value = 1;

    desiredReplicas = value;
    Console.WriteLine($"Desired replicas set to {desiredReplicas}");
    return desiredReplicas;
});

app.MapGet("/desired-replicas", () =>
{
    return desiredReplicas;
});

app.MapGet("/", () => "App is running");


// ✅ NU mai hardcoda URL aici
app.Run();