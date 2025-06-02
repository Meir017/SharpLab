using System.Management.Automation;
using Nivot.Aspire.Hosting.PowerShell;

var builder = DistributedApplication.CreateBuilder(args);

const string webappPort = "44200";
const string ContainerHostAuthorizationToken = "d344827a-ca42-4159-95f6-5fb9551d62aa";

var storage = builder.AddAzureStorage("storage")
    .RunAsEmulator(options => options.WithLifetime(ContainerLifetime.Persistent).WithBlobPort(10000));
var storageBlob = storage.AddBlobs("cache");
var blobContainer = storageBlob.AddBlobContainer("edge");

var ps = builder.AddPowerShell("ps", PSLanguageMode.FullLanguage);

var containerManagerBinPath = Path.Join(Directory.GetParent(new Projects.Container_Manager().ProjectPath)!.FullName, "bin", "Debug", "net9.0");
var setupContainerManager = ps.AddScript("Preparing-container-host",
$"""
$containerCapabilityId = New-Object Security.Principal.SecurityIdentifier @(
    'S-1-15-3-1024-4233803318-1181731508-1220533431-3050556506-2713139869-1168708946-594703785-1824610955'
)
$aclRule = New-Object Security.AccessControl.FileSystemAccessRule @(
    $containerCapabilityId,
    [Security.AccessControl.FileSystemRights]::ReadAndExecute,
    ([Security.AccessControl.InheritanceFlags]::ContainerInherit -bor [Security.AccessControl.InheritanceFlags]::ObjectInherit),
    [Security.AccessControl.PropagationFlags]::None,
    [Security.AccessControl.AccessControlType]::Allow
)

$binPath = '{containerManagerBinPath}'
$acl = Get-Acl $binPath
$acl.AddAccessRule($aclRule);
Set-Acl $binPath -AclObject $acl
""").WithArgs(containerManagerBinPath);

var containerManager = builder.AddProject<Projects.Container_Manager>("container-manager")
    .WithEnvironment("SHARPLAB_CONTAINER_HOST_AUTHORIZATION_TOKEN", ContainerHostAuthorizationToken)
    .WaitForCompletion(setupContainerManager);

var mirrorSharp = builder.AddExecutable("mirrorsharp-ci", "npm", "../#external/mirrorsharp/WebAssets", "ci");
var mirrorSharpBuild = builder.AddNpmApp("mirrorsharp-build", "../#external/mirrorsharp/WebAssets", "build")
    .WithParentRelationship(mirrorSharp)
    .WaitForCompletion(mirrorSharp);

var mirrorsharpPreview = builder.AddExecutable("mirrorsharp-preview", "npm", "../#external/mirrorsharp-codemirror-6-preview/WebAssets", "ci");
var mirrorsharpPreviewBuild = builder.AddNpmApp("mirrorsharp-preview-build", "../#external/mirrorsharp-codemirror-6-preview/WebAssets", "build")
    .WithParentRelationship(mirrorsharpPreview)
    .WaitForCompletion(mirrorsharpPreview);

var installWebApp = builder.AddExecutable("install-webapp", "npm", "../WebApp", "install")
    .WaitForCompletion(mirrorSharpBuild)
    .WaitForCompletion(mirrorsharpPreviewBuild);
var buildWebApp = builder.AddNpmApp("build-webapp", "../WebApp", "build")
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
    .WithEnvironment("SHARPLAB_LOCAL_SECRETS_PublicStorageConnectionString", storageBlob).WaitFor(storageBlob)
    .WithEnvironment("SHARPLAB_CONTAINER_HOST_URL", containerManager.GetEndpoint("http"))
    .WithEnvironment("SHARPLAB_ASSETS_BASE_URL", $"http://localhost:{webappPort}/")
    .WithEnvironment("SHARPLAB_ASSETS_LATEST_URL_V2", $"http://localhost:{webappPort}/latest")
    .WithEnvironment("SHARPLAB_LOCAL_SECRETS_ContainerHostAuthorizationToken", ContainerHostAuthorizationToken)
    .WithEnvironment("SHARPLAB_WEBAPP_NAME", "local")
    .WithEnvironment("SHARPLAB_CACHE_PATH_PREFIX", "edge")
    .WithEnvironment("SHARPLAB_ASSETS_RELOAD_TOKEN", "12345");

builder.Build().Run();
