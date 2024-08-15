using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitPantry.ProcessLock
{
    public class ProcessLockConfiguration
    {        
        public IServiceCollection Services { get; private set; }

        internal ProcessLockConfiguration(IServiceCollection services) { Services = services; }
    }
}
