using System.Runtime.Versioning;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using SharpLab.Container.Manager.Internal;

namespace SharpLab.Container.Manager {
    [SupportedOSPlatform("windows")]
    public class Program {
        public static void Main(string[] args) {
            DotEnv.Load();

            var builder = WebApplication.CreateBuilder(args);
            builder.AddServiceDefaults();

            var startup = new Startup();
            startup.ConfigureServices(builder.Services);

            var app = builder.Build();
            app.MapDefaultEndpoints();
            startup.Configure(app);
            app.Run();
        }
    }
}
