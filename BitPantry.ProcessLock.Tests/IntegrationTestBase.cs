using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;


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

            services.ConfigureProcessLocks(opt =>
            {
                opt.UseRelationalDatabase()
                    .WithSqlServer(Config.GetConnectionString("SqlServer"))
                    .UseUniqueTableNameSuffix();
            });

            ServiceProvider = services.BuildServiceProvider();
        }

    }
}
