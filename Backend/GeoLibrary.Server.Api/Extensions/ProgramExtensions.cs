namespace GeoLibrary.Server.Api.Extensions;

public static class ProgramExtensions
{

    public static IServiceCollection AddAuth(this IServiceCollection services, bool isDevelopment)
    {
        services.AddAuthentication()
                .AddJwtBearer(o =>
                {
                    o.Authority = "http://keycloak:8080/realms/geolibrary";
                    o.Audience = "geolibrary.api";
                    o.RequireHttpsMetadata = !isDevelopment; // solo dev
                });

        services.AddAuthorization();
        return services;
    }

}
