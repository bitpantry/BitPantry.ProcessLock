[![Build status](https://ci.appveyor.com/api/projects/status/pgaqasbgtgdwpy3u?svg=true)](https://ci.appveyor.com/project/bitpantry/bitpantry-processlock)

`PM> Install-Package BitPantry.ProcessLock`

# BitPantry.ProcessLock
This project was created out of a personal need to be able to synchronize the execution of distributed process instances so that even though multiple instances may be running in different machines, only one process will actually be executing at a time.

# Use Case
*For limiting multiple distributed instances of a process to one execution at a time*

This originated with the advent of the cloud - when I would have a process that gets kicked off by a message on a service bus queue, for example. To achieve high availability, I would have this process running in at least two instances, per Azure / AWS best practice (something like a container environment, or two instances of an Azure app service). However, often I would find that **the nature of the process would only allow once instance to run at a time** (smells funny at first sniff, but I've discovered that there are plenty of good technical approaches where this is the case)

For example, you may have a process that does the following when one or more record of a certain type are created in the database.

1. Looks in the database to determine which records are new and need to be processed
2. Initializes some monitoring data
3. Send each record off to another message queue where another process will do some parallel crunching on the new records
4. Finally, mark the records you've just sent off for processing as *in progress*

This process may be on a timer or it could be triggered by a message queue or web-hook.

In any case, you wouldn't want multiple instances of that process running at the same time. You'll at least get multiple requests to process the same record, but you may also get some weird results in your monitoring data and you won't be able to trust the record status for the records being processed.

So, you can either have only one of these processes running, or you can figure out a way to synchronize their execution so that when one starts up, it checks to see if any other instances are running, and then exits if they are.

# Quick Start
ProcessLock is pretty simple to get setup. 

I've provided an extension function for the `IServiceCollection`. Take a peak at the `BitPantry.ProcessLock.Tests` project for a full setup example and tests of the `IProcessLock` implementations. Since this is designed for distributed systems it necessarily depends on some connected service for the locking mechanism. So, there isn't a default configuration - you'll need to configure a specific implementation when adding it to your project. Out of the box, it comes with a SqlServer implementation.

### For SQL Server
```
services.AddProcessLock(opt => 
    opt.UseSqlServer(Config.GetConnectionString("SqlServer")));
```

As long as the connection string and credentials are valid and have sufficient privileges, the ProcessLock database table (called *ProcessLock*) will be created the first time it is needed.

To create and manage Process Locks, inject the `IProcessLock` dependency.

```
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
        /// overaccessing the locking mechanism. Using any number less than or equal to 0 and now minimum renew duration will be applied. The
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
```

For each of the three functions, there are only three unique parameters.

- `processName` is the name of the process - all instances of the process should use the same name
- `lockDuration` is the lifetime of the lock in milliseconds - once the lock is expired, a lock with the same process name can be created
- `minRenewalInterval` is only used when renewing a lock and helps prevent *abusing* the underlying synchronization mechanism (in the default case, the database) by actually renewing a lock each time `Renew` is called - instead it only actually renews a lock when the lock expiration time minus the `minRenewalInterval` is less than now
- When a lock is successfully created, a `Token` is returned and can be used to renew and release the lock in the future - without a token for a valid lock, the lock cannot be renewed or released

# Example - `ProcessLockScope`
The easiest way to create a process lock is by using a `ProcessLockScope`. You can create a `ProcessLockScope` using the `IProcessLock:BeginScope(string processName)` function.

In this example, the `TestLogic:Execute` function represents a process that may run in multiple simultaneous distributed instances and we only want one to actually execute at a time.

```
public class TestLogic
{
    private readonly string _processName = "MyProcess";
    private readonly IProcessLock _pl;
    private readonly ILogger<TestLogic> _log;

    public TestLogic(IProcessLock pl, ILogger<TestLogic> log)
    {
        _pl = pl;
        _log = log;
    }

    public async Task Execute(IEnumerable<DataToProcess> data)
    {
        // attempt to create a lock for the process named "MyProcess". Using a ProcessLockScope
        // will spin up an asynchronous process to maintain the lock until the scope is
        // disposed, or until scope.Stop() is explicitly called.

        using(var scope = _pl.BeginScope(_processName))
        {
            // to make sure a lock has been created ...

            if(!scope.IsLocked) 
            { 
                // if the scope fails to obtain a lock, it means that a lock is already
                // obtained by another process
            }
            else
            {
                _log.LogDebug(await _pl.Exists(_processName)); // logs 'true'
            }
        }

        _log.LogDebug(await _pl.Exists(_processName)); // logs 'false'

    }
}
```
# Example - Managing Locks Directly
If you want a little more control than the `ProcessLockScope` provides, you can create and maintain locks directly using the `IProcessLock`.

In this example, the `TestLogic:Execute` function represents a process that may run in multiple simultaneous distributed instances and we only want one to actually execute at a time.

```
public class TestLogic
{
    private readonly string _processName = "MyProcess";
    private readonly IProcessLock _pl;
    private readonly ILogger<TestLogic> _log;

    public TestLogic(IProcessLock pl, ILogger<TestLogic> log)
    {
        _pl = pl;
        _log = log;
    }

    public async Task Execute(IEnumerable<DataToProcess> data)
    {
        // attempt to create a lock for the process named "MyProcess" for 15 seconds.
        // if successfully able to create the lock, the Create function returns 
        // true (and the process can continue), otherwise false (and the process can exit)

        var token = await _pl.Create(_processName, 15000)

        if(token != null) 
        {
            _log.Debug("Acquired lock to run process - processing data ...");

            // assuming it takes about 200ms to process a single data item

            var numberOfItemsProcessed = 0;
            foreach (var item in data)
            {
                item.Process();
                numberOfItemsProcessed++;

                // if 0, we've processed 25 items, about 5 seconds have elapsed
                
                if (numberOfItemsProcessed % 25 == 0)
                {
                    // renew the lock and reset it to 15 seconds as long 
                    // as it is within seven seconds of expiring, otherwise do nothing
                    
                    await _pl.Renew(token, 15000, 7000); 
                }
            }

            // all done with processing, so release the lock. You could wrap the whole thing with a 
            // try-catch-finally and release in the finally block. But even if you don't, this lock
            // is always set to expire in 15 seconds, at which point another instance could obtain
            // a lock.

            await _pl.Release(_processName);
        }
        else
        {
            _log.Debug("Process exiting because it is already running somewhere else");
        }
    }
}
```

# How It Works
The project assumes that there are multiple mechanisms for synchronizing distributed process instances and provides the abstractions necessary to implement those metchanisms later. 

Right now, however, this project uses a SqlServer database that can be accessed by all instances of a process as the synchronization mechanism.

There are implementations for both **SQL Server** and **Sqlite**. 

Using a relational database, we leverage a key constraint to ensure that only one instance of the process can ever successfuly enter a lock record into the database at once.

# Extending
If you would like to add a new implementation around a different locking mechanism simply create a new implementation of `IProcessLock`. Use the SqlServer implementation for reference - you can find it in the `BitPantry.ProcessLock.Implementation.SqlServer` namespace. You can see how this implementation is configured by looking at the `BitPantry.ProcessLock.Implementation.SqlServer.ProcessLockConfigurationExtensions` class.

## Implement `IProcessLock`
This is pretty self-explanatory - you need to create an implementation of `IProcessLock` that will be used to create, renew, and release locks.
