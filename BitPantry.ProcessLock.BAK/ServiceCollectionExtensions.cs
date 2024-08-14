using BitPantry.ProcessLock.Implementation.Database;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace BitPantry.ProcessLock
{
    public static class ServiceCollectionExtensions
    {
        private static Dictionary<ProcessLockImplementation, IProcessLockServiceConfigurator> ConfiguratorDict 
            = new Dictionary<ProcessLockImplementation, IProcessLockServiceConfigurator>()
        {
            { ProcessLockImplementation.Database, new DatabaseProcessLockServiceConfigurator() }
        };

        /// <summary>
        /// Configures Process Locks for the given services collection
        /// </summary>
        /// <param name="services">The services collection</param>
        /// <param name="configureOptionsAction">The process lock options</param>
        public static void ConfigureProcessLocks(this IServiceCollection services, Action<ProcessLockOptions> configureOptionsAction)
        {
            var options = new ProcessLockOptions();
            configureOptionsAction(options);
            ConfiguratorDict[options.Implementation].Configure(services, options);
        }  
    }
}
