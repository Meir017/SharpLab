using Microsoft.Extensions.Hosting;
using Autofac.Extensions.DependencyInjection;
using SharpLab.Server.Common;
using Microsoft.AspNetCore.Builder;
using Autofac;

namespace SharpLab.Server {
    public class Program {
        public static void Main(string[] args) {
            DotEnv.Load();

            var builder = WebApplication.CreateBuilder(args);
            builder.AddServiceDefaults();
            builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());

            var startup = new Startup();
            startup.ConfigureServices(builder.Services);
            builder.Host.ConfigureContainer<ContainerBuilder>(startup.ConfigureContainer);

            var app = builder.Build();
            app.MapDefaultEndpoints();
            startup.Configure(app, app.Environment);
            app.Run();
        }
    }
}
