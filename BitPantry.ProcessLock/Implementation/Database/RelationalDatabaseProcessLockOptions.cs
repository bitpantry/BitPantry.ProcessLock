using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Text;

namespace BitPantry.ProcessLock.Implementation.Database
{
    public enum DatabaseProcessLockServerType
    {
        SqlServer
    }

    public class RelationalDatabaseProcessLockOptions
    {
        internal bool DoUseUniqueTableNameSuffix { get; private set; } = false;

        internal string ConnectionString { get; private set; }

        internal DatabaseProcessLockServerType ServerType { get; private set; }

        internal RelationalDatabaseProcessLockOptions() { }

        /// <summary>
        /// Uses SQL Server as the distributed locking mechanism
        /// </summary>
        /// <param name="connectionString">The connection string to a database where the process lock table should be created</param>
        /// <returns>The options</returns>
        public RelationalDatabaseProcessLockOptions WithSqlServer(string connectionString)
        {
            ServerType = DatabaseProcessLockServerType.SqlServer;
            ConnectionString = connectionString;

            return this;
        }

        /// <summary>
        /// Adds a unique table name suffix for each instance of the IRecordLock (see remarks)
        /// </summary>
        /// <returns>The options</returns>
        /// <remarks>This is primarily used for testing so that parallel tests can
        /// run where the table is created and deleted as a part of the test</remarks>
        public RelationalDatabaseProcessLockOptions UseUniqueTableNameSuffix()
        {
            DoUseUniqueTableNameSuffix = true;
            return this;
        }
    }
}
