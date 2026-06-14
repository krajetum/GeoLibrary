var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache");

var postgres = builder.AddPostgres("postgres")
    .WithImage("postgis/postgis")
    .WithImageTag("17-3.5")
    .WithDataVolume("geolibrary-postgres-data");

var database = postgres.AddDatabase("database");

var server = builder.AddProject<Projects.GeoLibrary_Server>("server")
    .WithReference(postgres)
    .WithReference(cache)
    .WaitFor(cache)
    .WaitFor(database)
    .WithHttpHealthCheck("/health")
    .WithExternalHttpEndpoints();

var webfrontend = builder.AddViteApp("webfrontend", "../frontend")
                         .WithReference(server)
                         .WaitFor(server);

server.PublishWithContainerFiles(webfrontend, "wwwroot");

builder.Build().Run();
