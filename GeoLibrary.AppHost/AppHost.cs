var builder = DistributedApplication.CreateBuilder(args);

var keycloak = builder.AddKeycloak("keycloak", 8080)
                      .WithDataVolume()
                      .WithRealmImport("../Keycloak/realms");


var cache = builder.AddRedis("cache");

var postgres = builder.AddPostgres("postgres")
    .WithImage("postgis/postgis")
    .WithImageTag("17-3.5")
    .WithDataVolume("geolibrary-postgres-data");

var database = postgres.AddDatabase("database");

var server = builder.AddProject<Projects.GeoLibrary_Server_Api>("server")
    .WithReference(postgres)
    .WithReference(database)
    .WithReference(keycloak)
    .WithReference(cache)
    // Stesso endpoint Keycloak usato dal frontend: garantisce che l'issuer atteso
    // dall'API combaci con l'`iss` del token (entrambi derivati dalla stessa URL).
    .WithEnvironment("Keycloak__Authority",
        ReferenceExpression.Create($"{keycloak.GetEndpoint("http")}/realms/GeoLibrary"))
    .WaitFor(database)
    .WaitFor(keycloak)
    .WaitFor(cache)
    .WithHttpHealthCheck("/health")
    .WithExternalHttpEndpoints();

var webfrontend = builder.AddViteApp("webfrontend", "../Frontend/GeoLibrary.Frontend")
                         .WithReference(server)
                         .WithEnvironment("VITE_API_URL", server.GetEndpoint("http"))
                         .WithEnvironment("VITE_KEYCLOAK_URL", keycloak.GetEndpoint("http"))
                         .WaitFor(server)
                         .WithEndpoint("http", endpoint =>
                         {
                             endpoint.Port = 5173;
                             endpoint.TargetPort = 5173;
                             endpoint.IsProxied = false;
                         });

server.PublishWithContainerFiles(webfrontend, "wwwroot");

builder.Build().Run();
