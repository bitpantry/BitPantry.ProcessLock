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

I've provided an extension function for the `IServiceCollection`. Take a peak at the `BitPantry.ProcessLock.Tests` project for a full setup example and tests of the `IProcessLock` implementations.

### For SQL Server
```
var services = new ServiceCollection();

services.ConfigureProcessLocks(opt =>
{
    opt.UseRelationalDatabase()
        .WithSqlServer("...a connection string goes here..."))
});
```

### For Sqlite
```
var services = new ServiceCollection();

services.ConfigureProcessLocks(opt =>
{
    opt.UseRelationalDatabase()
        .WithSqlite("...a connection string goes here..."))
});
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
```

For each of the three functions, there are only three unique parameters.

- `processName` is the name of the process - all instances of the process should use the same name
- `lockDuration` is the lifetime of the lock in milliseconds - once the lock is expired, a lock with the same process name can be created
- `minRenewalInterval` is only used when renewing a lock and helps prevent *abusing* the underlying synchronization mechanism (in the default case, the database) by actually renewing a lock each time `Renew` is called - instead it only actually renews a lock when the lock expiration time minus the `minRenewalInterval` is less than now

# Example
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

        if(await _pl.Create(_processName, 15000)) 
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
                    
                    await _pl.Renew(_processName, 15000, 7000); 
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

Right now, however, this project uses a centralized relational database that can be accessed by all instances of a process as the synchronization mechanism.

There are implementations for both **SQL Server** and **Sqlite**. 

- **SQL Server** is my typical production relational database, so I included that one as default
- As far as the ProcessLock project functionality is concerned, **Sqlite** works just like SQL Server and is great for limited integration testing (on a build server like AppVeyor, for example) - it also requres no additional processes or dependencies (all in memory and file system based)

Using a relational database, we leverage a key constraint to ensure that only one instance of the process can ever successfuly enter a lock record into the database at once.

# Extending
If you would like to add a new implementation around a different locking mechanism here's how.

## Implement `IProcessLock`
This is pretty self-explanatory - you need to create an implementation of `IProcessLock` that will be used to create, renew, and release locks.
## Implement `IProcessLockServiceConfigurator`
This will be used to 'wire up' your implementation at run-time. It accepts an `IServiceCollection` and the `ProcessLockOptions` object. Here's an example from the Database implementation.

The Database implementation wires up a `DatabaseProcessLock` implementation for the `IProcessLock` interface. This is technically all that is required to create a new implementation. However, the `DatabaseProcessLock` has some other dependencies that also need to be configured, including a `DatabaseProcessLockRepository` and an `IDatabaseProcessLockContext`.

You can see that for the given server type (SQL Server or Sqlite) it can configure the specific `IDatabaseProcessLockContext` implementation. 

```
internal class DatabaseProcessLockServiceConfigurator : IProcessLockServiceConfigurator
{
    public void Configure(IServiceCollection services, ProcessLockOptions options)
    {
        services.AddTransient<IProcessLock, DatabaseProcessLock>();
        services.AddTransient<DatabaseProcessLockRepository>();

        switch (options.DatabaseProcessLockOptions.ServerType)
        {
            case DatabaseProcessLockServerType.SqlServer:
                services.AddTransient<IDatabaseProcessLockContext>(svc => 
                    new SqlServerProcessLockContext(
                        options.DatabaseProcessLockOptions.ConnectionString, 
                        options.DatabaseProcessLockOptions.DoUseUniqueTableNameSuffix));
                break;
            case DatabaseProcessLockServerType.Sqlite:
                services.AddTransient<IDatabaseProcessLockContext>(svc => 
                    new SqliteProcessLockContext(
                        options.DatabaseProcessLockOptions.ConnectionString, 
                        options.DatabaseProcessLockOptions.DoUseUniqueTableNameSuffix));
                break;
            default:
                throw new ArgumentOutOfRangeException(
                    $"{options.DatabaseProcessLockOptions.ServerType} is not defined for this switch");
        }
    }
}
```
## Add a new value to the `ProcessLockImplementation` enumeration
Next, you'll need to add a new value to the `ProcessLockImplementation` enumeration to identify your implementation. 

```
public enum ProcessLockImplementation
{
    Database,
    NewImpl // enum value for my new implementation
}
```
## Add a map to the `ServiceCollectionExtensions:ConfiguratorDict`
Add a new map to the `ServiceCollectionExtensions.ConfiguratorDict` from your new `ProcessLockImplementation` enum to an instance of your implementation of `IProcessLockServiceConfigurator`. Here's an example.

```
private static Dictionary<ProcessLockImplementation, IProcessLockServiceConfigurator> ConfiguratorDict 
    = new Dictionary<ProcessLockImplementation, IProcessLockServiceConfigurator>()
{
    { ProcessLockImplementation.Database, new DatabaseProcessLockServiceConfigurator() },

    // NEW MAPPING FOR MYIMPL
    { ProcessLockImplementation.MyImpl, new MyImplProcessLockServiceConfigurator() }
};
```

## Add Configuration
Update the `ProcessLockOptions` object to allow for the configuration of your new implementation. Here's how the Database implementation has been added.

```
public class ProcessLockOptions
{
    internal ProcessLockImplementation Implementation { get; private set; }
    internal RelationalDatabaseProcessLockOptions DatabaseProcessLockOptions { get; set; }

    internal ProcessLockOptions() { }

    /// <summary>
    /// Configures process locks to use a relational database as the distributed locking mechanism
    /// </summary>
    /// <returns>The relational database options</returns>
    public RelationalDatabaseProcessLockOptions UseRelationalDatabase() 
    {
        Implementation = ProcessLockImplementation.Database;
        DatabaseProcessLockOptions = new RelationalDatabaseProcessLockOptions();

        return DatabaseProcessLockOptions;
    }
}
```

Notice `UseRelationalDatabase` returns a `RelationalDatabaseProcessLockOptions` object that allows for the continued configuration of the Database implementation. Create your own options object for additional configuration.

The configured `ProcessLockOptions` object will be passed to your implementation of the `IProcessLockServiceConfigurator` during startup.

## Organization
Currently, implementations of `IProcessLock` are organized within the `BitPantry.ProcessLock.Implementation` namespace. Currently, there is only the one *Database* implementation.