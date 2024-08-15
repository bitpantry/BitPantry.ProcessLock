using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using BitPantry.ProcessLock.Implementation.SqlServer;
using BitPantry.ProcessLock;

namespace BitPantry.ProcessLock.Tests
{

    public abstract class IntegrationTestBase
    {
        protected readonly IConfiguration Config;
        protected readonly IServiceProvider ServiceProvider;

        public IntegrationTestBase()
        {
            Config = new ConfigurationBuilder()
                .SetBasePath(Environment.CurrentDirectory)
                .AddJsonFile("appsettings.json")
                .Build();

            // create sql server services


            var services = new ServiceCollection();

            services.AddProcessLock(opt => opt.UseSqlServer(Config.GetConnectionString("SqlServer"), true));

            ServiceProvider = services.BuildServiceProvider();
        }

    }
}
