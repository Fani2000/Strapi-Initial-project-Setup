var builder = DistributedApplication.CreateBuilder(args);

// Infra
var redis = builder.AddRedis("redis"); 
var postgres = builder.AddPostgres("postgres")
    .WithDataVolume(); 

// Databases (optional but recommended separation)
var apiDb = postgres.AddDatabase("apidb");
var strapiDb = postgres.AddDatabase("strapidb");

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
    .WithHttpEndpoint(env: "PORT", port: 5145, name: "api");

var web = builder.AddNpmApp("webfrontend", "../frontend", scriptName: "start")
    .WithReference(api)
    .WaitFor(api)
    .WithEnvironment("REACT_APP_API_BASE_URL", api.GetEndpoint("api"))
    .WithHttpEndpoint(env: "PORT", name: "web-http");

builder.Build().Run();