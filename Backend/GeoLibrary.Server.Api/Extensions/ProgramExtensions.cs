using GeoLibrary.Server.Abstractions;
using GeoLibrary.Server.Services;

namespace GeoLibrary.Server.Api.Extensions;

public static class ProgramExtensions
{

    public static IServiceCollection AddAuth(this IServiceCollection services, bool isDevelopment)
    {
        services.AddAuthentication()
                .AddJwtBearer(o =>
                {
                    o.Authority = "http://keycloak:8080/realms/GeoLibrary";
                    o.Audience = "geolibrary-api";
                    o.RequireHttpsMetadata = !isDevelopment; // solo dev
                });

        services.AddAuthorization();
        return services;
    }

    public static WebApplicationBuilder AddHttpClients(this WebApplicationBuilder builder)
    {
        builder.Services.AddHttpClient<IMapsService, OpenStreetMapService>(client =>
        {
            client.BaseAddress = new Uri(builder.Configuration["OpenStreetMap:BaseUrl"] ?? "https://nominatim.openstreetmap.org/");

            client.DefaultRequestHeaders.UserAgent.ParseAdd("GeoLibraryServerApi/1.0 (https://github.com/GeoLibrary/GeoLibrary)");
        });




        return builder;
    }
}
