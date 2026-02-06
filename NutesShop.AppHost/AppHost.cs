var builder = DistributedApplication.CreateBuilder(args);

// Infra
var redis = builder.AddRedis("redis"); 
var postgres = builder.AddPostgres("postgres")
    .WithDataVolume(); 

// Databases (optional but recommended separation)
var apiDb = postgres.AddDatabase("apidb");
var strapiDb = postgres.AddDatabase("strapidb");

// PgAdmin (web UI for Postgres)
var pgadmin = builder.AddContainer("pgadmin", "dpage/pgadmin4", "latest")
    .WithEnvironment("PGADMIN_DEFAULT_EMAIL", "admin@local.dev")
    .WithEnvironment("PGADMIN_DEFAULT_PASSWORD", "Admin123!")
    .WithHttpEndpoint(targetPort: 80, name: "pgadmin-http")
    .WithReference(postgres);

// Strapi (container)
var strapi = builder.AddNpmApp("strapi", "../nutapp-strapi", scriptName: "dev")
    .WithReference(strapiDb)
    .WithEnvironment("NODE_ENV", "development")
    .WithEnvironment("HOST", "0.0.0.0")
    .WithHttpEndpoint(env: "PORT", port: 1337, name: "strapi-http");

// API (net9)
var api = builder.AddProject<Projects.NutesShop_Server>("api")
    .WithReference(redis)
    .WithReference(apiDb)
    .WithEnvironment("STRAPI_BASE_URL", strapi.GetEndpoint("strapi-http"))
    .WithEnvironment("STRAPI_API_TOKEN",
        "a096bdb7ab1af681f572cde39d8accc9d8e22b6a92adeae3c23c9586c55f1d730b53d5e04804eef012631777c279506deb390b74e08bdf2eb4b5aa5e31d0526f17f36f9c549b08645287e085fcd244c1734811fd43ba0828c9b2a0fe9325d4a2581ed7a490ee75649f3c3883e24744b95ef73fc59ab0e9e0b21504f7f688e6c6")
    .WaitFor(strapi)
    .WithHttpEndpoint(env: "PORT", port: 5145, name: "api-http")
    .WithExternalHttpEndpoints();

var web = builder.AddNpmApp("webfrontend", "../frontend", scriptName: "start")
    .WithReference(api)
    .WaitFor(api)
    .WithEnvironment("REACT_APP_API_BASE_URL", api.GetEndpoint("api-http"))
    .WithHttpEndpoint(env: "PORT", name: "web-http");

builder.Build().Run();
