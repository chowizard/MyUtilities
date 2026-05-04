namespace Summarizer.Core
{
    public record ReplaceMessage
    {
        public string Pattern { get; init; } = string.Empty;
        public string Replacement { get; init; } = string.Empty;
    }
}
