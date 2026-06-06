var builder = DistributedApplication.CreateBuilder(args);

// Database
var sql = builder.AddSqlServer("sqlserver");
var catalogDb = sql.AddDatabase("catalogdb");
var basketDb = sql.AddDatabase("basketdb");

// Backends
builder.AddProject<Projects.CatalogService>("catalogservice")
        .WithReference(catalogDb)
        .WaitFor(sql)
        .WaitFor(catalogDb);

//builder.AddProject<Projects.BasketApi>("basket-api")
//       .WithReference(basketDb);

//var apiService = builder.AddProject<Projects.Visiativ_ApiService>("apiservice")
//    .WithHttpHealthCheck("/health");

//builder.AddProject<Projects.Visiativ_Web>("webfrontend")
//    .WithExternalHttpEndpoints()
//    .WithHttpHealthCheck("/health")
//    .WithReference(apiService)
//    .WaitFor(apiService);


builder.Build().Run();
