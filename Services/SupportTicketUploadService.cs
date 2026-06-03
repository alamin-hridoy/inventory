using System.Text;
using System.Text.Json;
using InventoryPilot.Models.Integrations;

namespace InventoryPilot.Services;

public class SupportTicketUploadService
{
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<SupportTicketUploadService> _logger;

    public SupportTicketUploadService(IConfiguration configuration, IHttpClientFactory httpClientFactory, ILogger<SupportTicketUploadService> logger)
    {
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<SupportTicketUploadResult> UploadAsync(SupportTicketPayload ticket)
    {
        var provider = _configuration["SupportTickets:Provider"];
        return string.Equals(provider, "Dropbox", StringComparison.OrdinalIgnoreCase)
            ? await UploadToDropboxAsync(ticket)
            : SupportTicketUploadResult.Succeeded("Support ticket JSON was generated. Configure SupportTickets:Provider=Dropbox to upload it for Power Automate.");
    }

    private async Task<SupportTicketUploadResult> UploadToDropboxAsync(SupportTicketPayload ticket)
    {
        var accessToken = _configuration["Dropbox:AccessToken"] ?? _configuration["SupportTickets:DropboxAccessToken"];
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return SupportTicketUploadResult.Failed("Dropbox access token is missing.");
        }

        var folder = _configuration["SupportTickets:DropboxFolder"] ?? "/inventorypilot-support";
        var fileName = $"support-ticket-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}.json";
        var path = $"{folder.TrimEnd('/')}/{fileName}";
        var json = JsonSerializer.Serialize(ticket, new JsonSerializerOptions { WriteIndented = true });

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://content.dropboxapi.com/2/files/upload")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/octet-stream")
        };
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.Add("Dropbox-API-Arg", JsonSerializer.Serialize(new
        {
            path,
            mode = "add",
            autorename = true,
            mute = false
        }));

        try
        {
            using var client = _httpClientFactory.CreateClient();
            using var response = await client.SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Dropbox upload failed. Status: {StatusCode}. Body: {Body}", response.StatusCode, body);
                return SupportTicketUploadResult.Failed("Dropbox upload failed. Check app logs and Dropbox token permissions.");
            }

            return SupportTicketUploadResult.Succeeded($"Support ticket uploaded to Dropbox as {path}.");
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidOperationException)
        {
            _logger.LogError(ex, "Dropbox upload failed.");
            return SupportTicketUploadResult.Failed("Dropbox upload failed. Check app logs and network configuration.");
        }
    }
}

public class SupportTicketUploadResult
{
    public bool Success { get; private init; }
    public string Message { get; private init; } = string.Empty;

    public static SupportTicketUploadResult Succeeded(string message) => new() { Success = true, Message = message };
    public static SupportTicketUploadResult Failed(string message) => new() { Message = message };
}
