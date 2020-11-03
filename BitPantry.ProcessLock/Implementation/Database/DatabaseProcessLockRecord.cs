using System;

namespace BitPantry.ProcessLock
{
    public class DatabaseProcessLockRecord
    {
        public string Id { get; set; }
        public string HostName { get; set; }
        public DateTime ExpiresOn { get; set; }
    }
}
