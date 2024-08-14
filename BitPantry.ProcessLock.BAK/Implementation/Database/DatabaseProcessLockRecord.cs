using System;

namespace BitPantry.ProcessLock
{
    public class DatabaseProcessLockRecord
    {
        public string ProcessName { get; set; }
        public string Token { get; set; }

        private DateTime _expiresOn;

        public DateTime ExpiresOn
        {
            get { return _expiresOn; }
            set 
            {
                if (value.Kind == DateTimeKind.Unspecified)
                    _expiresOn = DateTime.SpecifyKind(value, DateTimeKind.Utc); // assume it's universal time if unspecified
                else if (value.Kind == DateTimeKind.Utc)
                    _expiresOn = value;
                else
                    _expiresOn = value.ToUniversalTime();
            }
        }



        public int LockDuration { get; set; }
    }
}
