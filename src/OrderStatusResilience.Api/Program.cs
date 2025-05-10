using OrderStatusResilienceApi.Policies;
using OrderStatusResilienceApi.ExternalServices;
using OrderStatusResilienceApi.Services;
using OrderStatusResilience.Api.ExternalServices;
using OrderStatusResilience.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IRetryTracker, RetryTracker>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddPolicyRegistry(PolicyRegistryFactory.Create(builder.Services.BuildServiceProvider()));

builder.Services.AddHttpClient<IExternalOrderStatusClient, ExternalOrderStatusClient>(client =>
{
    client.BaseAddress = new Uri("https://localhost:7038");
})
.AddPolicyHandlerFromRegistry("ResiliencePolicy");

builder.Services.AddHttpClient("healthcheck", client =>
{
    client.BaseAddress = new Uri("https://localhost:7038");
});

builder.Services.AddScoped<IOrderStatusService, OrderStatusService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
