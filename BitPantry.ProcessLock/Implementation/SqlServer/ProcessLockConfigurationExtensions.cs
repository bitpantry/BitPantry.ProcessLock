using Microsoft.Extensions.DependencyInjection;

namespace BitPantry.ProcessLock.Implementation.SqlServer
{
    public static class ProcessLockConfigurationExtensions
    {
        public static ProcessLockConfiguration UseSqlServer(this ProcessLockConfiguration config, string connectionString, bool useUniqueTableNameSuffix = false)
        {
            config.Services.AddScoped<IProcessLock, DatabaseProcessLock>();
            config.Services.AddScoped<DatabaseProcessLockRepository>();

            config.Services.AddScoped<IDatabaseProcessLockContext>(svc =>
                new SqlServerProcessLockContext(connectionString, useUniqueTableNameSuffix));

            return config;
        }
    }
}
