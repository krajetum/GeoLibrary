using GeoLibrary.Server.Api.Extensions;

var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

var isDevelopment = env == "Development";

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

builder.AddRedisClientBuilder("cache")
    .WithOutputCache();

// Add services to the container.
builder.Services.AddProblemDetails();

builder.Services.AddAuth(isDevelopment);

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseOutputCache();

app.MapDefaultEndpoints();

app.UseFileServer();

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();