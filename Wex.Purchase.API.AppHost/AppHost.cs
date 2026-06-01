var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.Wex_Purchase_API>("wex-purchase-api");

builder.Build().Run();
