using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BinaryDocumentDb
{
    /// <summary>
    /// DI Registrations for BinaryDocumentDb assembly.
    /// </summary>
    public static class DependencyInjectionRegistration
    {
        /// <summary>
        /// Registration of <see cref="IBinaryDocumentDbFactory"/> implementation
        /// </summary>
        public static IServiceCollection RegisterBinaryDocumentDbFactory(this IServiceCollection services)
        {
            services.TryAddSingleton<IBinaryDocumentDbFactory, BinaryDocumentDbFactory>();

            return services;
        }
    }
}
