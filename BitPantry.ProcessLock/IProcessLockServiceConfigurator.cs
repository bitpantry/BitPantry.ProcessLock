using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace BitPantry.ProcessLock
{
    internal interface IProcessLockServiceConfigurator
    {
        void Configure(IServiceCollection services, ProcessLockOptions options);
    }
}
