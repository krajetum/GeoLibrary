using GeoLibrary.Server.Abstractions;
using GeoLibrary.Server.Services;
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
                });

        services.AddAuthorization();
        return services;
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
}
