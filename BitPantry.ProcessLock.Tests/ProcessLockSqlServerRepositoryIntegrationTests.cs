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
        [InlineData(IntegrationTestServerType.Sqlite)]
        [InlineData(IntegrationTestServerType.SqlServer)]
        public async Task CreateLock_RecordCreated(IntegrationTestServerType serverType)
        {
            CheckIfSkipped(serverType);

            using (var scope = CreateScope(serverType))
            {
                var pl = scope.ServiceProvider.GetRequiredService<DatabaseProcessLockRepository>();

                var processId = Guid.NewGuid().ToString();
                var expiresOn = DateTime.Now.AddMinutes(15);

                await pl.Create(new DatabaseProcessLockRecord
                {
                    Id = processId,
                    HostName = Environment.MachineName,
                    ExpiresOn = expiresOn
                });

                var persistedRecord = await pl.Read(processId);

                persistedRecord.Id.Should().Be(processId);
                persistedRecord.HostName.Should().Be(Environment.MachineName);
                persistedRecord.ExpiresOn.ToString("yyyy-MM-dd HH:mm:ss").Should().Be(expiresOn.ToString("yyyy-MM-dd HH:mm:ss"));
            }
        }

        [SkippableTheory]
        [InlineData(IntegrationTestServerType.Sqlite)]
        [InlineData(IntegrationTestServerType.SqlServer)]
        public async Task DeleteLock_RecordDeleted(IntegrationTestServerType serverType)
        {
            CheckIfSkipped(serverType);

            using (var scope = CreateScope(serverType))
            {
                var pl = scope.ServiceProvider.GetRequiredService<DatabaseProcessLockRepository>();

                var processId = Guid.NewGuid().ToString();
                var expiresOn = DateTime.Now.AddMinutes(15);

                await pl.Create(new DatabaseProcessLockRecord
                {
                    Id = processId,
                    HostName = Environment.MachineName,
                    ExpiresOn = expiresOn
                });

                var persistedRecord = await pl.Read(processId);

                persistedRecord.Id.Should().Be(processId);
                persistedRecord.HostName.Should().Be(Environment.MachineName);
                persistedRecord.ExpiresOn.ToString("yyyy-MM-dd HH:mm:ss").Should().Be(expiresOn.ToString("yyyy-MM-dd HH:mm:ss"));

                await pl.Delete(processId);

                persistedRecord = await pl.Read(processId);

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
                    Id = Guid.NewGuid().ToString(),
                    HostName = Environment.MachineName,
                    ExpiresOn = DateTime.Now.AddMinutes(15)
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
