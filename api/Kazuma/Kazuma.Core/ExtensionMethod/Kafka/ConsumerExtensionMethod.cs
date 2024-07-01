using Kazuma.Common.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Reflection;

namespace Kazuma.Core.ExtensionMethod.Kafka
{
    public static class ConsumerExtensionMethod
    {
        public static IServiceCollection RegisterAssemblyTypes<T>(this IServiceCollection services, ServiceLifetime lifetime, out bool found, List<Func<TypeInfo, bool>> predicates = null, bool usingBaseInterfaceType = false, params Assembly[] assemblies)
        {
            IEnumerable<Assembly> localAssemblies;
            if (assemblies != null && assemblies.Length > 0)
                localAssemblies = assemblies;
            else
            {
                List<Assembly> scanAssemblies = AppDomain.CurrentDomain.GetAssemblies().ToList();
                scanAssemblies.SelectMany(x => x.GetReferencedAssemblies())
                    .Where(t => false == scanAssemblies.Any(a => a.FullName == t.FullName))
                    .Distinct()
                    .ToList()
                    .ForEach(x => scanAssemblies.Add(AppDomain.CurrentDomain.Load(x)));
                localAssemblies = scanAssemblies;
            }

            var interfaces = localAssemblies
                .SelectMany(o => o.DefinedTypes
                    .Where(x => x.IsInterface)
                    .Where(x => x != typeof(T))
                    .Where(x => typeof(T).IsAssignableFrom(x))
                ).Concat(new TypeInfo[] { typeof(T).GetTypeInfo() });

            var serviceDescriptors = new List<ServiceDescriptor>();
            foreach (var @interface in interfaces)
            {
                var types = localAssemblies
                    .SelectMany(o => o.DefinedTypes
                        .Where(x => x.IsClass)
                        .Where(x => @interface.IsAssignableFrom(x))
                    //.Where(x => !x.CustomAttributes.Any(ca => ca.AttributeType == typeof(IgnoreServiceCollectionAttribute)))
                    );

                if (predicates?.Count() > 0)
                {
                    foreach (var predict in predicates)
                    {
                        types = types.Where(predict);
                    }
                }

                foreach (var type in types)
                {
                    serviceDescriptors.Add(new ServiceDescriptor(
                        usingBaseInterfaceType ? typeof(T) : @interface,
                        type,
                        lifetime)
                    );
                }
            }

            services.TryAddEnumerable(serviceDescriptors);
            found = serviceDescriptors.Count > 0;

            return services;
        }

        public static IServiceCollection RegisterKafkaConsumers(this IServiceCollection services, params Assembly[] consumerContainers)
        {
            bool foundConsumers;

            // read later
            services.RegisterAssemblyTypes<ITopicConsumer>(ServiceLifetime.Singleton, out foundConsumers, null, true, consumerContainers);

            // Then register the ConsumerWorker
            // if (foundConsumers)
            services.AddHostedService<ConsumerWorker>();

            return services;
        }
    }
}
