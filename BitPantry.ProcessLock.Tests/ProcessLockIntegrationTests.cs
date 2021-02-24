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

                var processName = Guid.NewGuid().ToString();

                (await svc.Create(processName, 500)).Should().NotBeNull();
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

                var processName = Guid.NewGuid().ToString();

                (await svc.Create(processName, 250)).Should().NotBeNull();

                await Task.Delay(300);

                (await svc.Create(processName, 250)).Should().NotBeNull();
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

                var processName = Guid.NewGuid().ToString();

                (await svc.Create(processName, 250)).Should().NotBeNull();

                (await svc.Create(processName, 250)).Should().BeNull();
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

                var processName = Guid.NewGuid().ToString();

                var token = await svc.Create(processName, 500);
                token.Should().NotBeNull();

                (await svc.Renew(token, 500, 500)).Should().BeTrue();
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

                var processName = Guid.NewGuid().ToString();

                var token = await svc.Create(processName, 500);
                token.Should().NotBeNull();

                (await svc.Renew(processName, 500, 250)).Should().BeFalse();
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

                var processName = Guid.NewGuid().ToString();

                var token = await svc.Create(processName, 500);
                token.Should().NotBeNull();

                await Task.Delay(300);

                (await svc.Renew(token, 500, 400)).Should().BeTrue();

                await Task.Delay(200);

                var newToken = await svc.Create(processName, 500);
                newToken.Should().BeNull();
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

                var processName = Guid.NewGuid().ToString();

                var token = await svc.Create(processName, 250);
                token.Should().NotBeNull();

                await Task.Delay(100);

                (await svc.Renew(token, 250, 250)).Should().BeTrue();

                await Task.Delay(500);

                var newToken = await svc.Create(processName, 500);
                newToken.Should().NotBeNull();
            }
        }

        [SkippableTheory]
        [InlineData(IntegrationTestServerType.Sqlite)]
        [InlineData(IntegrationTestServerType.SqlServer)]
        public async Task CheckLockExists_Exists(IntegrationTestServerType serverType)
        {
            CheckIfSkipped(serverType);

            using (var scope = CreateScope(serverType))
            {
                var svc = scope.ServiceProvider.GetRequiredService<IProcessLock>();

                var processName = Guid.NewGuid().ToString();

                var token = await svc.Create(processName, 60000);
                token.Should().NotBeNull();

                (await svc.Exists(processName)).Should().BeTrue();
            }
        }

        [SkippableTheory]
        [InlineData(IntegrationTestServerType.Sqlite)]
        [InlineData(IntegrationTestServerType.SqlServer)]
        public async Task CheckExpiredLockExists_NotExist(IntegrationTestServerType serverType)
        {
            CheckIfSkipped(serverType);

            using (var scope = CreateScope(serverType))
            {
                var svc = scope.ServiceProvider.GetRequiredService<IProcessLock>();

                var processName = Guid.NewGuid().ToString();

                var token = await svc.Create(processName, 50);
                token.Should().NotBeNull();

                await Task.Delay(100);

                (await svc.Exists(processName)).Should().BeFalse();
            }
        }

        [SkippableTheory]
        [InlineData(IntegrationTestServerType.Sqlite)]
        [InlineData(IntegrationTestServerType.SqlServer)]
        public async Task CheckLockExists_NotExists(IntegrationTestServerType serverType)
        {
            CheckIfSkipped(serverType);

            using (var scope = CreateScope(serverType))
            {
                var svc = scope.ServiceProvider.GetRequiredService<IProcessLock>();

                var processName = Guid.NewGuid().ToString();

                (await svc.Exists(processName)).Should().BeFalse();
            }
        }
    }
}
