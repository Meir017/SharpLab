using System.Numerics;

var builder = DistributedApplication.CreateBuilder(args);

var webappPort = builder.AddParameter("webapp-port", "44200");

var storage = builder.AddAzureStorage("storage")
    .RunAsEmulator(options => options.WithLifetime(ContainerLifetime.Persistent).WithBlobPort(10000));
var storageBlob = storage.AddBlobs("cache");
var blobContainer = storageBlob.AddBlobContainer("edge");

var containerManager = builder.AddProject<Projects.Container_Manager>("container-manager");

var installWebApp = builder.AddExecutable("install-webapp", "npm", "../WebApp", "install");
var buildWebApp = builder.AddExecutable("build-webapp", "npm", "../WebApp", "run", "build")
    .WithParentRelationship(installWebApp)
    .WaitForCompletion(installWebApp);
var webapp = builder.AddNpmApp("webapp", "../WebApp", scriptName: "start:fast")
    .WaitForCompletion(buildWebApp)
    .WithHttpEndpoint(env: "PORT")
    .WithEnvironment("PORT", webappPort)
    .WithEnvironment("test", blobContainer).WaitFor(blobContainer);
installWebApp.WithParentRelationship(webapp);

var webappServer = builder.AddProject<Projects.WebApp_Server>("webapp-server")
    .WaitFor(webapp)
    .WaitFor(containerManager)
    .WithEnvironment("SHARPLAB_LOCAL_SECRETS_PublicStorageConnectionString", storageBlob).WaitFor(storageBlob);

builder.Build().Run();
