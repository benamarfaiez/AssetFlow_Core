var builder = DistributedApplication.CreateBuilder(args);

// 1. Aspire va automatiquement chercher la clé "Parameters:sqlserver-password" dans le User Secrets.
var passwordParameter = builder.AddParameter("sqlserver-password");

// 2. On déclare le serveur SQL Server local (géré via un conteneur Docker par Aspire)
var sqlServer = builder.AddSqlServer("sqlserver-server", password: passwordParameter)
                       .WithLifetime(ContainerLifetime.Session);
// 3. On déclare le nom de la base de données
var database = sqlServer.AddDatabase("assetflow-db");

// 4. On référence le projet Web API et on lui injecte automatiquement la base de données
builder.AddProject<Projects.AssetFlowCore_WebApi>("webapi")
       .WithReference(database);

builder.Build().Run();