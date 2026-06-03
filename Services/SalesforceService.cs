using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using InventoryPilot.Models.ViewModels;

namespace InventoryPilot.Services;

public class SalesforceService
{
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<SalesforceService> _logger;

    public SalesforceService(IConfiguration configuration, IHttpClientFactory httpClientFactory, ILogger<SalesforceService> logger)
    {
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<SalesforceExportResult> CreateAccountAndContactAsync(SalesforceExportInputModel model)
    {
        var authResult = await GetAuthenticationAsync();
        if (!authResult.Success || authResult.Authentication is null)
        {
            return SalesforceExportResult.Failed(authResult.Message);
        }

        var auth = authResult.Authentication;

        using var client = _httpClientFactory.CreateClient();
        if (!Uri.TryCreate(auth.InstanceUrl.TrimEnd('/') + "/", UriKind.Absolute, out var instanceUri))
        {
            return SalesforceExportResult.Failed("Salesforce instance URL is invalid.");
        }

        client.BaseAddress = instanceUri;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);

        var accountPayload = BuildPayload(
            ("Name", model.AccountName),
            ("Phone", model.Phone),
            ("Website", model.Website),
            ("Description", model.Notes));

        var accountResult = await PostSalesforceObjectAsync(client, "Account", accountPayload);
        if (!accountResult.Success || string.IsNullOrWhiteSpace(accountResult.Id))
        {
            return SalesforceExportResult.Failed($"Salesforce account creation failed: {accountResult.Message}");
        }

        var contactPayload = BuildPayload(
            ("AccountId", accountResult.Id),
            ("FirstName", model.FirstName),
            ("LastName", model.LastName),
            ("Email", model.Email),
            ("Phone", model.Phone),
            ("Title", model.Title),
            ("Description", model.Notes));

        var contactResult = await PostSalesforceObjectAsync(client, "Contact", contactPayload);
        if (!contactResult.Success || string.IsNullOrWhiteSpace(contactResult.Id))
        {
            return SalesforceExportResult.Failed($"Salesforce contact creation failed after Account {accountResult.Id} was created: {contactResult.Message}");
        }

        return SalesforceExportResult.Succeeded(accountResult.Id, contactResult.Id);
    }

    private async Task<SalesforceAuthenticationResult> GetAuthenticationAsync()
    {
        var accessToken = _configuration["Salesforce:AccessToken"];
        var instanceUrl = _configuration["Salesforce:InstanceUrl"];
        if (!string.IsNullOrWhiteSpace(accessToken) && !string.IsNullOrWhiteSpace(instanceUrl))
        {
            return SalesforceAuthenticationResult.Succeeded(new SalesforceAuthentication(accessToken, instanceUrl));
        }

        var loginUrl = _configuration["Salesforce:LoginUrl"] ?? "https://login.salesforce.com";
        var authFlow = _configuration["Salesforce:AuthFlow"] ?? "Password";
        var clientId = _configuration["Salesforce:ClientId"];
        var clientSecret = _configuration["Salesforce:ClientSecret"];
        var username = _configuration["Salesforce:Username"];
        var password = _configuration["Salesforce:Password"];
        var securityToken = _configuration["Salesforce:SecurityToken"];

        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
        {
            return SalesforceAuthenticationResult.Failed("Salesforce is not configured. Add ClientId and ClientSecret first.");
        }

        if (!Uri.TryCreate(loginUrl.TrimEnd('/') + "/services/oauth2/token", UriKind.Absolute, out var tokenUri))
        {
            return SalesforceAuthenticationResult.Failed("Salesforce login URL is invalid.");
        }

        using var client = _httpClientFactory.CreateClient();
        var auth = string.Equals(authFlow, "ClientCredentials", StringComparison.OrdinalIgnoreCase)
            ? await RequestClientCredentialsGrantAsync(client, tokenUri, clientId, clientSecret)
            : await RequestPasswordGrantWithFallbackAsync(client, tokenUri, clientId, clientSecret, username, password, securityToken);

        if (!auth.Response.IsSuccessStatusCode)
        {
            _logger.LogError("Salesforce authentication failed. Status: {StatusCode}. Body: {Body}", auth.Response.StatusCode, auth.Body);
            return SalesforceAuthenticationResult.Failed($"Salesforce authentication failed: {ExtractSalesforceError(auth.Body)}");
        }

        var oauth = JsonSerializer.Deserialize<SalesforceOAuthResponse>(auth.Body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        return string.IsNullOrWhiteSpace(oauth?.AccessToken) || string.IsNullOrWhiteSpace(oauth.InstanceUrl)
            ? SalesforceAuthenticationResult.Failed("Salesforce authentication succeeded but did not return an access token.")
            : SalesforceAuthenticationResult.Succeeded(new SalesforceAuthentication(oauth.AccessToken, oauth.InstanceUrl));
    }

    private async Task<SalesforceTokenResponse> RequestPasswordGrantWithFallbackAsync(
        HttpClient client,
        Uri tokenUri,
        string clientId,
        string clientSecret,
        string? username,
        string? password,
        string? securityToken)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            return SalesforceTokenResponse.Failed("Salesforce is not configured. Add Username and Password, or set AuthFlow to ClientCredentials.");
        }

        var auth = await RequestPasswordGrantAsync(client, tokenUri, clientId, clientSecret, username, password + (securityToken ?? string.Empty));
        if (!auth.Response.IsSuccessStatusCode && auth.ErrorCode == "invalid_grant" && !string.IsNullOrWhiteSpace(securityToken))
        {
            _logger.LogWarning("Salesforce authentication failed with password plus security token. Retrying once with password only.");
            auth = await RequestPasswordGrantAsync(client, tokenUri, clientId, clientSecret, username, password);
        }

        return auth;
    }

    private static async Task<SalesforceTokenResponse> RequestPasswordGrantAsync(
        HttpClient client,
        Uri tokenUri,
        string clientId,
        string clientSecret,
        string username,
        string password)
    {
        var response = await client.PostAsync(tokenUri, new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["client_id"] = clientId,
            ["client_secret"] = clientSecret,
            ["username"] = username,
            ["password"] = password
        }));

        var body = await response.Content.ReadAsStringAsync();
        var errorCode = response.IsSuccessStatusCode ? null : ExtractSalesforceErrorCode(body);
        return new SalesforceTokenResponse(response, body, errorCode);
    }

    private static async Task<SalesforceTokenResponse> RequestClientCredentialsGrantAsync(
        HttpClient client,
        Uri tokenUri,
        string clientId,
        string clientSecret)
    {
        var response = await client.PostAsync(tokenUri, new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = clientId,
            ["client_secret"] = clientSecret
        }));

        var body = await response.Content.ReadAsStringAsync();
        var errorCode = response.IsSuccessStatusCode ? null : ExtractSalesforceErrorCode(body);
        return new SalesforceTokenResponse(response, body, errorCode);
    }

    private static string ExtractSalesforceError(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return "Salesforce returned an empty error response.";
        }

        try
        {
            var error = JsonSerializer.Deserialize<SalesforceErrorResponse>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            var message = string.IsNullOrWhiteSpace(error?.ErrorDescription)
                ? error?.Error ?? body
                : $"{error.Error} - {error.ErrorDescription}";

            return error?.Error == "invalid_grant"
                ? $"{message}. Check the selected Salesforce AuthFlow, connected app flow enablement, client credentials, assigned run-as user, API access, and org login policy."
                : message;
        }
        catch (JsonException)
        {
            return body;
        }
    }

    private static string? ExtractSalesforceErrorCode(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<SalesforceErrorResponse>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })?.Error;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string ExtractSalesforceObjectError(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return "Salesforce returned an empty error response.";
        }

        try
        {
            var errors = JsonSerializer.Deserialize<List<SalesforceObjectErrorResponse>>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (errors is { Count: > 0 })
            {
                return string.Join("; ", errors.Select(error =>
                {
                    var fields = error.Fields is { Count: > 0 }
                        ? $" Fields: {string.Join(", ", error.Fields)}."
                        : string.Empty;
                    return $"{error.ErrorCode}: {error.Message}{fields}";
                }));
            }
        }
        catch (JsonException)
        {
        }

        return ExtractSalesforceError(body);
    }

    private async Task<SalesforceCreateResult> PostSalesforceObjectAsync(HttpClient client, string objectName, IReadOnlyDictionary<string, string> payload)
    {
        var version = NormalizeApiVersion(_configuration["Salesforce:ApiVersion"]);
        var json = JsonSerializer.Serialize(payload);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        using var response = await client.PostAsync($"services/data/{version}/sobjects/{objectName}", content);
        var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Salesforce {ObjectName} creation failed. Status: {StatusCode}. Body: {Body}", objectName, response.StatusCode, body);
            return SalesforceCreateResult.Failed(ExtractSalesforceObjectError(body));
        }

        var created = JsonSerializer.Deserialize<SalesforceCreateResponse>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        return string.IsNullOrWhiteSpace(created?.Id)
            ? SalesforceCreateResult.Failed("Salesforce did not return the created record ID.")
            : SalesforceCreateResult.Succeeded(created.Id);
    }

    private static Dictionary<string, string> BuildPayload(params (string Field, string? Value)[] fields)
    {
        return fields
            .Where(field => !string.IsNullOrWhiteSpace(field.Value))
            .ToDictionary(field => field.Field, field => field.Value!.Trim());
    }

    private static string NormalizeApiVersion(string? configuredVersion)
    {
        var version = string.IsNullOrWhiteSpace(configuredVersion) ? "v59.0" : configuredVersion.Trim();
        return version.StartsWith('v') ? version : $"v{version}";
    }

    private record SalesforceAuthentication(string AccessToken, string InstanceUrl);
    private record SalesforceAuthenticationResult(bool Success, string Message, SalesforceAuthentication? Authentication)
    {
        public static SalesforceAuthenticationResult Succeeded(SalesforceAuthentication authentication) => new(true, string.Empty, authentication);
        public static SalesforceAuthenticationResult Failed(string message) => new(false, message, null);
    }
    private record SalesforceTokenResponse(HttpResponseMessage Response, string Body, string? ErrorCode)
    {
        public static SalesforceTokenResponse Failed(string message) =>
            new(new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest), JsonSerializer.Serialize(new SalesforceErrorResponse
            {
                Error = "configuration_error",
                ErrorDescription = message
            }), "configuration_error");
    }
    private record SalesforceCreateResult(bool Success, string? Id, string Message)
    {
        public static SalesforceCreateResult Succeeded(string id) => new(true, id, string.Empty);
        public static SalesforceCreateResult Failed(string message) => new(false, null, message);
    }
    private sealed class SalesforceErrorResponse
    {
        [JsonPropertyName("error")]
        public string? Error { get; set; }

        [JsonPropertyName("error_description")]
        public string? ErrorDescription { get; set; }
    }
    private sealed class SalesforceObjectErrorResponse
    {
        public string? Message { get; set; }
        public string? ErrorCode { get; set; }
        public List<string>? Fields { get; set; }
    }
    private sealed class SalesforceOAuthResponse
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }

        [JsonPropertyName("instance_url")]
        public string? InstanceUrl { get; set; }
    }
    private sealed class SalesforceCreateResponse
    {
        public string? Id { get; set; }
    }
}

public class SalesforceExportResult
{
    public bool Success { get; private init; }
    public string Message { get; private init; } = string.Empty;
    public string? AccountId { get; private init; }
    public string? ContactId { get; private init; }

    public static SalesforceExportResult Succeeded(string accountId, string contactId) => new()
    {
        Success = true,
        AccountId = accountId,
        ContactId = contactId,
        Message = $"Created Salesforce Account {accountId} and Contact {contactId}."
    };

    public static SalesforceExportResult Failed(string message) => new() { Message = message };
}
