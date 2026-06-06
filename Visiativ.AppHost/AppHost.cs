var builder = DistributedApplication.CreateBuilder(args);

// Connection string statique dans Web.config du BasketService => cf. appsettings.Development.json 
var sqlPassword = builder.AddParameter("sql-password", secret: true);

// Database
var sql = builder.AddSqlServer("sqlserver", password: sqlPassword);
var catalogDb = sql.AddDatabase("catalogdb");
var basketDb = sql.AddDatabase("basketdb");

// Backends
builder.AddProject<Projects.CatalogService>("catalogservice")
        .WithReference(catalogDb)
        .WaitFor(sql)
        .WaitFor(catalogDb);

// BasketService (.NET Framework 4.8) : AddProject<T> ne supporte pas les anciens projets.
// La connection string est hardcodée dans Web.config — hostname et port sont fixes
// dans le réseau Aspire (sqlserver.dev.internal:1433), seul le mdp était variable.
builder.AddDockerfile("basket-api", "../BasketService")
       .WithHttpEndpoint(targetPort: 8080)
       .WaitFor(sql)
       .WaitFor(basketDb);

//var apiService = builder.AddProject<Projects.Visiativ_ApiService>("apiservice")
//    .WithHttpHealthCheck("/health");

//builder.AddProject<Projects.Visiativ_Web>("webfrontend")
//    .WithExternalHttpEndpoints()
//    .WithHttpHealthCheck("/health")
//    .WithReference(apiService)
//    .WaitFor(apiService);

builder.Build().Run();
