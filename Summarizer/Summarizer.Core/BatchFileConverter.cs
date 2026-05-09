using System.Text;

namespace Summarizer.Core
{
    public class BatchFileConverter
    {
        private readonly MessageConverter converter;


        public BatchFileConverter(AppSettings settings)
        {
            converter = new MessageConverter(settings);
        }

        public void ConvertFile(string inputFilePath)
        {
            var text = File.ReadAllText(inputFilePath, new UTF8Encoding(false));
            var conversations = ParseConversations(text);
            var results = conversations.Select(c => converter.Convert(c));
            var outputFilePath = BuildOutputFilePath(inputFilePath);
            File.WriteAllLines(outputFilePath, results, new UTF8Encoding(false));
        }

        private static List<string> ParseConversations(string text)
        {
            List<string> conversations = [];
            List<string> currentBlock = [];
            bool isInsideBlock = false;

            foreach (var line in text.Split(["\r\n", "\n"], StringSplitOptions.None))
            {
                var trimmed = line.Trim();
                if (!isInsideBlock)
                {
                    if (trimmed == "[")
                    {
                        currentBlock.Clear();
                        isInsideBlock = true;
                    }
                }
                else
                {
                    if (trimmed == "]")
                    {
                        conversations.Add(string.Join(Environment.NewLine, currentBlock));
                        isInsideBlock = false;
                    }
                    else
                    {
                        currentBlock.Add(line);
                    }
                }
            }

            return conversations;
        }

        private static string BuildOutputFilePath(string inputFilePath)
        {
            var directory = Path.GetDirectoryName(inputFilePath) ?? string.Empty;
            var nameWithoutExtension = Path.GetFileNameWithoutExtension(inputFilePath);
            var extension = Path.GetExtension(inputFilePath);
            return Path.Combine(directory, $"{nameWithoutExtension}-converted{extension}");
        }
    }
}
