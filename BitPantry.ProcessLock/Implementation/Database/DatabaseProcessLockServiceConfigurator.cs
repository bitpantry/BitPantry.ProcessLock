using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace BitPantry.ProcessLock.Implementation.Database
{
    internal class DatabaseProcessLockServiceConfigurator : IProcessLockServiceConfigurator
    {
        public void Configure(IServiceCollection services, ProcessLockOptions options)
        {
            services.AddTransient<IProcessLock, DatabaseProcessLock>();
            services.AddTransient<DatabaseProcessLockRepository>();

            switch (options.DatabaseProcessLockOptions.ServerType)
            {
                case DatabaseProcessLockServerType.SqlServer:
                    services.AddScoped<IDatabaseProcessLockContext>(svc => 
                        new SqlServerProcessLockContext(
                            options.DatabaseProcessLockOptions.ConnectionString, 
                            options.DatabaseProcessLockOptions.DoUseUniqueTableNameSuffix));
                    break;
                case DatabaseProcessLockServerType.Sqlite:
                    services.AddScoped<IDatabaseProcessLockContext>(svc => 
                        new SqliteProcessLockContext(
                            options.DatabaseProcessLockOptions.ConnectionString, 
                            options.DatabaseProcessLockOptions.DoUseUniqueTableNameSuffix));
                    break;
                default:
                    throw new ArgumentOutOfRangeException($"{options.DatabaseProcessLockOptions.ServerType} is not defined for this switch");
            }
        }
    }
}
