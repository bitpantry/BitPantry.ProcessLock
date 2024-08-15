using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace BitPantry.ProcessLock.Implementation.SqlServer
{
    public interface IDatabaseProcessLockContext : IDisposable
    {
        IDbConnection Connection { get; }
        bool UseTableNameSuffix { get; }

        bool IsUniqueKeyViolatedException(Exception ex);
    }
}
