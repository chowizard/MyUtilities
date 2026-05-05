using System.Text.Json.Serialization;

namespace Summarizer.Core
{
    public record ReplaceMessage
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Comment { get; init; }
        public string Pattern { get; init; } = string.Empty;
        public string Replacement { get; init; } = string.Empty;
    }
}
