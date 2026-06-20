using GeoLibrary.Server.Abstractions;
using GeoLibrary.Server.Api.Extensions;
using GeoLibrary.Server.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.Services.AddHttpClient();
builder.AddServiceDefaults();

builder.AddRedisClientBuilder("cache")
    .WithOutputCache();

// Add services to the container.
builder.Services.AddProblemDetails();

builder.Services.AddAuth(builder.Environment.IsDevelopment());

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.AddHttpClients();

builder.Services.AddDbContext<GeoLibrary.Server.Database.GeoLibraryDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("database"));
});



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