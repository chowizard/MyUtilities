using System.Text;
using System.Text.RegularExpressions;

namespace Summarizer
{
    public partial class SummarizerForm : Form
    {
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

        //[GeneratedRegex("\\b010\\d{8}\\b")]
        [GeneratedRegex("\\b010.*?\\d{4}.*?\\d{4}\\b")]
        private static partial Regex PhoneNumberRegex();


        public SummarizerForm()
        {
            InitializeComponent();
        }

        private void Summarizer_Load(object sender, EventArgs e)
        {
            Text = $"Summarizer (v{Program.Version})";
        }

        private void checkBoxAlwaysTop_CheckedChanged(object sender, EventArgs e)
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
        private void buttonClear_Click(object sender, EventArgs e)
        {
            textBoxInput.Text = string.Empty;
            textBoxOutput.Text = string.Empty;
        }

        /// <summary>
        /// 변환 결과 문자를 복사하는 버튼 클릭 이벤트
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonCopyOutput_Click(object sender, EventArgs e)
        {
            Clipboard.SetText(textBoxOutput.Text);
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
