using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using FluentAssertions;
using System;

namespace BitPantry.ProcessLock.Tests;

[TestClass]
public class ProcessLockScopeTests : IntegrationTestBase
{

    [TestMethod]
    public async Task CreateScope_ScopeCreated()
    {
        using (var scope = ServiceProvider.CreateScope())
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

    [TestMethod]
    public async Task CreateScope_ExistingLock_NoLock()
    {
        using (var scope = ServiceProvider.CreateScope())
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

