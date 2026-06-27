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
    .WaitFor(database)
    .WaitFor(keycloak)
    .WaitFor(cache)
    .WithHttpHealthCheck("/health")
    .WithExternalHttpEndpoints();

var webfrontend = builder.AddViteApp("webfrontend", "../Frontend/GeoLibrary.Frontend")
                         .WithReference(server)
                         .WaitFor(server)
                         .WithEndpoint("http", endpoint =>
                         {
                             endpoint.Port = 5173;
                             endpoint.TargetPort = 5173;
                             endpoint.IsProxied = false;
                         });

server.PublishWithContainerFiles(webfrontend, "wwwroot");

builder.Build().Run();
