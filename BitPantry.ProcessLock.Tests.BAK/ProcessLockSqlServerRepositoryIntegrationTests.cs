using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using Xunit;
using BitPantry.ProcessLock;
using BitPantry.ProcessLock.Implementation.Database;
using FluentAssertions;
using System.Threading.Tasks;
using System.Data.SqlClient;
using Microsoft.Data.Sqlite;

namespace BitPantry.ProcessLock.Tests
{

    public class ProcessLockSqlServerRepositoryIntegrationTests : IntegrationTestBase
    {

        public ProcessLockSqlServerRepositoryIntegrationTests() { }

        [SkippableTheory]
        [InlineData(IntegrationTestServerType.Sqlite)]
        [InlineData(IntegrationTestServerType.SqlServer)]
        public async Task ResetDatabase_TableNotExists(IntegrationTestServerType serverType)
        {
            CheckIfSkipped(serverType);

            using (var scope = CreateScope(serverType))
            {
                var pl = scope.ServiceProvider.GetRequiredService<DatabaseProcessLockRepository>();

                await pl.ResetDatabase();
                (await pl.DoesTableExist()).Should().BeFalse();
            }
        }

        [SkippableTheory]
        [InlineData(IntegrationTestServerType.Sqlite)]
        [InlineData(IntegrationTestServerType.SqlServer)]
        public async Task CreateTable_TableExists(IntegrationTestServerType serverType)
        {
            CheckIfSkipped(serverType);

            using (var scope = CreateScope(serverType))
            {
                var pl = scope.ServiceProvider.GetRequiredService<DatabaseProcessLockRepository>();

                await pl.ResetDatabase();

                (await pl.DoesTableExist()).Should().BeFalse();
                await pl.CreateTable();
                (await pl.DoesTableExist()).Should().BeTrue();
            }
        }

        [SkippableTheory]
        [InlineData(IntegrationTestServerType.Sqlite)]
        [InlineData(IntegrationTestServerType.SqlServer)]
        public async Task ResetCreatedDatabase_TableNotExists(IntegrationTestServerType serverType)
        {
            CheckIfSkipped(serverType);

            using (var scope = CreateScope(serverType))
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

        [SkippableTheory]
        [InlineData(IntegrationTestServerType.Sqlite, false)]
        [InlineData(IntegrationTestServerType.SqlServer, false)]
        [InlineData(IntegrationTestServerType.Sqlite, true)]
        [InlineData(IntegrationTestServerType.SqlServer, true)]
        public async Task CreateLock_RecordCreated(IntegrationTestServerType serverType, bool readByToken)
        {
            CheckIfSkipped(serverType);

            using (var scope = CreateScope(serverType))
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

                var persistedRecord = readByToken ? await pl.ReadByToken(token) : await pl.ReadByProcessName(processName);

                persistedRecord.ProcessName.Should().Be(processName);
                persistedRecord.Token.Should().Be(token);
                persistedRecord.ExpiresOn.ToString("yyyy-MM-dd HH:mm:ss").Should().Be(expiresOn.ToString("yyyy-MM-dd HH:mm:ss"));
                persistedRecord.LockDuration.Should().Be(lockDuration);
            }
        }

        [SkippableTheory]
        [InlineData(IntegrationTestServerType.Sqlite, false)]
        [InlineData(IntegrationTestServerType.SqlServer, false)]
        [InlineData(IntegrationTestServerType.Sqlite, true)]
        [InlineData(IntegrationTestServerType.SqlServer, true)]
        public async Task UpdateLock_RecordUpdated(IntegrationTestServerType serverType, bool readByToken)
        {
            CheckIfSkipped(serverType);

            using (var scope = CreateScope(serverType))
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

                var persistedRecord = readByToken ? await pl.ReadByToken(token) : await pl.ReadByProcessName(processName);

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

        [SkippableTheory]
        [InlineData(IntegrationTestServerType.Sqlite, false)]
        [InlineData(IntegrationTestServerType.SqlServer, false)]
        [InlineData(IntegrationTestServerType.Sqlite, true)]
        [InlineData(IntegrationTestServerType.SqlServer, true)]
        public async Task DeleteLock_RecordDeleted(IntegrationTestServerType serverType, bool readByToken)
        {
            CheckIfSkipped(serverType);

            using (var scope = CreateScope(serverType))
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

                var persistedRecord = readByToken ? await pl.ReadByToken(token) : await pl.ReadByProcessName(processName);

                persistedRecord.ProcessName.Should().Be(processName);
                persistedRecord.Token.Should().Be(token);
                persistedRecord.ExpiresOn.ToString("yyyy-MM-dd HH:mm:ss").Should().Be(expiresOn.ToString("yyyy-MM-dd HH:mm:ss"));
                persistedRecord.LockDuration.Should().Be(lockDuration);

                await pl.Delete(token);

                persistedRecord = readByToken ? await pl.ReadByToken(token) : await pl.ReadByProcessName(processName);

                persistedRecord.Should().BeNull();
            }
        }

        [SkippableTheory]
        [InlineData(IntegrationTestServerType.Sqlite)]
        [InlineData(IntegrationTestServerType.SqlServer)]
        public async Task ViolateKeyConstraint_ConstraintViolated(IntegrationTestServerType serverType)
        {
            CheckIfSkipped(serverType);

            using (var scope = CreateScope(serverType))
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
}
