using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BitPantry.ProcessLock
{
    public interface IProcessLock
    {
        /// <summary>
        /// Attempts to create a process lock
        /// </summary>
        /// <param name="processName">The name of the process to lock</param>
        /// <param name="lockDuration">The duration in minutes of the lock</param>
        /// <returns>True if the lock was successfully created, otherwise false</returns>
        /// <remarks>The lock is considered expired after the lock duration has elapsed - renewing the lock resets the duration</remarks>
        Task<bool> Create(string processName, int lockDuration);

        /// <summary>
        /// Releases a process lock
        /// </summary>
        /// <param name="processName">The name of the process</param>
        /// <returns>The asynchronous task</returns>
        /// <remarks>Function completes successfully even if process lock doesn't exist</remarks>
        Task Release(string processName);

        /// <summary>
        /// Renews a process lock for the given duration
        /// </summary>
        /// <param name="processName">The name of the process</param>
        /// <param name="lockDuration">The duration in milliseconds to renew the lock for</param>
        /// <param name="minRenewalInterval">The number of milliseconds before the lock expires before it can be renewed - this prevents
        /// unnecessary updates to the locking mechanism while allowing Renew to be called frequently</param>
        /// <returns>Whether or not the lock was renewed</returns>
        Task<bool> Renew(string processName, int lockDuration, int minRenewalInterval);
    }
}
