using GeoLibrary.Server.Api.Extensions;
using GeoLibrary.Server.Database;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient();
builder.AddServiceDefaults();

builder.AddRedisClientBuilder("cache")
    .WithOutputCache();

builder.Services.AddProblemDetails();
builder.Services.AddAuth(builder.Environment.IsDevelopment());
builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.AddHttpClients();

// Aspire inietta la connection string con chiave "database" (da AppHost: postgres.AddDatabase("database")).
// GetConnectionString("database") la legge dalla configurazione standard .NET.
builder.Services.AddDbContext<GeoLibraryDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("database"),
        o => o.UseNetTopologySuite()
    ));

builder.Services.AddAutoMapper(config => { }, typeof(Program).Assembly);

var app = builder.Build();

// Applica le migration automaticamente all'avvio.
// Sicuro: Aspire garantisce che il database sia pronto via WaitFor(database) in AppHost.
await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<GeoLibraryDbContext>();
    await db.Database.MigrateAsync();
}

app.MapScalarApiReference();

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