using BitPantry.ProcessLock.Implementation.Database;
using System.Net.NetworkInformation;

namespace BitPantry.ProcessLock
{
    public enum ProcessLockImplementation
    {
        Database
    }

    public class ProcessLockOptions
    {
        internal ProcessLockImplementation Implementation { get; private set; }
        internal RelationalDatabaseProcessLockOptions DatabaseProcessLockOptions { get; set; }

        internal ProcessLockOptions() { }

        /// <summary>
        /// Configures process locks to use a relational database as the distributed locking mechanism
        /// </summary>
        /// <returns>The relational database options</returns>
        public RelationalDatabaseProcessLockOptions UseRelationalDatabase() 
        {
            Implementation = ProcessLockImplementation.Database;
            DatabaseProcessLockOptions = new RelationalDatabaseProcessLockOptions();

            return DatabaseProcessLockOptions;
        }
    }
}
