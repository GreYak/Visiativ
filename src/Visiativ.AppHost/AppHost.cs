var builder = DistributedApplication.CreateBuilder(args);

var sqlPassword = builder.AddParameter("sql-password", secret: true);

// Database
var sql = builder.AddSqlServer("sqlserver", password: sqlPassword);
var catalogDb = sql.AddDatabase("catalogdb");
var basketDb  = sql.AddDatabase("basketdb");

// CatalogService (.NET 10)
var catalogService = builder.AddProject<Projects.CatalogService>("catalogservice")
    .WithReference(catalogDb)
    .WaitFor(sql)
    .WaitFor(catalogDb);

// BasketService (.NET Framework 4.8 — connection string dans Web.config)
var basketApi = builder.AddDockerfile("basket-api", "../BasketService")
    .WithHttpEndpoint(targetPort: 8080, name: "http");

// BFF 
builder.AddProject<Projects.Visiativ_ApiService>("apiservice")
    .WithReference(catalogService)
    .WithReference(basketApi.GetEndpoint("http"))
    .WaitFor(catalogService)
    .WithHttpHealthCheck("/health");

//builder.AddProject<Projects.Visiativ_Web>("webfrontend")
//    .WithExternalHttpEndpoints()
//    .WithHttpHealthCheck("/health")
//    .WithReference(apiService)
//    .WaitFor(apiService);

builder.Build().Run();
