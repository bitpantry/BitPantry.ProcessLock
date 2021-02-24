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
    public class ProcessLockScopeTests : IntegrationTestBase
    {
        public ProcessLockScopeTests() { }

        [SkippableTheory]
        [InlineData(IntegrationTestServerType.Sqlite)]
        [InlineData(IntegrationTestServerType.SqlServer)]
        public async Task CreateScope_ScopeCreated(IntegrationTestServerType serverType)
        {
            CheckIfSkipped(serverType);

            using (var scope = CreateScope(serverType))
            {
                var svc = scope.ServiceProvider.GetRequiredService<IProcessLock>();
                
                var processName = Guid.NewGuid().ToString();

                ProcessLockScope pls = null;

                (await svc.Exists(processName)).Should().BeFalse();

                using (pls = svc.BeginScope(processName))
                {
                    pls.Token.Should().NotBeNull();
                    pls.IsLocked.Should().BeTrue();

                    (await svc.Exists(processName)).Should().BeTrue();
                }

                (await svc.Exists(processName)).Should().BeFalse();
            }
        }

        [SkippableTheory]
        [InlineData(IntegrationTestServerType.Sqlite)]
        [InlineData(IntegrationTestServerType.SqlServer)]
        public async Task CreateScope_ExistingLock_NoLock(IntegrationTestServerType serverType)
        {
            CheckIfSkipped(serverType);

            using (var scope = CreateScope(serverType))
            {
                var svc = scope.ServiceProvider.GetRequiredService<IProcessLock>();

                var processName = Guid.NewGuid().ToString();
                await svc.Create(processName, 10000);

                using (var pls = svc.BeginScope(processName))
                {
                    pls.Token.Should().BeNull();
                    pls.IsLocked.Should().BeFalse();
                }
            }
        }
    }
}
