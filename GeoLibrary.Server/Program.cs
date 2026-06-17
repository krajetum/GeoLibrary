var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

var isDevelopment = env == "Development";

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

builder.AddRedisClientBuilder("cache")
    .WithOutputCache();

// Add services to the container.
builder.Services.AddProblemDetails();

builder.Services.AddAuthentication()
    .AddJwtBearer(o =>
    {
        o.Authority = "http://keycloak:8080/realms/geolibrary";
        o.Audience = "geolibrary.api";
        o.RequireHttpsMetadata = isDevelopment; // solo dev
    });
builder.Services.AddAuthorization();



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

app.Run();