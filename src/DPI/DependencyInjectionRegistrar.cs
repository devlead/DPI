using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;

namespace DPI
{
    internal class DependencyInjectionRegistrar : ITypeRegistrar, IDisposable
    {
        private class DependencyInjectionResolver : ITypeResolver, IDisposable
        {
            private ServiceProvider ServiceProvider { get; }

            internal DependencyInjectionResolver(ServiceProvider serviceProvider)
            {
                ServiceProvider = serviceProvider;
            }

            public void Dispose()
            {
                ServiceProvider.Dispose();
            }

            public object Resolve(Type type)
            {
                return ServiceProvider.GetService(type) ?? Activator.CreateInstance(type);
            }
        }
        private IServiceCollection Services { get; }
        private IList<IDisposable> BuiltProviders { get; }

        public DependencyInjectionRegistrar(IServiceCollection services)
        {
            Services = services;
            BuiltProviders = new List<IDisposable>();
        }
        public ITypeResolver Build()
        {
            var buildServiceProvider = Services.BuildServiceProvider();
            BuiltProviders.Add(buildServiceProvider);
            return new DependencyInjectionResolver(buildServiceProvider);
        }

        public void Register(Type service, Type implementation)
        {
            Services.AddSingleton(service, implementation);
        }

        public void RegisterInstance(Type service, object implementation)
        {
            Services.AddSingleton(service, implementation);
        }

        public void Dispose()
        {
            foreach (var provider in BuiltProviders)
            {
                provider.Dispose();
            }
        }
    }
}