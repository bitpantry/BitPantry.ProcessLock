using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using FluentAssertions;
using BitPantry.ProcessLock.Implementation.SqlServer;

namespace BitPantry.ProcessLock.Tests;

[TestClass]
public class ProcessLockSqlServerRepositoryIntegrationTests : IntegrationTestBase
{

    [TestMethod]
    public async Task ResetDatabase_TableNotExists()
    {
        using (var scope = ServiceProvider.CreateScope())
        {
            var pl = scope.ServiceProvider.GetRequiredService<DatabaseProcessLockRepository>();

            await pl.ResetDatabase();
            (await pl.DoesTableExist()).Should().BeFalse();
        }
    }

    [TestMethod]
    public async Task CreateTable_TableExists()
    {
        using (var scope = ServiceProvider.CreateScope())
        {
            var pl = scope.ServiceProvider.GetRequiredService<DatabaseProcessLockRepository>();

            await pl.ResetDatabase();

            (await pl.DoesTableExist()).Should().BeFalse();
            await pl.CreateTable();
            (await pl.DoesTableExist()).Should().BeTrue();
        }
    }

    [TestMethod]
    public async Task ResetCreatedDatabase_TableNotExists()
    {
        using (var scope = ServiceProvider.CreateScope())
        {
            var pl = scope.ServiceProvider.GetRequiredService<DatabaseProcessLockRepository>();

            await pl.ResetDatabase();

            (await pl.DoesTableExist()).Should().BeFalse();
            await pl.CreateTable();
            (await pl.DoesTableExist()).Should().BeTrue();
            await pl.ResetDatabase();
            (await pl.DoesTableExist()).Should().BeFalse();
        }
    }

    [TestMethod]
    public async Task CreateLockReadByToken_RecordCreated()
    {
        using (var scope = ServiceProvider.CreateScope())
        {
            var pl = scope.ServiceProvider.GetRequiredService<DatabaseProcessLockRepository>();

            var processName = Guid.NewGuid().ToString();
            var token = Guid.NewGuid().ToString();
            var expiresOn = DateTime.UtcNow.AddMinutes(15);
            var lockDuration = (int)TimeSpan.FromMinutes(15).TotalMilliseconds;

            await pl.Create(new DatabaseProcessLockRecord
            {
                ProcessName = processName,
                Token = token,
                ExpiresOn = expiresOn,
                LockDuration = lockDuration
            });

            var persistedRecord = await pl.ReadByToken(token);

            persistedRecord.ProcessName.Should().Be(processName);
            persistedRecord.Token.Should().Be(token);
            persistedRecord.ExpiresOn.ToString("yyyy-MM-dd HH:mm:ss").Should().Be(expiresOn.ToString("yyyy-MM-dd HH:mm:ss"));
            persistedRecord.LockDuration.Should().Be(lockDuration);
        }
    }

    [TestMethod]
    public async Task CreateLockReadByProcessName_RecordCreated()
    {
        using (var scope = ServiceProvider.CreateScope())
        {
            var pl = scope.ServiceProvider.GetRequiredService<DatabaseProcessLockRepository>();

            var processName = Guid.NewGuid().ToString();
            var token = Guid.NewGuid().ToString();
            var expiresOn = DateTime.UtcNow.AddMinutes(15);
            var lockDuration = (int)TimeSpan.FromMinutes(15).TotalMilliseconds;

            await pl.Create(new DatabaseProcessLockRecord
            {
                ProcessName = processName,
                Token = token,
                ExpiresOn = expiresOn,
                LockDuration = lockDuration
            });

            var persistedRecord = await pl.ReadByProcessName(processName);

            persistedRecord.ProcessName.Should().Be(processName);
            persistedRecord.Token.Should().Be(token);
            persistedRecord.ExpiresOn.ToString("yyyy-MM-dd HH:mm:ss").Should().Be(expiresOn.ToString("yyyy-MM-dd HH:mm:ss"));
            persistedRecord.LockDuration.Should().Be(lockDuration);
        }
    }

    [TestMethod]
    public async Task UpdateLockReadByToken_RecordUpdated()
    {
        using (var scope = ServiceProvider.CreateScope())
        {
            var pl = scope.ServiceProvider.GetRequiredService<DatabaseProcessLockRepository>();

            var processName = Guid.NewGuid().ToString();
            var token = Guid.NewGuid().ToString();
            var expiresOn = DateTime.UtcNow.AddMinutes(15);
            var lockDuration = (int)TimeSpan.FromMinutes(15).TotalMilliseconds;

            // create

            var record = new DatabaseProcessLockRecord
            {
                ProcessName = processName,
                Token = token,
                ExpiresOn = expiresOn,
                LockDuration = lockDuration
            };

            await pl.Create(record);

            var persistedRecord = await pl.ReadByToken(token);

            persistedRecord.ProcessName.Should().Be(processName);
            persistedRecord.Token.Should().Be(token);
            persistedRecord.ExpiresOn.ToString("yyyy-MM-dd HH:mm:ss").Should().Be(expiresOn.ToString("yyyy-MM-dd HH:mm:ss"));
            persistedRecord.LockDuration.Should().Be(lockDuration);

            // update

            var processName2 = Guid.NewGuid().ToString();
            var token2 = Guid.NewGuid().ToString();
            var expiresOn2 = DateTime.UtcNow.AddMinutes(10);
            var lockDuration2 = (int)TimeSpan.FromMinutes(10).TotalMilliseconds;

            persistedRecord.ProcessName = processName2;
            persistedRecord.ExpiresOn = expiresOn2;
            persistedRecord.LockDuration = lockDuration2;

            await pl.Update(persistedRecord);
            persistedRecord = await pl.ReadByToken(token);

            persistedRecord.ProcessName.Should().Be(processName);
            persistedRecord.Token.Should().Be(token);
            persistedRecord.ExpiresOn.ToString("yyyy-MM-dd HH:mm:ss").Should().Be(expiresOn2.ToString("yyyy-MM-dd HH:mm:ss"));
            persistedRecord.LockDuration.Should().Be(lockDuration2);
        }
    }

    [TestMethod]
    public async Task UpdateLockReadByProcessName_RecordUpdated()
    {
        using (var scope = ServiceProvider.CreateScope())
        {
            var pl = scope.ServiceProvider.GetRequiredService<DatabaseProcessLockRepository>();

            var processName = Guid.NewGuid().ToString();
            var token = Guid.NewGuid().ToString();
            var expiresOn = DateTime.UtcNow.AddMinutes(15);
            var lockDuration = (int)TimeSpan.FromMinutes(15).TotalMilliseconds;

            // create

            var record = new DatabaseProcessLockRecord
            {
                ProcessName = processName,
                Token = token,
                ExpiresOn = expiresOn,
                LockDuration = lockDuration
            };

            await pl.Create(record);

            var persistedRecord = await pl.ReadByProcessName(processName);

            persistedRecord.ProcessName.Should().Be(processName);
            persistedRecord.Token.Should().Be(token);
            persistedRecord.ExpiresOn.ToString("yyyy-MM-dd HH:mm:ss").Should().Be(expiresOn.ToString("yyyy-MM-dd HH:mm:ss"));
            persistedRecord.LockDuration.Should().Be(lockDuration);

            // update

            var processName2 = Guid.NewGuid().ToString();
            var token2 = Guid.NewGuid().ToString();
            var expiresOn2 = DateTime.UtcNow.AddMinutes(10);
            var lockDuration2 = (int)TimeSpan.FromMinutes(10).TotalMilliseconds;

            persistedRecord.ProcessName = processName2;
            persistedRecord.ExpiresOn = expiresOn2;
            persistedRecord.LockDuration = lockDuration2;

            await pl.Update(persistedRecord);
            persistedRecord = await pl.ReadByToken(token);

            persistedRecord.ProcessName.Should().Be(processName);
            persistedRecord.Token.Should().Be(token);
            persistedRecord.ExpiresOn.ToString("yyyy-MM-dd HH:mm:ss").Should().Be(expiresOn2.ToString("yyyy-MM-dd HH:mm:ss"));
            persistedRecord.LockDuration.Should().Be(lockDuration2);
        }
    }

    [TestMethod]
    public async Task DeleteLockReadByToken_RecordDeleted()
    {
        using (var scope = ServiceProvider.CreateScope())
        {
            var pl = scope.ServiceProvider.GetRequiredService<DatabaseProcessLockRepository>();

            var processName = Guid.NewGuid().ToString();
            var token = Guid.NewGuid().ToString();
            var expiresOn = DateTime.UtcNow.AddMinutes(15);
            var lockDuration = (int)TimeSpan.FromMinutes(15).TotalMilliseconds;

            await pl.Create(new DatabaseProcessLockRecord
            {
                ProcessName = processName,
                Token = token,
                ExpiresOn = expiresOn,
                LockDuration = lockDuration
            });

            var persistedRecord = await pl.ReadByToken(token);

            persistedRecord.ProcessName.Should().Be(processName);
            persistedRecord.Token.Should().Be(token);
            persistedRecord.ExpiresOn.ToString("yyyy-MM-dd HH:mm:ss").Should().Be(expiresOn.ToString("yyyy-MM-dd HH:mm:ss"));
            persistedRecord.LockDuration.Should().Be(lockDuration);

            await pl.Delete(token);

            persistedRecord = await pl.ReadByToken(token);

            persistedRecord.Should().BeNull();
        }
    }

    [TestMethod]
    public async Task DeleteLockReadByProcessName_RecordDeleted()
    {
        using (var scope = ServiceProvider.CreateScope())
        {
            var pl = scope.ServiceProvider.GetRequiredService<DatabaseProcessLockRepository>();

            var processName = Guid.NewGuid().ToString();
            var token = Guid.NewGuid().ToString();
            var expiresOn = DateTime.UtcNow.AddMinutes(15);
            var lockDuration = (int)TimeSpan.FromMinutes(15).TotalMilliseconds;

            await pl.Create(new DatabaseProcessLockRecord
            {
                ProcessName = processName,
                Token = token,
                ExpiresOn = expiresOn,
                LockDuration = lockDuration
            });

            var persistedRecord = await pl.ReadByProcessName(processName);

            persistedRecord.ProcessName.Should().Be(processName);
            persistedRecord.Token.Should().Be(token);
            persistedRecord.ExpiresOn.ToString("yyyy-MM-dd HH:mm:ss").Should().Be(expiresOn.ToString("yyyy-MM-dd HH:mm:ss"));
            persistedRecord.LockDuration.Should().Be(lockDuration);

            await pl.Delete(token);

            persistedRecord = await pl.ReadByProcessName(processName);

            persistedRecord.Should().BeNull();
        }
    }

    [TestMethod]
    public async Task ViolateKeyConstraint_ConstraintViolated()
    {
        using (var scope = ServiceProvider.CreateScope())
        {
            var pl = scope.ServiceProvider.GetRequiredService<DatabaseProcessLockRepository>();

            var record = new DatabaseProcessLockRecord
            {
                ProcessName = Guid.NewGuid().ToString(),
                Token = Guid.NewGuid().ToString(),
                ExpiresOn = DateTime.UtcNow.AddMinutes(15),
                LockDuration = (int)TimeSpan.FromMinutes(15).TotalMilliseconds
            };

            await pl.Create(record);

            try
            {
                await pl.Create(record);
            }
            catch (Exception ex)
            {
                pl.IsUniqueKeyViolatedException(ex).Should().BeTrue();
            }
        }
    }

}

