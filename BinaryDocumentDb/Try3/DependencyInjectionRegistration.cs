using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace BinaryDocumentDb
{
    public static class DependencyInjectionRegistration
    {
        public static IServiceCollection RegisterBinaryDocumentDb(this IServiceCollection services)
        {


            return services;
        }
    }
}
