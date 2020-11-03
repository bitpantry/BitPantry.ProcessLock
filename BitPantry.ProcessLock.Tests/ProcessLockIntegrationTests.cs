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
    public class ProcessLockIntegrationTests : IntegrationTestBase
    {
        public ProcessLockIntegrationTests() { }

        [SkippableTheory]
        [InlineData(IntegrationTestServerType.Sqlite)]
        [InlineData(IntegrationTestServerType.SqlServer)]
        public async Task CreateLock_LockCreated(IntegrationTestServerType serverType)
        {
            CheckIfSkipped(serverType);

            using (var scope = CreateScope(serverType))
            {
                var svc = scope.ServiceProvider.GetRequiredService<IProcessLock>();

                var processId = Guid.NewGuid().ToString();

               (await svc.Create(processId, 500)).Should().BeTrue();
            }
        }

        [SkippableTheory]
        [InlineData(IntegrationTestServerType.Sqlite)]
        [InlineData(IntegrationTestServerType.SqlServer)]
        public async Task CreateLockOverExpiredLock_LockCreated(IntegrationTestServerType serverType)
        {
            CheckIfSkipped(serverType);

            using (var scope = CreateScope(serverType))
            {
                var svc = scope.ServiceProvider.GetRequiredService<IProcessLock>();

                var processId = Guid.NewGuid().ToString();

                (await svc.Create(processId, 250)).Should().BeTrue();

                await Task.Delay(250);

                (await svc.Create(processId, 250)).Should().BeTrue();
            }
        }

        [SkippableTheory]
        [InlineData(IntegrationTestServerType.Sqlite)]
        [InlineData(IntegrationTestServerType.SqlServer)]
        public async Task CreateLockOverLock_LockDenied(IntegrationTestServerType serverType)
        {
            CheckIfSkipped(serverType);

            using (var scope = CreateScope(serverType))
            {
                var svc = scope.ServiceProvider.GetRequiredService<IProcessLock>();

                var processId = Guid.NewGuid().ToString();

                (await svc.Create(processId, 250)).Should().BeTrue();

                (await svc.Create(processId, 250)).Should().BeFalse();
            }
        }

        [SkippableTheory]
        [InlineData(IntegrationTestServerType.Sqlite)]
        [InlineData(IntegrationTestServerType.SqlServer)]
        public async Task RenewLock_LockRenewed(IntegrationTestServerType serverType)
        {
            CheckIfSkipped(serverType);

            using (var scope = CreateScope(serverType))
            {
                var svc = scope.ServiceProvider.GetRequiredService<IProcessLock>();

                var processId = Guid.NewGuid().ToString();

                (await svc.Create(processId, 500)).Should().BeTrue();

                (await svc.Renew(processId, 500, 500)).Should().BeTrue();
            }
        }

        [SkippableTheory]
        [InlineData(IntegrationTestServerType.Sqlite)]
        [InlineData(IntegrationTestServerType.SqlServer)]
        public async Task RenewLockEarly_LockNotRenewed(IntegrationTestServerType serverType)
        {
            CheckIfSkipped(serverType);

            using (var scope = CreateScope(serverType))
            {
                var svc = scope.ServiceProvider.GetRequiredService<IProcessLock>();

                var processId = Guid.NewGuid().ToString();

                (await svc.Create(processId, 500)).Should().BeTrue();

                (await svc.Renew(processId, 500, 250)).Should().BeFalse();
            }
        }

        [SkippableTheory]
        [InlineData(IntegrationTestServerType.Sqlite)]
        [InlineData(IntegrationTestServerType.SqlServer)]
        public async Task RenewLock_CannotCreateNewLock(IntegrationTestServerType serverType)
        {
            CheckIfSkipped(serverType);

            using (var scope = CreateScope(serverType))
            {
                var svc = scope.ServiceProvider.GetRequiredService<IProcessLock>();

                var processId = Guid.NewGuid().ToString();

                (await svc.Create(processId, 500)).Should().BeTrue();

                await Task.Delay(300);

                (await svc.Renew(processId, 500, 400)).Should().BeTrue();

                await Task.Delay(200);

                (await svc.Create(processId, 500)).Should().BeFalse();
            }
        }

        [SkippableTheory]
        [InlineData(IntegrationTestServerType.Sqlite)]
        [InlineData(IntegrationTestServerType.SqlServer)]
        public async Task CreateOverExpiredRenewedLock_LockCreated(IntegrationTestServerType serverType)
        {
            CheckIfSkipped(serverType);

            using (var scope = CreateScope(serverType))
            {
                var svc = scope.ServiceProvider.GetRequiredService<IProcessLock>();

                var processId = Guid.NewGuid().ToString();

                (await svc.Create(processId, 250)).Should().BeTrue();

                await Task.Delay(100);

                (await svc.Renew(processId, 250, 250)).Should().BeTrue();

                await Task.Delay(500);

                (await svc.Create(processId, 500)).Should().BeTrue();
            }
        }
    }
}
