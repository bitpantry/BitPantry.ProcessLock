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
        /// <param name="lockDuration">The duration in milliseconds of the lock</param>
        /// <returns>The lock token used for interacting with the lock later</returns>
        /// <remarks>The lock is considered expired after the lock duration has elapsed - renewing the lock resets the duration</remarks>
        Task<string> Create(string processName, int lockDuration);

        /// <summary>
        /// Releases a process lock
        /// </summary>
        /// <param name="token">The lock token obtained when creating the lock</param>
        /// <returns>The asynchronous task</returns>
        /// <remarks>Function completes successfully even if process lock doesn't exist</remarks>
        Task Release(string token);

        /// <summary>
        /// Renews a process lock for the given duration
        /// </summary>
        /// <param name="token">The lock token obtained when creating the lock</param>
        /// <param name="lockDuration">The duration in milliseconds to renew the lock for. Using any number less than or equal to 0 and the 
        /// current lock duration will be used</param>
        /// <param name="minRenewDuration">The amount of time in milliseconds before the lock expires before it can be renewed in order to prevent 
        /// overaccessing the synchronization mechanism. Using any number less than or equal to 0 and now minimum renew duration will be applied. The
        /// default is 5000 milliseconds
        /// <returns>Whether or not the lock was renewed</returns>
        Task<bool> Renew(string token, int lockDuration = -1, int minRenewDuration = 5000);

        /// <summary>
        /// Checks to see if an active lock exists
        /// </summary>
        /// <param name="processName">The process name</param>
        /// <returns>Whether or not an active lock exists for this process name</returns>
        Task<bool> Exists(string processName);

        /// <summary>
        /// Creates a process lock scope
        /// </summary>
        /// <param name="processName">The name of the process to create the scope for</param>
        /// <returns>A process lock scope</returns>
        ProcessLockScope BeginScope(string processName);
    }
}
