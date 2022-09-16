﻿using Microsoft.Extensions.DependencyInjection;

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
            services.AddSingleton<IBinaryDocumentDbFactory, BinaryDocumentDbFactory>();

            return services;
        }
    }
}
