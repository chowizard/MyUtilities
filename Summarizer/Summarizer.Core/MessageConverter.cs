using System.Text;
using System.Text.RegularExpressions;

namespace Summarizer.Core
{
    public partial class MessageConverter
    {
        private readonly AppSettings settings;
        private readonly ReplaceMatcher[] replaceMatchers;
        private readonly FormMatcher[] formMatchers;


        [GeneratedRegex(@"(?:\+?\b8?2[-\s.]*|\b)0?10[-\s.]*\d{4}[-\s.]*\d{4}\b")]
        private static partial Regex PhoneNumberRegex();

        [GeneratedRegex(@"(\b\d?\d?\d{2})년?.*?([0-2]\d)월?.*?([0-3]\d일?)\b")]
        private static partial Regex BirthNumberRegex();

        [GeneratedRegex(@"오(전|후)\d{2}:\d{2}(\r?\n)")]
        private static partial Regex KakaoTalkMessageTimeRegex();

        [GeneratedRegex(@"님이 보냄 보낸 메시지 가이드")]
        private static partial Regex KakaoTalkStaffMessageRegex();

        [GeneratedRegex(@"(\d+?([/.-]|년))?[ ]?(\d?\d?([/.-]|월))[ ]?(\d?\d?일?)[ ]?(일|월|화|수|목|금|토)(요일)?[ ]?\d?\d?시[ ]?\d?\d?분?[ ]?(.+?[ ]?원장님?)[ ]?상담[ ]?(예약)?$")]
        private static partial Regex ReservationRegex();


        private readonly record struct ReplaceMatcher(bool IsRegex, string PlainPattern, string Replacement, Regex? CompiledPattern);

        private readonly record struct FormMatcher(bool IsRegex, string PlainPattern, Regex? CompiledPattern);


        public MessageConverter(AppSettings settings)
        {
            this.settings = settings;
            replaceMatchers = [.. settings.ReplaceStaffMessages.Select(ParseReplaceMatcher)];
            formMatchers = [.. settings.FormMessages.Select(ParseFormMatcher)];
        }

        private static ReplaceMatcher ParseReplaceMatcher(ReplaceMessage message)
        {
            const string regexPrefix = "regex:";
            if (message.Pattern.StartsWith(regexPrefix, StringComparison.OrdinalIgnoreCase))
            {
                var pattern = message.Pattern[regexPrefix.Length..];
                return new ReplaceMatcher(true, pattern, message.Replacement, new Regex(pattern, RegexOptions.Compiled));
            }
            return new ReplaceMatcher(false, message.Pattern, message.Replacement, null);
        }

        private static FormMatcher ParseFormMatcher(string formMessage)
        {
            const string regexPrefix = "regex:";
            if (formMessage.StartsWith(regexPrefix, StringComparison.OrdinalIgnoreCase))
            {
                var pattern = formMessage[regexPrefix.Length..];
                return new FormMatcher(true, pattern, new Regex(pattern, RegexOptions.Compiled));
            }
            return new FormMatcher(false, formMessage, null);
        }

        public string Convert(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            var matches = KakaoTalkMessageTimeRegex().Matches(text);
            if (matches.Count > 0)
            {
                List<string> convertedTexts = new(matches.Count);
                for (int matchIndex = 0; matchIndex < matches.Count; ++matchIndex)
                {
                    var match = matches[matchIndex];

                    // @NOTE 예외상황에 대한 처리
                    // : KakaoTalk Business 메시지의 여러 대화들을 한번에 복사할 때, 최초 화자의 대화 문자열에는 시간 표시 텍스트가 없다.
                    //   이 경우에는 KakaoTalkMessageTimeRegex()에 의한 탐지가 안되기 때문에, 이때는 예외적으로 한번 더 처리한다.
                    if ((matchIndex == 0) && (match.Index > 0))
                    {
                        int matchEndIndex = match.Index - 1;
                        char[] paragraphBuffer = new char[matchEndIndex];
                        text.CopyTo(0, paragraphBuffer, 0, matchEndIndex);
                        string paragraph = new(paragraphBuffer);

                        if (ConvertCategorizedText(paragraph, true, out var convertedCategorized))
                        {
                            var splited = paragraph.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                            if (splited.Length >= 2)
                                convertedTexts.Add(convertedCategorized);
                        }
                    }

                    {
                        int matchStartIndex = match.Index;
                        int matchEndIndex = ((matchIndex + 1) >= matches.Count) ? text.Length : matches[matchIndex + 1].Index;
                        int copyCount = matchEndIndex - matchStartIndex;
                        char[] paragraphBuffer = new char[copyCount];
                        text.CopyTo(matchStartIndex, paragraphBuffer, 0, copyCount);
                        string paragraph = new(paragraphBuffer);

                        if (ConvertCategorizedText(paragraph, false, out var convertedCategorized))
                        {
                            var splited = paragraph.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                            if (splited.Length >= 3)
                                convertedTexts.Add(convertedCategorized);
                        }
                    }
                }

                return string.Join(" / ", convertedTexts);
            }
            else
            {
                return ConvertCustomerText(text);
            }
        }

        private bool ConvertCategorizedText(string paragraph, bool isFirstMessage, out string convertCategorized)
        {
            convertCategorized = string.Empty;

            var splited = paragraph.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (splited.Length < 3)
                return false;

            var senderText = isFirstMessage ? splited[0] : splited[1];
            if (KakaoTalkStaffMessageRegex().IsMatch(senderText))
            {
                var startIndex = isFirstMessage ? 1 : 2;
                var targetText = string.Join(Environment.NewLine, splited, startIndex, splited.Length - 2);
                convertCategorized = ConvertStaffText(targetText);
            }
            else
            {
                int startIndex = senderText.Contains("프로필 사진") ?
                                 isFirstMessage ? 2 : 3 :
                                 isFirstMessage ? 1 : 2;
                var targetText = string.Join(Environment.NewLine, splited, startIndex, splited.Length - startIndex);
                convertCategorized = ConvertCustomerText(targetText);
            }

            return true;
        }

        private string ConvertCustomerText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            var splitTexts = SplitTextContents(text);
            if (splitTexts.Count <= 0)
                return string.Empty;

            var summarizedLines = SummarizeTextContents(splitTexts);

            StringBuilder builder = new();
            builder.Append("[ ");
            for (int index = 0; index < summarizedLines.Count; ++index)
            {
                var splitText = summarizedLines[index];
                if (string.IsNullOrEmpty(splitText))
                    continue;

                var currentText = splitText;

                if (IsCellPhoneNumber(currentText))
                    currentText = StandardizeCellPhoneNumber(currentText);
                else if (settings.NormalizeBirthNumber)
                    currentText = StandardizeBirthNumber(currentText);

                builder.Append(currentText);

                if (index < (summarizedLines.Count - 1))
                    builder.Append(" / ");
            }
            builder.Append(" ]");

            return builder.ToString();
        }

        private string ConvertStaffText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            var splitTexts = SplitTextContents(text);
            if (splitTexts.Count <= 0)
                return string.Empty;

            StringBuilder builder = new();
            for (int index = 0; index < splitTexts.Count; ++index)
            {
                var splitText = splitTexts[index];
                if (string.IsNullOrEmpty(splitText))
                    continue;

                var currentText = splitText;

                if (IsCellPhoneNumber(currentText))
                    currentText = StandardizeCellPhoneNumber(currentText);
                else if (settings.NormalizeBirthNumber)
                    currentText = StandardizeBirthNumber(currentText);

                currentText = ApplyReplaceMessages(currentText);

                builder.Append(currentText);

                if (index < (splitTexts.Count - 1))
                    builder.Append(" / ");
            }

            var lastMessage = splitTexts[^1];
            if (ReservationRegex().IsMatch(lastMessage))
                builder.Append($" / {settings.ReservationConfirmMessage} // {settings.StaffName}");

            return builder.ToString();
        }

        private static List<string> SplitTextContents(string text)
        {
            if (string.IsNullOrEmpty(text))
                return [];

            List<string> resultTexts = [];

            var splitTextInLines = text.Split(['\'', '\"'], StringSplitOptions.TrimEntries);
            for (int index = 0; index < splitTextInLines.Length; ++index)
            {
                string textLine = splitTextInLines[index];
                if (string.IsNullOrEmpty(textLine))
                    continue;

                if (index % 2 > 0)
                    textLine = textLine.Replace(Environment.NewLine, " ");

                var splitTexts = textLine.Split(
                    [Environment.NewLine],
                    StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                resultTexts.AddRange(splitTexts);
            }

            return resultTexts;
        }

        private List<string> SummarizeTextContents(ICollection<string> texts)
        {
            List<string> summarizedTexts = new(texts.Count);
            foreach (var text in texts)
            {
                int index = Array.FindIndex(formMatchers, matcher => FormMatcherMatches(text, matcher));
                summarizedTexts.Add(index >= 0 ? ApplyFormMatcher(text, formMatchers[index]) : text);
            }
            return summarizedTexts;
        }

        private static bool FormMatcherMatches(string text, FormMatcher matcher)
        {
            if (string.Compare(text, matcher.PlainPattern, StringComparison.InvariantCultureIgnoreCase) == 0)
                return false;

            return matcher.IsRegex
                ? matcher.CompiledPattern!.IsMatch(text)
                : text.Contains(matcher.PlainPattern);
        }

        private static string ApplyFormMatcher(string text, FormMatcher matcher)
        {
            return matcher.IsRegex
                ? matcher.CompiledPattern!.Replace(text, string.Empty)
                : text.Replace(matcher.PlainPattern, string.Empty);
        }

        private string ApplyReplaceMessages(string text)
        {
            var current = text;
            foreach (var matcher in replaceMatchers)
            {
                if (matcher.IsRegex)
                    current = matcher.CompiledPattern!.Replace(current, matcher.Replacement);
                else
                    current = current.Replace(matcher.PlainPattern, matcher.Replacement);
            }
            return current;
        }

        private static bool IsCellPhoneNumber(string text)
            => !string.IsNullOrEmpty(text) && PhoneNumberRegex().IsMatch(text);

        private static string StandardizeCellPhoneNumber(string phoneNumberText)
        {
            if (string.IsNullOrEmpty(phoneNumberText))
                return string.Empty;

            var match = PhoneNumberRegex().Match(phoneNumberText);
            if (!match.Success)
                return phoneNumberText;

            StringBuilder builder = new();
            builder.Append(phoneNumberText, 0, match.Index);
            builder.Append("010-");

            var numbers = match.Value
                .Where(character => (character >= '0') && (character <= '9'))
                .TakeLast(8)
                .ToArray();
            for (int index = 0; index < numbers.Length; ++index)
            {
                if (index == 4)
                    builder.Append('-');
                builder.Append(numbers[index]);
            }

            var extraTextStartIndex = match.Index + match.Length;
            builder.Append(phoneNumberText, extraTextStartIndex, phoneNumberText.Length - extraTextStartIndex);

            return builder.ToString();
        }

        private static string StandardizeBirthNumber(string birthNumberText)
        {
            if (string.IsNullOrEmpty(birthNumberText))
                return string.Empty;

            var match = BirthNumberRegex().Match(birthNumberText);
            if (!match.Success)
                return birthNumberText;

            StringBuilder builder = new();
            builder.Append(birthNumberText, 0, match.Index);

            var numbers = match.Value.Where(char.IsDigit).ToArray();
            builder.Append(numbers, 0, numbers.Length - 4); // 연도
            builder.Append('-');
            builder.Append(numbers, numbers.Length - 4, 2); // 월
            builder.Append('-');
            builder.Append(numbers, numbers.Length - 2, 2); // 일

            var extraTextStartIndex = match.Index + match.Length;
            builder.Append(birthNumberText, extraTextStartIndex, birthNumberText.Length - extraTextStartIndex);

            return builder.ToString();
        }
    }
}
