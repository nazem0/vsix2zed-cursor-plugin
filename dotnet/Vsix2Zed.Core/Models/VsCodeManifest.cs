using System.Text.Json;
using System.Text.Json.Serialization;

namespace Vsix2Zed.Core.Models;

public sealed class VsCodeManifest
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("displayName")]
    public string? DisplayName { get; set; }

    [JsonPropertyName("version")]
    public string? Version { get; set; }

    [JsonPropertyName("publisher")]
    public string? Publisher { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("author")]
    public JsonElement? Author { get; set; }

    [JsonPropertyName("repository")]
    public JsonElement? Repository { get; set; }

    [JsonPropertyName("main")]
    public string? Main { get; set; }

    [JsonPropertyName("browser")]
    public string? Browser { get; set; }

    [JsonPropertyName("activationEvents")]
    public JsonElement? ActivationEvents { get; set; }

    [JsonPropertyName("engines")]
    public JsonElement? Engines { get; set; }

    [JsonPropertyName("categories")]
    public JsonElement? Categories { get; set; }

    [JsonPropertyName("contributes")]
    public JsonElement? Contributes { get; set; }

    [JsonPropertyName("extensionDependencies")]
    public JsonElement? ExtensionDependencies { get; set; }

    [JsonPropertyName("dependencies")]
    public JsonElement? Dependencies { get; set; }

    public string ExtensionId =>
        string.IsNullOrWhiteSpace(Publisher)
            ? (Name ?? "unknown")
            : $"{Publisher}.{Name}";

    public string AuthorName
    {
        get
        {
            if (Author is null) return Publisher ?? "unknown";
            var a = Author.Value;
            if (a.ValueKind == JsonValueKind.String) return a.GetString() ?? "unknown";
            if (a.ValueKind == JsonValueKind.Object && a.TryGetProperty("name", out var n))
                return n.GetString() ?? Publisher ?? "unknown";
            return Publisher ?? "unknown";
        }
    }

    public string? RepositoryUrl
    {
        get
        {
            if (Repository is null) return null;
            var r = Repository.Value;
            if (r.ValueKind == JsonValueKind.String) return r.GetString();
            if (r.ValueKind == JsonValueKind.Object && r.TryGetProperty("url", out var u))
                return u.GetString();
            return null;
        }
    }
}
