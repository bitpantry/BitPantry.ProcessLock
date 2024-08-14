using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace BitPantry.ProcessLock.Implementation.Database
{
    public interface IDatabaseProcessLockContext : IDisposable
    {
        IDbConnection Connection { get; }
        DatabaseProcessLockServerType ServerType { get; }
        bool UseTableNameSuffix { get; }

        bool IsUniqueKeyViolatedException(Exception ex);
    }
}
