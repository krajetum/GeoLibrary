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

// Credenziali di default di MinIO: valgono solo in locale.
const string minioUser = "minioadmin";
const string minioPassword = "minioadmin";

var minio = builder.AddContainer("minio", "minio/minio")
    .WithArgs("server", "/data", "--console-address", ":9001")
    .WithEnvironment("MINIO_ROOT_USER", minioUser)
    .WithEnvironment("MINIO_ROOT_PASSWORD", minioPassword)
    .WithVolume("geolibrary-minio-data", "/data")
    .WithHttpEndpoint(port: 9000, targetPort: 9000, name: "api")
    .WithHttpEndpoint(port: 9001, targetPort: 9001, name: "console");

var server = builder.AddProject<Projects.GeoLibrary_Server_Api>("server")
    .WithReference(postgres)
    .WithReference(database)
    .WithReference(keycloak)
    .WithReference(cache)
    .WithEnvironment("Minio__Endpoint", minio.GetEndpoint("api"))
    .WithEnvironment("Minio__AccessKey", minioUser)
    .WithEnvironment("Minio__SecretKey", minioPassword)
    // Stesso endpoint Keycloak usato dal frontend: garantisce che l'issuer atteso
    // dall'API combaci con l'`iss` del token (entrambi derivati dalla stessa URL).
    .WithEnvironment("Keycloak__Authority",
        ReferenceExpression.Create($"{keycloak.GetEndpoint("http")}/realms/GeoLibrary"))
    .WaitFor(database)
    .WaitFor(keycloak)
    .WaitFor(cache)
    .WaitFor(minio)
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
