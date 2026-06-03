using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;

namespace InventoryPilot.Services;

public class InventoryApiTokenService
{
    private readonly IConfiguration _configuration;

    public InventoryApiTokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GenerateToken(int inventoryId)
    {
        var payload = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(inventoryId.ToString()));
        var signature = Sign(payload);
        return $"{payload}.{signature}";
    }

    public bool TryReadInventoryId(string? token, out int inventoryId)
    {
        inventoryId = 0;
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        var parts = token.Split('.', 2);
        if (parts.Length != 2 || !CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(Sign(parts[0])),
                Encoding.UTF8.GetBytes(parts[1])))
        {
            return false;
        }

        try
        {
            var raw = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(parts[0]));
            return int.TryParse(raw, out inventoryId) && inventoryId > 0;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private string Sign(string payload)
    {
        var secret = _configuration["Integrations:TokenSecret"];
        if (string.IsNullOrWhiteSpace(secret))
        {
            secret = "inventorypilot-local-integration-token-secret";
        }

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        return WebEncoders.Base64UrlEncode(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload)));
    }
}
