using System.Threading.Channels;
using Processing.Models;
using Simulation;
using Infrastructure;
using Processing;
using Prometheus;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel to use dev certificate from environment if provided
var certPath = builder.Configuration["ASPNETCORE_Kestrel:Certificates:Default:Path"]
              ?? builder.Configuration["ASPNETCORE_Kestrel__Certificates__Default__Path"];
var certPassword = builder.Configuration["ASPNETCORE_Kestrel:Certificates:Default:Password"]
                 ?? builder.Configuration["ASPNETCORE_Kestrel__Certificates__Default__Password"];
if (!string.IsNullOrWhiteSpace(certPath) && File.Exists(certPath))
{
    builder.WebHost.ConfigureKestrel(options =>
    {
        options.ConfigureHttpsDefaults(https =>
        {
            https.ServerCertificate = new System.Security.Cryptography.X509Certificates.X509Certificate2(certPath, certPassword);
        });
    });
}

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSignalR();
builder.Services.AddControllers();
builder.Services.AddHealthChecks();
builder.Services.AddCors(options =>
{
    options.AddPolicy("default", policy =>
        policy
            .WithOrigins(
                builder.Configuration.GetValue<string>("FRONTEND_ORIGIN") ?? "http://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials());
});

// Shared bounded channel for simulator -> processor pipeline
builder.Services.AddSingleton(sp =>
{
    var options = new BoundedChannelOptions(100_000)
    {
        FullMode = BoundedChannelFullMode.DropOldest,
        SingleReader = false,
        SingleWriter = false,
        AllowSynchronousContinuations = false,
    };
    return Channel.CreateBounded<Reading>(options);
});

// Simulator hosted service (emits readings into the channel)
builder.Services.AddHostedService<SimulatorHostedService>();

// Processor service (reads from channel and computes aggregates/anomalies)
builder.Services.AddHostedService<Processing.Processor>();

// Data store
builder.Services.AddSingleton<IRedisStore, RedisStore>();
builder.Services.AddSingleton<Processing.IStorageSink>(sp => sp.GetRequiredService<IRedisStore>());

// Frame broadcaster (coalesces frames and emits to SignalR groups)
// Ensure a single instance is used both as IFrameSink and the hosted service
builder.Services.AddSingleton<Api.FrameBroadcaster>();
builder.Services.AddSingleton<IFrameSink>(sp => sp.GetRequiredService<Api.FrameBroadcaster>());
builder.Services.AddHostedService(sp => sp.GetRequiredService<Api.FrameBroadcaster>());

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseCors("default");

// Basic security headers
app.Use((ctx, next) =>
{
    ctx.Response.Headers["X-Content-Type-Options"] = "nosniff";
    ctx.Response.Headers["X-Frame-Options"] = "DENY";
    ctx.Response.Headers["X-XSS-Protection"] = "0";
    return next();
});

// CORS will be configured later (T026)

// API is open locally: no API key enforcement

// Prometheus metrics endpoint (scrape)
app.MapMetrics("/metrics");

// Liveness/Readiness probe for containers
app.MapHealthChecks("/healthz");

// SignalR stream hub mapping (requires API key via middleware)
app.MapHub<Api.StreamHub>("/api/stream");

// Map attribute-routed controllers
app.MapControllers();

app.Run();

// Expose Program class for integration testing
#pragma warning disable SA1600 // Elements should be documented
#pragma warning disable SA1601 // Partial elements should be documented
namespace Api
{
    public partial class Program
    {
    }
}
#pragma warning restore SA1601
#pragma warning restore SA1600
