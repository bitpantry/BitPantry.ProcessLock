using BitPantry.ProcessLock.Implementation.Database;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace BitPantry.ProcessLock.Tests
{
    public enum IntegrationTestServerType
    {
        SqlServer,
        Sqlite
    }

    public abstract class IntegrationTestBase
    {
        protected IConfigurationRoot Config { get; }

        private IServiceProvider SqlServerServiceProvider { get; }
        private IServiceProvider SqliteServiceProvider { get; }

        public IntegrationTestBase()
        {
            Config = new ConfigurationBuilder()
                .SetBasePath(Environment.CurrentDirectory)
                .AddJsonFile("appsettings.json")
                .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")}.json", true, true)
                .Build();

            // create sql server services

            var sqlServerServices = new ServiceCollection();

            sqlServerServices.ConfigureProcessLocks(opt =>
            {
                opt.UseRelationalDatabase()
                    .WithSqlServer(Config.GetConnectionString("SqlServer"))
                    .UseUniqueTableNameSuffix();
            });

            SqlServerServiceProvider = sqlServerServices.BuildServiceProvider();

            // create sqlite services

            var sqliteServices = new ServiceCollection();

            sqliteServices.ConfigureProcessLocks(opt =>
            {
                opt.UseRelationalDatabase()
                    .WithSqlite(Config.GetConnectionString("Sqlite"))
                    .UseUniqueTableNameSuffix();
            });

            SqliteServiceProvider = sqliteServices.BuildServiceProvider();
        }

        protected IServiceScope CreateScope(IntegrationTestServerType serverType)
        {
            return serverType == IntegrationTestServerType.SqlServer
                ? SqlServerServiceProvider.CreateScope()
                : SqliteServiceProvider.CreateScope();
        }

        protected void CheckIfSkipped(IntegrationTestServerType serverType)
        {
            if (serverType == IntegrationTestServerType.Sqlite && Config["Environment"] != "Development")
                throw new SkipException($"{serverType} is disabled for testing in this environment");
        }
    }
}
