using InventoryPilot.Services;
using Microsoft.Extensions.Configuration;

namespace InventoryPilot.Tests;

public class InventoryApiTokenServiceTests
{
    [Fact]
    public void GenerateToken_CanBeReadBackWithSameSecret()
    {
        var service = CreateService("test-secret");

        var token = service.GenerateToken(42);

        Assert.True(service.TryReadInventoryId(token, out var inventoryId));
        Assert.Equal(42, inventoryId);
    }

    [Fact]
    public void TryReadInventoryId_RejectsTokenSignedWithDifferentSecret()
    {
        var token = CreateService("first-secret").GenerateToken(7);

        var result = CreateService("second-secret").TryReadInventoryId(token, out var inventoryId);

        Assert.False(result);
        Assert.Equal(0, inventoryId);
    }

    [Fact]
    public void TryReadInventoryId_RejectsTamperedPayload()
    {
        var token = CreateService("test-secret").GenerateToken(7);
        var parts = token.Split('.');
        var tampered = $"{parts[0]}x.{parts[1]}";

        Assert.False(CreateService("test-secret").TryReadInventoryId(tampered, out _));
    }

    private static InventoryApiTokenService CreateService(string secret)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Integrations:TokenSecret"] = secret
            })
            .Build();

        return new InventoryApiTokenService(configuration);
    }
}
