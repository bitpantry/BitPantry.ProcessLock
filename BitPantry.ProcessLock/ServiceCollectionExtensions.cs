using Microsoft.Extensions.DependencyInjection;


namespace BitPantry.ProcessLock
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Configures the process locks feature on startup
        /// </summary>
        /// <param name="services">The applications IServiceCollection </param>
        /// <param name="configAction">The action to configure the process lock</param>
        /// <returns>The IServiceCollection</returns>
        public static IServiceCollection AddProcessLock(this IServiceCollection services, Action<ProcessLockConfiguration> configAction)
        {
            configAction(new ProcessLockConfiguration(services));
            return services;
        }


    }
}
