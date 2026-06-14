namespace Summarizer.Core.RecruitmentConverters
{
    public interface IRecruitmentMessageConverter
    {
        bool CanConvert(string firstLine);
        string Convert(string text);
    }
}
