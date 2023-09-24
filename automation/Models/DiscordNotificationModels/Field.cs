using System.Text.Json.Serialization;

namespace Automation.Models.DiscordNotificationModels;

public class Field
{
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("value")]
    public string Value { get; set; }

    [JsonPropertyName("inline")]
    public bool? Inline { get; set; }
}