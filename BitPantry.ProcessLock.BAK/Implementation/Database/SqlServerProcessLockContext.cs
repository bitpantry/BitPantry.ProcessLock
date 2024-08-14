using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Text;

namespace BitPantry.ProcessLock.Implementation.Database
{
    public class SqlServerProcessLockContext : IDatabaseProcessLockContext
    {
        public bool UseTableNameSuffix { get; }
        public DatabaseProcessLockServerType ServerType => DatabaseProcessLockServerType.SqlServer;
        public IDbConnection Connection { get; private set; }

        public SqlServerProcessLockContext(string connectionString, bool useTableNameSuffix)
        {
            UseTableNameSuffix = useTableNameSuffix;

            Connection = new SqlConnection(connectionString);
            Connection.Open();
        }

        public void Dispose()
        {
            Connection.Close();
            Connection.Dispose();
        }

        public bool IsUniqueKeyViolatedException(Exception ex)
            => ex is SqlException exception && exception.Number == 2627;
    }
}
