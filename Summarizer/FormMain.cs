using System.Text;
using System.Text.RegularExpressions;

namespace Summarizer
{
    public partial class SummarizerForm : Form
    {
        /// <summary>
        /// 휴대전화번호를 식별하는 정규표현식 (01000000000 또는 010-0000-0000 방식)
        /// </summary>
        /// <returns></returns>
        //[GeneratedRegex("\\b010\\d{8}\\b")]
        [GeneratedRegex("\\b010.*?\\d{4}.*?\\d{4}\\b")]
        private static partial Regex PhoneNumberRegex();

        /// <summary>
        /// 카카오톡 메시지의 시간값을 식별하는 정규표현식
        /// </summary>
        /// <returns></returns>
        [GeneratedRegex("오(전|후)\\d{2}:\\d{2}(\r?\n)")]
        private static partial Regex KakaoTalkMessageTimeRegex();

        /// <summary>
        /// 카카오톡 메시지의 스태프 메시지 식별 정규표현식
        /// </summary>
        /// <returns></returns>
        [GeneratedRegex("님이 보냄 보낸 메시지 가이드")]
        private static partial Regex KakaoTalkStaffMessageRegex();


        public SummarizerForm()
        {
            InitializeComponent();

            #region TEST
            var exampleText = "오후08:32\r\naaaaabbbbb";
            var match = KakaoTalkMessageTimeRegex().Match(exampleText);
            if (match.Success)
            {

            }
            #endregion TEST
        }

        private void Summarizer_Load(object sender, EventArgs e)
        {
            Text = $"Summarizer (v{Program.Version})";
        }

        private void CheckBoxAlwaysTop_CheckedChanged(object sender, EventArgs e)
        {
            TopMost = checkBoxAlwaysTop.Checked;
            Update();
        }

        /// <summary>
        /// 변환하기 버튼 클릭 이벤트
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonConvert_Click(object sender, EventArgs e)
        {
            var converted = ConvertText(textBoxInput.Text);
            textBoxOutput.Text = converted;
        }

        /// <summary>
        /// 비우기 버튼 클릭 이벤트
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonClear_Click(object sender, EventArgs e)
        {
            textBoxInput.Text = string.Empty;
            textBoxOutput.Text = string.Empty;
        }

        /// <summary>
        /// 변환 결과 문자를 복사하는 버튼 클릭 이벤트
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonCopyOutput_Click(object sender, EventArgs e)
        {
            Clipboard.SetText(textBoxOutput.Text);
        }

        private static bool IsCellPhoneNumber(string text)
        {
            return !string.IsNullOrEmpty(text) && PhoneNumberRegex().IsMatch(text);
        }

        private static string StandardizeCellPhoneNumber(string phoneNumberText)
        {
            if (string.IsNullOrEmpty(phoneNumberText))
                return string.Empty;

            int numberCount = 0;
            StringBuilder builder = new();
            for (int index = 0; index < phoneNumberText.Length; ++index)
            {
                var character = phoneNumberText[index];
                if (numberCount is 3 or 8)
                {
                    builder.Append('-');
                    ++numberCount;
                }

                bool isPhoneNumberText = (numberCount > 0) && (numberCount <= 11);
                if (isPhoneNumberText)
                {
                    // 전화번호의 '-' 구분자만 중복으로 추가되지 않게 한다.
                    if (character is not '-')
                        builder.Append(character);
                }
                else
                {
                    // 그 외의 경우에는 '-' 구분자도 원본 메시지에 포함되어 있는 문자로 간주한다.
                    builder.Append(character);
                }

                if ((character >= '0') && (character <= '9'))
                    ++numberCount;
            }

            return builder.ToString();
        }

        /// <summary>
        /// 텍스트 변환 처리
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        private static string ConvertText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            var matches = KakaoTalkMessageTimeRegex().Matches(text);
            if (matches.Count > 0)
            {
                // KakoTalk 메시지를 복사한 것으로 판정되면, 각 문단을 식별하여 손님 또는 스태프의 메시지로 구분하여 변환 처리
                List<string> convertedTexts = new(matches.Count);
                for (int index = 0; index < matches.Count; ++index)
                {
                    var match = matches[index];

                    int matchStartIndex = match.Index;
                    int matchEndIndex = ((index + 1) >= matches.Count) ? text.Length : matches[index + 1].Index;
                    int copyCount = matchEndIndex - matchStartIndex;
                    char[] paragraphBuffer = new char[matchEndIndex - matchStartIndex];
                    text.CopyTo(match.Index, paragraphBuffer, 0, copyCount);
                    string paragraph = new(paragraphBuffer);

                    var splited = paragraph.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    if (splited.Length < 3)
                        continue;

                    string currentConverted;
                    var senderText = splited[1];
                    if (KakaoTalkStaffMessageRegex().IsMatch(senderText))
                    {
                        var targetText = string.Join(Environment.NewLine, splited, 2, splited.Length - 2);
                        currentConverted = ConvertStaffText(targetText);
                    }
                    else
                    {
                        // '프로필 사진' 텍스트가 있는 경우, 그 다음 줄이 보낸 사람의 이름이다.
                        int startIndex = senderText.Contains("프로필 사진") ? 3 : 2;
                        var targetText = string.Join(Environment.NewLine, splited, startIndex, splited.Length - startIndex);
                        currentConverted = ConvertCustomerText(targetText);
                    }

                    convertedTexts.Add(currentConverted);
                }

                var finalText = string.Join(" / ", convertedTexts);
                return finalText;
            }
            else
            {
                // KakoTalk 메시지 복사 방식이 아닌 것으로 판정되면, 전체 텍스트를 손님의 메시지로 간주하고 변환 처리
                return ConvertCustomerText(text);
            }
        }

        /// <summary>
        /// 손님의 메시지 형식으로 변환한다.
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        /// <remarks>
        /// 메시지의 전체를 '['']' 기호로 감싼다.
        /// </remarks>
        private static string ConvertCustomerText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            var splitTexts = SplitTextContents(text);
            if (splitTexts.Count <= 0)
                return string.Empty;

            StringBuilder builder = new();
            builder.Append("[ ");
            for (int index = 0; index < splitTexts.Count; ++index)
            {
                var splitText = splitTexts[index];
                if (string.IsNullOrEmpty(splitText))
                    continue;

                var currentText = splitText;

                // 휴대전화번호인 텍스트는 '-' 기호로 구분
                if (IsCellPhoneNumber(splitText))
                    currentText = StandardizeCellPhoneNumber(splitText);
                //{
                //    var phoneNumber = currentText;
                //    currentText = $"010-{currentText.Substring(3, 4)}-{currentText.Substring(7, 4)}";
                //}

                builder.Append(currentText);

                if (index < (splitTexts.Count - 1))
                    builder.Append(" / ");
            }
            builder.Append(" ]");

            return builder.ToString();
        }

        /// <summary>
        /// 스태프의 메시지 저장 형식으로 변환한다.
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        /// <remarks>
        /// 메시지를 대괄호 기호([])로 감쌀 필요 없음. + 사전에 지정되어 있는 문자 추가
        /// </remarks>
        private static string ConvertStaffText(string text)
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

                // 휴대전화번호인 텍스트는 '-' 기호로 구분
                if (IsCellPhoneNumber(splitText))
                    currentText = StandardizeCellPhoneNumber(splitText);
                //{
                //    var phoneNumber = currentText;
                //    currentText = $"010-{currentText.Substring(3, 4)}-{currentText.Substring(7, 4)}";
                //}

                builder.Append(currentText);

                if (index < (splitTexts.Count - 1))
                    builder.Append(" / ");
            }

            return builder.ToString();
        }

        /// <summary>
        /// 변환할 텍스트의 구분 처리
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        private static List<string> SplitTextContents(string text)
        {
            if (string.IsNullOrEmpty(text))
                return [];

            List<string> resultTexts = [];

            // "" 기호로 한 줄에 들어가야 할 텍스트를 먼저 구별한다.
            var splitTextInLines = text.Split(['\'', '\"'], StringSplitOptions.TrimEntries);
            for (int index = 0; index < splitTextInLines.Length; ++index)
            {
                string textLine = splitTextInLines[index];
                if (string.IsNullOrEmpty(textLine))
                    continue;

                // 홀수 인덱스의 요소들은 ' 또는 " 기호에 둘러싸인 문자열로 취급한다.
                if (index % 2 > 0)
                    textLine = textLine.Replace(Environment.NewLine, " ");

                // '/' 기호를 띄어쓰기 없이 붙여쓴 것은 날짜 등을 표기하기 위한 것으로 간주하고, 하나의 문장으로 취급한다.
                var splitTexts = textLine.Split([Environment.NewLine, " / "], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                resultTexts.AddRange(splitTexts);
            }

            return resultTexts;
        }
    }
}
