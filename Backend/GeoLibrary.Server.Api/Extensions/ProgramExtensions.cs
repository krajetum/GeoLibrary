using System.Security.Claims;
using System.Text.Json;
using GeoLibrary.Server.Abstractions;
using GeoLibrary.Server.Abstractions.Models;
using GeoLibrary.Server.Abstractions.Services;
using GeoLibrary.Server.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Minio;

namespace GeoLibrary.Server.Api.Extensions;

public static class ProgramExtensions
{
    public const string FrontendCorsPolicy = "FrontendCorsPolicy";

    public static IServiceCollection AddFrontendCors(this IServiceCollection services, IConfiguration configuration)
    {
        // Origini consentite lette da configurazione (Cors:AllowedOrigins),
        // con fallback all'origine del dev server Vite.
        var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? ["http://localhost:5173"];

        services.AddCors(options =>
        {
            options.AddPolicy(FrontendCorsPolicy, policy =>
                policy.WithOrigins(allowedOrigins)
                      .AllowAnyHeader()
                      .AllowAnyMethod());
        });

        return services;
    }

    public static IServiceCollection AddAuth(this IServiceCollection services, IConfiguration configuration, bool isDevelopment)
    {
        // Iniettata dall'AppHost (Keycloak__Authority) e derivata dallo stesso endpoint
        // Keycloak usato dal frontend, così l'issuer combacia con l'`iss` del token.
        var authority = configuration["Keycloak:Authority"]
            ?? "https://localhost:8080/realms/GeoLibrary";

        services.AddAuthentication()
                .AddJwtBearer(o =>
                {
                    o.Authority = authority;
                    o.Audience = "geolibrary-api";
                    o.RequireHttpsMetadata = !isDevelopment; // solo dev
                    o.MapInboundClaims = false;

                    // Con MapInboundClaims = false i claim restano quelli originali del token:
                    // i ruoli di realm arrivano dentro l'oggetto JSON "realm_access" e non
                    // verrebbero visti da [Authorize(Roles = ...)]. Li appiattiamo qui.
                    o.TokenValidationParameters.RoleClaimType = ClaimTypes.Role;
                    o.TokenValidationParameters.NameClaimType = "preferred_username";

                    o.Events = new JwtBearerEvents
                    {
                        OnTokenValidated = context =>
                        {
                            if (context.Principal?.Identity is ClaimsIdentity identity)
                            {
                                foreach (var role in ExtractRealmRoles(identity))
                                {
                                    identity.AddClaim(new Claim(ClaimTypes.Role, role));
                                }
                            }

                            return Task.CompletedTask;
                        }
                    };
                });

        services.AddAuthorization(options =>
        {
            options.AddPolicy(AuthPolicies.Admin, policy => policy.RequireRole(AuthPolicies.AdminRole));
        });

        return services;
    }

    /// <summary>
    /// Legge i ruoli di realm dal claim "realm_access" del token Keycloak,
    /// che ha forma {"roles":["admin","user",...]}. Un token senza il claim
    /// (o con JSON inatteso) non è un errore: l'utente semplicemente non ha ruoli.
    /// </summary>
    private static IEnumerable<string> ExtractRealmRoles(ClaimsIdentity identity)
    {
        var realmAccess = identity.FindFirst("realm_access")?.Value;
        if (string.IsNullOrWhiteSpace(realmAccess))
        {
            return [];
        }

        try
        {
            using var document = JsonDocument.Parse(realmAccess);
            if (!document.RootElement.TryGetProperty("roles", out var roles) || roles.ValueKind != JsonValueKind.Array)
            {
                return [];
            }

            return roles.EnumerateArray()
                        .Select(r => r.GetString())
                        .Where(r => !string.IsNullOrWhiteSpace(r))
                        .Select(r => r!)
                        .ToList();
        }
        catch (JsonException)
        {
            return [];
        }
    }

    public static IServiceCollection AddStorage(this IServiceCollection services, IConfiguration configuration)
    {
        // Endpoint iniettato dall'AppHost (Minio__Endpoint), es. http://localhost:9000
        var endpoint = new Uri(configuration["Minio:Endpoint"] ?? "http://localhost:9000");

        services.AddSingleton<IMinioClient>(_ => new MinioClient()
            .WithEndpoint(endpoint.Host, endpoint.Port)
            .WithCredentials(configuration["Minio:AccessKey"] ?? "minioadmin",
                             configuration["Minio:SecretKey"] ?? "minioadmin")
            .WithSSL(endpoint.Scheme == Uri.UriSchemeHttps)
            .Build());

        services.AddScoped<IStorageService, MinioStorageService>();

        return services;
    }

    public static WebApplicationBuilder AddHttpClients(this WebApplicationBuilder builder)
    {
        builder.Services.AddHttpClient<IMapsService, OpenStreetMapService>(client =>
        {
            client.BaseAddress = new Uri(builder.Configuration["OpenStreetMap:BaseUrl"] ?? "https://nominatim.openstreetmap.org/");

            client.DefaultRequestHeaders.UserAgent.ParseAdd("GeoLibraryServerApi/1.0 (https://github.com/GeoLibrary/GeoLibrary)");
        });

        builder.Services.AddHttpClient<ISBNService>(client =>
        {
            client.BaseAddress = new Uri(builder.Configuration["ISBN:BaseUrl"] ?? "https://openlibrary.org/api/books");
            client.DefaultRequestHeaders.UserAgent.ParseAdd("GeoLibraryServerApi/1.0 (https://github.com/GeoLibrary/GeoLibrary)");
        });


        return builder;
    }


    public static WebApplicationBuilder AddTrackingServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddScoped<ITrackingService<BookTrackingRequest>, BookViewTrackingService>();
        builder.Services.AddScoped<ITrackingService<LibraryTrackingRequest>, LibraryViewTrackingService>();
        return builder;
    }
}
