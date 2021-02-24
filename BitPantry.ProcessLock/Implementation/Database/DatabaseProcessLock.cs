using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitPantry.ProcessLock.Implementation.Database
{
    public class DatabaseProcessLock : IProcessLock
    {
        private DatabaseProcessLockRepository _repo;

        public DatabaseProcessLock(DatabaseProcessLockRepository repo)
        {
            _repo = repo;
        }

        public async Task<string> Create(string processName, int lockDuration)
        {
            //Token returned would mean new lock was inserted -- null would mean we exited for some reason, i.e. a new lock was not created
            //1. Handle expired lock
            //    a. Check if expired
            //    b. Delete the old
            //2. Try insert new lock
            //3. Handle duplicate PK exception
            //    a. Don’t assume all exceptions are this
            //    b. Handel this exception specifically
            //    c. Let other exceptions bubble up

            if (lockDuration <= 0)
                throw new ArgumentException($"{nameof(lockDuration)} must be greater than 0");

            string token = null;

            //Get existing lock if one exists
            var existingLock = await _repo.ReadByProcessName(processName);

            if (existingLock != null)
            {
                //if a lock exists for this process, check expiration
                if (existingLock.ExpiresOn < DateTime.UtcNow)
                {
                    //if expired, delete the old lock
                    await _repo.Delete(existingLock.Token);
                    //insert a new lock
                    token = await InsertLock(processName, lockDuration);
                }
                // if the expiration is not expired leave creationSuccess at false (exit)

            }
            else // if no lock exists then insert a new lock
            {
                token = await InsertLock(processName, lockDuration);
            }


            //return the result of InsertLock
            return token;
        }

        public async Task<bool> Renew(string token, int lockDuration = -1, int minRenewalInterval = 5000)
        {
            var lockToRenew = await _repo.ReadByToken(token);
            if (lockDuration <= 0) lockDuration = lockToRenew.LockDuration;

            if (lockToRenew != null)
            {
                if (lockToRenew.ExpiresOn.AddMilliseconds(-1 * minRenewalInterval) < DateTime.UtcNow)
                {
                    lockToRenew.ExpiresOn = DateTime.UtcNow.AddMilliseconds(lockDuration);
                    await _repo.Update(lockToRenew);

                    return true;
                }
            }

            return false;
        }

        public async Task Release(string token)
        {
            var lockToRelease = await _repo.ReadByToken(token);

            if (lockToRelease != null)
            {
                await _repo.Delete(lockToRelease.Token);
            }
        }

        public async Task<bool> Exists(string processName)
        {
            var lockToCheck = await _repo.ReadByProcessName(processName);
            return lockToCheck != null && lockToCheck.ExpiresOn > DateTime.UtcNow;
        }

        private async Task<string> InsertLock(string processName, int lockDuration)
        {
            //create the new Process Lock
            var newProcessLock = new DatabaseProcessLockRecord
            {
                ProcessName = processName,
                Token = Guid.NewGuid().ToString(),
                ExpiresOn = DateTime.UtcNow.AddMilliseconds(lockDuration),
                LockDuration = lockDuration
            };

            try
            {
                //try to insert the new process lock
                await _repo.Create(newProcessLock);
            }
            catch (Exception ex)
            {
                if (_repo.IsUniqueKeyViolatedException(ex))
                    return null;

                throw;
            }

            //If no errors are thrown during save then the insert was successful, return true

            return newProcessLock.Token;
        }

        public ProcessLockScope BeginScope(string processName)
            => new ProcessLockScope(this, processName);
    }
}
