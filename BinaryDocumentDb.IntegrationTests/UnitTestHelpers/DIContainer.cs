// For intergration tests, comment out this #define.
#define UNIT_TESTS

using Microsoft.Extensions.DependencyInjection;

namespace BinaryDocumentDb.IntegrationTests.UnitTestHelpers
{
    internal static class DIContainer
    {
        private static Lazy<IServiceProvider> di = new Lazy<IServiceProvider>(() =>
        {
            var di = new ServiceCollection();

#if UNIT_TESTS

            di.AddTransient<IBinaryDocumentDbFactory, FakeIBinaryDocumentDbFactory> ();
#else

            di.RegisterBinaryDocumentDb();

#endif

            return di.BuildServiceProvider();
        });

        public static IServiceProvider DI => di.Value;
    }
}
