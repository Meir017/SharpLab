using System;
using Autofac.Core;
using Autofac.Core.Registration;
using JetBrains.Annotations;

namespace Autofac.Extras.FileSystemRegistration.Internal
{
    public class ImmediateModuleRegistrar : IModuleRegistrar
    {
        private readonly IComponentRegistryBuilder _registry;

        public ImmediateModuleRegistrar([NotNull] IComponentRegistryBuilder registry, ModuleRegistrarData registrarData)
        {
            ArgumentNullException.ThrowIfNull(registry);
            ArgumentNullException.ThrowIfNull(registrarData);

            _registry = registry;
            RegistrarData = registrarData;
        }

        public ModuleRegistrarData RegistrarData { get; }

        public IModuleRegistrar RegisterModule(IModule module)
        {
            module.Configure(_registry);
            return this;
        }
    }
}