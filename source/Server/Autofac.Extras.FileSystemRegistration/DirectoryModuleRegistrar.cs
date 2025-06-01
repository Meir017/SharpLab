using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Autofac.Core;
using Autofac.Core.Registration;
using JetBrains.Annotations;

namespace Autofac.Extras.FileSystemRegistration.Internal {
    public class DirectoryModuleRegistrar : IDirectoryModuleRegistrar {
        private readonly ContainerBuilder _builder;
        private readonly string[] _directoryPaths;

        private string _filePattern = "*.*";
        private Func<FileInfo, bool> _fileFilter = f => f.Extension.Equals(".dll", StringComparison.InvariantCultureIgnoreCase)
                                                     || f.Extension.Equals(".exe", StringComparison.InvariantCultureIgnoreCase);
        private Func<Assembly, bool> _assemblyFilter = a => true;

        public DirectoryModuleRegistrar([NotNull] ContainerBuilder builder, params string[] directoryPaths)
        {
            ArgumentNullException.ThrowIfNull(builder);
            _builder = builder;
            _directoryPaths = directoryPaths;

            var callback = _builder.RegisterCallback(DiscoverModules);
            RegistrarData = new ModuleRegistrarData(callback);
        }
        
        public ModuleRegistrarData RegistrarData { get; }

        public IDirectoryModuleRegistrar WhereFileMatches(string filePattern)
        {
            ArgumentNullException.ThrowIfNull(filePattern);

            _filePattern = filePattern;
            return this;
        }

        public IDirectoryModuleRegistrar WhereFile(Func<FileInfo, bool> filter) {
            ArgumentNullException.ThrowIfNull(filter);

            _fileFilter = filter;
            return this;
        }

        public IDirectoryModuleRegistrar WhereAssembly(Func<Assembly, bool> filter) {
            ArgumentNullException.ThrowIfNull(filter);

            _assemblyFilter = filter;
            return this;
        }

        private void DiscoverModules(IComponentRegistryBuilder registry) {
            var files = _directoryPaths.Select(p => new DirectoryInfo(p))
                                       .SelectMany(d => d.GetFiles(_filePattern))
                                       .Where(_fileFilter);

            var registrar = new ImmediateModuleRegistrar(registry, this.RegistrarData);
            foreach (var file in files) {
                var assembly = LoadAssemblySafe(file);
                if (assembly == null || !_assemblyFilter(assembly))
                    continue;

                registrar.RegisterAssemblyModules(assembly);
            }
        }

        [CanBeNull]
        private static Assembly? LoadAssemblySafe(FileInfo file) {
            try {
                return Assembly.LoadFrom(file.FullName);
            }
            catch (BadImageFormatException) {
                return null;
            }
        }

        [NotNull]
        public IModuleRegistrar RegisterModule([NotNull] IModule module) {
            ArgumentNullException.ThrowIfNull(module);
            _builder.RegisterCallback(module.Configure);
            return this;
        }
    }
}