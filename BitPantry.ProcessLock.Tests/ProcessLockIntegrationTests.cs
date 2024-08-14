using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using FluentAssertions;

namespace BitPantry.ProcessLock.Tests;

[TestClass]
public class ProcessLockIntegrationTests : IntegrationTestBase
{

    [TestMethod]
    public async Task CreateLock_LockCreated()
    {
        using (var scope = ServiceProvider.CreateScope())
        {
            var svc = scope.ServiceProvider.GetRequiredService<IProcessLock>();

            var processName = Guid.NewGuid().ToString();

            (await svc.Create(processName, 500)).Should().NotBeNull();
        }
    }

    [TestMethod]
    public async Task CreateLockOverExpiredLock_LockCreated()
    {
        using (var scope = ServiceProvider.CreateScope())
        {
            var svc = scope.ServiceProvider.GetRequiredService<IProcessLock>();

            var processName = Guid.NewGuid().ToString();

            (await svc.Create(processName, 250)).Should().NotBeNull();

            await Task.Delay(300);

            (await svc.Create(processName, 250)).Should().NotBeNull();
        }
    }

    [TestMethod]
    public async Task CreateLockOverLock_LockDenied()
    {
        using (var scope = ServiceProvider.CreateScope())
        {
            var svc = scope.ServiceProvider.GetRequiredService<IProcessLock>();

            var processName = Guid.NewGuid().ToString();

            (await svc.Create(processName, 250)).Should().NotBeNull();

            (await svc.Create(processName, 250)).Should().BeNull();
        }
    }

    [TestMethod]
    public async Task RenewLock_LockRenewed()
    {
        using (var scope = ServiceProvider.CreateScope())
        {
            var svc = scope.ServiceProvider.GetRequiredService<IProcessLock>();

            var processName = Guid.NewGuid().ToString();

            var token = await svc.Create(processName, 500);
            token.Should().NotBeNull();

            (await svc.Renew(token, 500, 500)).Should().BeTrue();
        }
    }

    [TestMethod]
    public async Task RenewLockEarly_LockNotRenewed()
    {
        using (var scope = ServiceProvider.CreateScope())
        {
            var svc = scope.ServiceProvider.GetRequiredService<IProcessLock>();

            var processName = Guid.NewGuid().ToString();

            var token = await svc.Create(processName, 500);
            token.Should().NotBeNull();

            (await svc.Renew(processName, 500, 250)).Should().BeFalse();
        }
    }

    [TestMethod]
    public async Task RenewLock_CannotCreateNewLock()
    {
        using (var scope = ServiceProvider.CreateScope())
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

    [TestMethod]
    public async Task CreateOverExpiredRenewedLock_LockCreated()
    {
        using (var scope = ServiceProvider.CreateScope())
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

    [TestMethod]
    public async Task CheckLockExists_Exists()
    {
        using (var scope = ServiceProvider.CreateScope())
        {
            var svc = scope.ServiceProvider.GetRequiredService<IProcessLock>();

            var processName = Guid.NewGuid().ToString();

            var token = await svc.Create(processName, 60000);
            token.Should().NotBeNull();

            (await svc.Exists(processName)).Should().BeTrue();
        }
    }

    [TestMethod]
    public async Task CheckExpiredLockExists_NotExist()
    {
        using (var scope = ServiceProvider.CreateScope())
        {
            var svc = scope.ServiceProvider.GetRequiredService<IProcessLock>();

            var processName = Guid.NewGuid().ToString();

            var token = await svc.Create(processName, 50);
            token.Should().NotBeNull();

            await Task.Delay(100);

            (await svc.Exists(processName)).Should().BeFalse();
        }
    }

    [TestMethod]
    public async Task CheckLockExists_NotExists()
    {
        using (var scope = ServiceProvider.CreateScope())
        {
            var svc = scope.ServiceProvider.GetRequiredService<IProcessLock>();

            var processName = Guid.NewGuid().ToString();

            (await svc.Exists(processName)).Should().BeFalse();
        }
    }
}