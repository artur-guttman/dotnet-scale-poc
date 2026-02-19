using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using k8s;
using k8s.Models;

int desiredReplicas = 1;

// Identificator unic pentru pod - Kubernetes setează HOSTNAME automat ca nume de pod
string podName = Environment.GetEnvironmentVariable("HOSTNAME") ?? $"local-{Guid.NewGuid().ToString()[..8]}";

// Counter state accesibil din endpoint
var counterState = new CounterState();

IKubernetes? k8sClient = null;
string? currentNamespace = null;
string deploymentName = "dotnet-scale-poc";

try
{
    var config = KubernetesClientConfiguration.InClusterConfig();
    k8sClient = new Kubernetes(config);

    var nsFile = "/var/run/secrets/kubernetes.io/serviceaccount/namespace";
    if (File.Exists(nsFile))
    {
        currentNamespace = File.ReadAllText(nsFile).Trim();
    }
    Console.WriteLine($"[{podName}] Kubernetes client initialized. Namespace: {currentNamespace}");
}
catch (Exception ex)
{
    Console.WriteLine($"[{podName}] Could not initialize Kubernetes client: {ex.Message}");
}

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHostedService<CounterService>();
builder.Services.AddSingleton(new PodInfo(podName));
builder.Services.AddSingleton(counterState);

var app = builder.Build();

/*
 * ca sa nu raspunda acelasi pod folosim in powershell comanda (altfel browserul are keep-alive si reuse de sesiune si loveste acelasi pod)
 * 1..10 | % { curl.exe -H "Connection: close" "https://dotnet-scale-poc-artur-guttman-dev.apps.rm2.thpm.p1.openshiftapps.com/whoami" }
 * */
app.MapGet("/whoami", () =>
{
    return Results.Ok(new
    {
        pod = podName,
        count = counterState.Count,
        timestamp = DateTime.UtcNow.ToString("HH:mm:ss")
    });
});

//// Apelează rapid pentru a vedea diferite pod-uri răspunzând
//app.MapGet("/poll", async () =>
//{
//    var responses = new List<object>();
//    // Returnează 5 răspunsuri rapide - load balancer-ul va distribui către diferite pod-uri
//    for (int i = 0; i < 5; i++)
//    {
//        responses.Add(new
//        {
//            pod = podName,
//            count = counterState.Count,
//            timestamp = DateTime.UtcNow.ToString("HH:mm:ss.fff")
//        });
//        await Task.Delay(100);
//    }
//    return Results.Ok(responses);
//});

app.MapGet("/set", async (int value) =>
{
    if (value < 1)
        value = 1;

    desiredReplicas = value;
    Console.WriteLine($"[{podName}] Desired replicas set to {desiredReplicas}");

    if (k8sClient != null && currentNamespace != null)
    {
        try
        {
            var scale = await k8sClient.AppsV1.ReadNamespacedDeploymentScaleAsync(deploymentName, currentNamespace);
            scale.Spec.Replicas = desiredReplicas;
            await k8sClient.AppsV1.ReplaceNamespacedDeploymentScaleAsync(scale, deploymentName, currentNamespace);

            Console.WriteLine($"[{podName}] Deployment scaled to {desiredReplicas} replicas");
            return Results.Ok(new { replicas = desiredReplicas, scaled = true, message = "Deployment scaled successfully" });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{podName}] Error scaling: {ex.Message}");
            return Results.Ok(new { replicas = desiredReplicas, scaled = false, error = ex.Message });
        }
    }

    return Results.Ok(new { replicas = desiredReplicas, scaled = false, message = "Kubernetes client not available" });
});

app.MapGet("/desired-replicas", () => desiredReplicas);

app.MapGet("/current-replicas", async () =>
{
    if (k8sClient != null && currentNamespace != null)
    {
        try
        {
            var scale = await k8sClient.AppsV1.ReadNamespacedDeploymentScaleAsync(deploymentName, currentNamespace);
            return Results.Ok(new
            {
                desired = desiredReplicas,
                specReplicas = scale.Spec.Replicas,
                statusReplicas = scale.Status.Replicas
            });
        }
        catch (Exception ex)
        {
            return Results.Ok(new { error = ex.Message });
        }
    }
    return Results.Ok(new { desired = desiredReplicas, message = "Kubernetes client not available" });
});

app.MapGet("/", () => $"App is running on pod: {podName}");

app.MapGet("/health", () => Results.Ok("Healthy"));

app.Run();


public record PodInfo(string PodName);

public class CounterState
{
    public int Count { get; set; } = 0;
}

public class CounterService : BackgroundService
{
    private readonly string _podName;
    private readonly CounterState _state;

    public CounterService(PodInfo podInfo, CounterState state)
    {
        _podName = podInfo.PodName;
        _state = state;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Console.WriteLine($"[{_podName}] Counter started!");

        while (!stoppingToken.IsCancellationRequested)
        {
            _state.Count += 2;
            Console.WriteLine($"[{_podName}] Count: {_state.Count}");
            await Task.Delay(1000, stoppingToken);
        }
    }
}