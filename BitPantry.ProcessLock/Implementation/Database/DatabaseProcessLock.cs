using System;
using System.Collections.Generic;
using System.Data.SqlClient;
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

        public async Task<bool> Create(string processName, int lockDuration)
        {
            //True would mean new lock was inserted -- False would mean we exited for some reason, i.e. a new lock was not created
            //1. Handle expired lock
            //    a. Check if expired
            //    b. Delete the old
            //2. Try insert new lock
            //3. Handle duplicate PK exception
            //    a. Don’t assume all exceptions are this
            //    b. Handel this exception specifically
            //    c. Let other exceptions bubble up

            var creationSuccess = false;

            //Get existing lock if one exists
            var existingLock = await _repo.Read(processName);

            if (existingLock != null)
            {
                //if a lock exists for this process, check expiration
                if (existingLock.ExpiresOn < DateTime.Now)
                {
                    //if expired, delete the old lock
                    await _repo.Delete(existingLock.Id);
                    //insert a new lock
                    creationSuccess = await InsertLock(processName, lockDuration);
                }
                // if the expiration is not expired leave creationSuccess at false (exit)

            }
            else // if no lock exists then insert a new lock
            {
                creationSuccess = await InsertLock(processName, lockDuration);
            }


            //return the result of InsertLock
            return creationSuccess;
        }

        public async Task<bool> Renew(string processName, int lockDuration, int minRenewalInterval)
        {
            var lockToRenew = await _repo.Read(processName);

            if (lockToRenew != null)
            {
                if (lockToRenew.ExpiresOn.AddMilliseconds(-1 * minRenewalInterval) < DateTime.Now)
                {
                    lockToRenew.ExpiresOn = DateTime.Now.AddMilliseconds(lockDuration);
                    await _repo.Update(lockToRenew);

                    return true;
                }
            }

            return false;
        }

        public async Task Release(string processName)
        {
            var lockToRelease = await _repo.Read(processName);

            if (lockToRelease != null)
            {
                await _repo.Delete(lockToRelease.Id);
            }
        }

        private async Task<bool> InsertLock(string processName, int lockDuration)
        {
            //create the new Process Lock
            var newProcessLock = new DatabaseProcessLockRecord
            {
                Id = processName,
                HostName = Environment.MachineName,
                ExpiresOn = DateTime.Now.AddMilliseconds(lockDuration)
            };

            try
            {
                //try to insert the new process lock
                await _repo.Create(newProcessLock);
            }
            catch (Exception ex)
            {
                if (_repo.IsUniqueKeyViolatedException(ex))
                    return false;

                throw;
            }

            //If no errors are thrown during save then the insert was successful, return true

            return true;
        }
    }
}
