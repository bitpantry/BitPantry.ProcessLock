using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace BitPantry.ProcessLock.Implementation.Database
{
    public class SqliteProcessLockContext : IDatabaseProcessLockContext
    {
        public bool UseTableNameSuffix { get; }
        public DatabaseProcessLockServerType ServerType => DatabaseProcessLockServerType.Sqlite;
        public IDbConnection Connection { get; private set; }

        public SqliteProcessLockContext(string connectionString, bool useTableNameSuffix)
        {
            UseTableNameSuffix = useTableNameSuffix;

            Connection = new SqliteConnection(connectionString);
            Connection.Open();
        }

        public bool IsUniqueKeyViolatedException(Exception ex)
            => ex is SqliteException exception && exception.SqliteExtendedErrorCode == 1555;

        public void Dispose()
        {
            Connection.Close();
            Connection.Dispose();
        }
    }
}
