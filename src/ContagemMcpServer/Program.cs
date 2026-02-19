using ContagemMcpServer.Tools;
using ContagemMcpServer.Tracing;
using Grafana.OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using StackExchange.Redis;
using Testcontainers.Redis;

var builder = WebApplication.CreateBuilder(args);

var redisContainer = new RedisBuilder("redis:8.6.0")
  .Build();
await redisContainer.StartAsync();

using var connectionRedis = ConnectionMultiplexer.Connect(
    redisContainer.GetConnectionString());
builder.Services.AddSingleton(connectionRedis);

var resourceBuilder = ResourceBuilder.CreateDefault()
    .AddService(serviceName: OpenTelemetryExtensions.ServiceName,
        serviceVersion: OpenTelemetryExtensions.ServiceVersion);
builder.Services.AddOpenTelemetry()
    .WithTracing((traceBuilder) =>
    {
        traceBuilder
            .AddSource(OpenTelemetryExtensions.ServiceName)
            .SetResourceBuilder(resourceBuilder)
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddRedisInstrumentation(connectionRedis)
            .AddConsoleExporter()
            .UseGrafana();
    });

builder.Services.AddMcpServer()
    .WithHttpTransport()
    .WithTools<ContadorTool>();

var app = builder.Build();

app.MapMcp();

app.Run();