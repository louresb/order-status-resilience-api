using System.Net.Mime;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using OrderStatusResilience.Api.ExternalServices;
using OrderStatusResilience.Api.Health;
using OrderStatusResilience.Api.Services;
using OrderStatusResilience.Api.Simulations;
using Polly;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<SimulationAttemptTracker>();
builder.Services.AddSingleton<IExternalOrderSimulator, ExternalOrderSimulator>();
builder.Services.AddTransient<SimulatedExternalOrderHandler>();

builder.Services
    .AddHttpClient<IExternalOrderStatusClient, ExternalOrderStatusClient>(client =>
    {
        client.BaseAddress = new Uri("https://simulated-orders.local");
    })
    .ConfigurePrimaryHttpMessageHandler<SimulatedExternalOrderHandler>()
    .AddStandardResilienceHandler(options =>
    {
        options.Retry.MaxRetryAttempts = 2;
        options.Retry.Delay = TimeSpan.FromMilliseconds(50);
        options.Retry.BackoffType = DelayBackoffType.Exponential;
        options.Retry.UseJitter = false;

        options.CircuitBreaker.FailureRatio = 1;
        options.CircuitBreaker.MinimumThroughput = 4;
        options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(10);
        options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(5);

        options.AttemptTimeout.Timeout = TimeSpan.FromMilliseconds(250);
        options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(2);
    });

builder.Services
    .AddHttpClient<ExternalOrderServiceHealthCheck>(client =>
    {
        client.BaseAddress = new Uri("https://simulated-orders.local");
    })
    .ConfigurePrimaryHttpMessageHandler<SimulatedExternalOrderHandler>();

builder.Services.AddHealthChecks()
    .AddCheck<ExternalOrderServiceHealthCheck>("external-order-service");

builder.Services.AddScoped<IOrderStatusService, OrderStatusService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = MediaTypeNames.Application.Json;

        var response = new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
                description = entry.Value.Description
            })
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
});

app.Run();

public partial class Program;
