using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BitPantry.ProcessLock
{
    /// <summary>
    /// A process lock scope will create and maintain a process lock on an asynchronous thread until disposed or explicitly stopped
    /// </summary>
    public class ProcessLockScope : IDisposable
    {
        private readonly int LockDuration = 30000;
        private readonly int MinRenewDuration = 50000;

        private bool _isRunning = false;
        private Task _task = null;

        private IProcessLock _locks;
        private string _processName;

        public string Token { get; private set; }
        public bool IsLocked => !string.IsNullOrEmpty(Token);

        internal ProcessLockScope(
            IProcessLock locks,
            string processName)
        {
            _locks = locks;
            _processName = processName;

            Token = _locks.Create(_processName, LockDuration).GetAwaiter().GetResult();
            
            if(IsLocked)
                _task = Run();
        }

        private async Task Run()
        {
            _isRunning = true;

            DateTime expiresOn = DateTime.MinValue.ToUniversalTime();

            do
            {
                if (expiresOn.AddMilliseconds(-1 * MinRenewDuration) < DateTime.UtcNow)
                {
                    await _locks.Renew(Token);
                    expiresOn = DateTime.UtcNow.AddMilliseconds(LockDuration);
                }

                await Task.Delay(500);

            } while (_isRunning);
        }

        public void Stop()
        {
            lock (this)
            {
                if (_isRunning)
                {
                    _isRunning = false;
                    _task?.GetAwaiter().GetResult();
                    _locks.Release(Token).GetAwaiter().GetResult();
                    Token = null;
                }
            }
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
