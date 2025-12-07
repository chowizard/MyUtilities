using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace Summarizer
{
    public partial class SummarizerForm : Form
    {
        /// <summary>
        /// 설정 객체
        /// </summary>
        private readonly Configuration configuration = new();


        /// <summary>
        /// 휴대전화번호를 식별하는 정규표현식
        /// </summary>
        /// <returns></returns>
        /// <remarks>
        /// 010-0000-0000 : 표준
        /// 01000000000
        /// 010 0000 0000
        /// </remarks>
        //[GeneratedRegex("\\b010\\d{8}\\b")]
        [GeneratedRegex(@"\b010.*?\d{4}.*?\d{4}\b")]
        private static partial Regex PhoneNumberRegex();

        /// <summary>
        /// 생년월일을 식별하는 정규표현식
        /// </summary>
        /// <returns></returns>
        /// <remarks>
        /// 0000-00-00 : 표준
        /// 0000년 00월 00일
        /// 00000000-
        /// 0000.00.00
        /// 0000 00 00
        /// 00-00-00
        /// 00년 00월 00일
        /// 000000
        /// 00 00 00
        /// 00.00.00
        /// </remarks>
        [GeneratedRegex(@"(\b\d?\d?\d{2})년?.*?([0-2]\d)월?.*?([0-3]\d일?)\b")]
        private static partial Regex BirthNumberRegex();

        /// <summary>
        /// 카카오톡 메시지의 시간값을 식별하는 정규표현식
        /// </summary>
        /// <returns></returns>
        [GeneratedRegex(@"오(전|후)\d{2}:\d{2}(\r?\n)")]
        private static partial Regex KakaoTalkMessageTimeRegex();

        /// <summary>
        /// 카카오톡 메시지의 직원 메시지 식별 정규표현식
        /// </summary>
        /// <returns></returns>
        [GeneratedRegex(@"님이 보냄 보낸 메시지 가이드")]
        private static partial Regex KakaoTalkStaffMessageRegex();

        /// <summary>
        /// 내원 예약 메시지의 식별 정규표현식
        /// </summary>
        /// <returns></returns>
        /// <remarks>
        /// 예를 들어, 다음과 같은 문장들을 모두 잡아낼 수 있다.
        /// ex)
        /// 11월 21일 금요일 2시 30분 aaa원장님 상담 예약
        /// 11월 21일금 2시 30분 bbb원장님 상담예약
        /// 11.21 금요일 2시 3분 cc 원장 상담예약
        /// 11-21일금요일 2시 30분 dddd원장님 상담
        /// 1월 2일 금요일 12시 30분 eee원장님 상담예약
        /// </remarks>
        [GeneratedRegex(@"(\d+?([/.-]|년))?[ ]?(\d?\d?([/.-]|월))[ ]?(\d?\d?일?)[ ]?(일|월|화|수|목|금|토)(요일)?[ ]?\d?\d?시[ ]?\d?\d?분?[ ]?(.+?[ ]?원장님?)[ ]?상담[ ]?(예약)?$")]
        private static partial Regex ReservationRegex();


        public SummarizerForm()
        {
            InitializeComponent();
            LoadAppSettings();
        }

        private void LoadAppSettings()
        {
            if (AppSettings.Default == null)
                return;

            configuration.staffName = AppSettings.Default.StaffName;
            configuration.reservationConfirmMessage = AppSettings.Default.ReservationConfirmMessage;
            if ((AppSettings.Default.FormMessages != null) && (AppSettings.Default.FormMessages.Count > 0))
            {
                configuration.formMessages = new string[AppSettings.Default.FormMessages.Count];
                AppSettings.Default.FormMessages.CopyTo(configuration.formMessages, 0);

                configuration.sliceMessages = new string[AppSettings.Default.SliceStaffMessages.Count];
                AppSettings.Default.SliceStaffMessages.CopyTo(configuration.sliceMessages, 0);
            }
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

        private static string StandardizeBirthNumber(string birthNumberText)
        {
            if (string.IsNullOrEmpty(birthNumberText))
                return string.Empty;

            int numberCount = 0;
            StringBuilder builder = new();

            // 100세 미만의 연령이 되도록 연도의 백년 단위를 구분하여 적용한다.
            //char[] yearBuffer = new char[4];
            //if (birthNumberText.Length > 6)
            //    yearBuffer.CopyTo()

            for (int index = 0; index < birthNumberText.Length; ++index)
            {
                var character = birthNumberText[index];
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
        private string ConvertText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            var matches = KakaoTalkMessageTimeRegex().Matches(text);
            if (matches.Count > 0)
            {
                // KakoTalk Business 메시지를 복사한 것으로 판정되면, 각 문단을 식별하여 손님 또는 직원의 메시지로 구분하여 변환 처리
                List<string> convertedTexts = new(matches.Count);
                for (int matchIndex = 0; matchIndex < matches.Count; ++matchIndex)
                {
                    var match = matches[matchIndex];

                    // @NOTE 예외상황에 대한 처리
                    // : KakaoTalk Business 메시지의 여러 대화들을 한번에 복사할 때, 최초 화자의 대화 문자열에는 시간 표시 텍스트가 없다.
                    //   이 경우에는 KakaoTalkMessageTimeRegex()에 의한 탐지가 안되기 때문에, 이때는 예외적으로 한번 더 처리한다.
                    if ((matchIndex == 0) && (match.Index > 0))
                    {
                        int matchStartIndex = 0;
                        int matchEndIndex = match.Index - 1;
                        int copyCount = matchEndIndex - matchStartIndex;
                        char[] paragraphBuffer = new char[matchEndIndex - matchStartIndex];
                        text.CopyTo(0, paragraphBuffer, 0, copyCount);
                        string paragraph = new(paragraphBuffer);

                        if (ConvertCategorizedText(paragraph, true, out var convertedCategorized))
                        {
                            // @NOTE 첫 메시지는 시간 표시 텍스트가 없으므로, <화자 이름> + <대화 내용>의 2줄이 최소 단위이다.
                            var splited = paragraph.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                            if (splited.Length >= 2)
                                convertedTexts.Add(convertedCategorized);
                        }
                    }

                    {
                        int matchStartIndex = match.Index;
                        int matchEndIndex = ((matchIndex + 1) >= matches.Count) ? text.Length : matches[matchIndex + 1].Index;
                        int copyCount = matchEndIndex - matchStartIndex;
                        char[] paragraphBuffer = new char[matchEndIndex - matchStartIndex];
                        text.CopyTo(match.Index, paragraphBuffer, 0, copyCount);
                        string paragraph = new(paragraphBuffer);

                        if (ConvertCategorizedText(paragraph, false, out var convertedCategorized))
                        {
                            var splited = paragraph.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                            if (splited.Length >= 3)
                                convertedTexts.Add(convertedCategorized);
                        }
                    }
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

        private bool ConvertCategorizedText(string paragraph, bool isFirstMessage, out string convertCategorized)
        {
            convertCategorized = null;

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
                // '프로필 사진' 텍스트가 있는 경우, 그 다음 줄이 보낸 사람의 이름이다.
                int startIndex = senderText.Contains("프로필 사진") ?
                                 isFirstMessage ? 2 : 3 :
                                 isFirstMessage ? 1 : 2;
                var targetText = string.Join(Environment.NewLine, splited, startIndex, splited.Length - startIndex);
                convertCategorized = ConvertCustomerText(targetText);
            }

            return true;
        }

        /// <summary>
        /// 손님의 메시지 형식으로 변환한다.
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        /// <remarks>
        /// 메시지의 전체를 '['']' 기호로 감싼다.
        /// </remarks>
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

                // 휴대전화번호인 텍스트는 '-' 기호로 구분
                if (IsCellPhoneNumber(splitText))
                    currentText = StandardizeCellPhoneNumber(splitText);

                builder.Append(currentText);

                if (index < (summarizedLines.Count - 1))
                    builder.Append(" / ");
            }
            builder.Append(" ]");

            return builder.ToString();
        }

        /// <summary>
        /// 직원의 메시지 저장 형식으로 변환한다.
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        /// <remarks>
        /// 메시지를 대괄호 기호([])로 감쌀 필요 없음. + 사전에 지정되어 있는 문자 추가
        /// </remarks>
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

                // 휴대전화번호인 텍스트는 '-' 기호로 구분
                if (IsCellPhoneNumber(splitText))
                    currentText = StandardizeCellPhoneNumber(splitText);

                // 생략해도 되는 직원 메시지는 문자열에서 잘라냄.
                currentText = SliceStaffMessageText(currentText);

                builder.Append(currentText);

                if (index < (splitTexts.Count - 1))
                    builder.Append(" / ");
            }

            // 마지막 문장이 예약 신청 문자로 판단한 경우, 사전에 약속한 텍스트를 추가한다.
            var lastMessage = splitTexts[^1];
            if (ReservationRegex().IsMatch(lastMessage))
            {
                var extraMessage = $" / {configuration.reservationConfirmMessage} // {configuration.staffName}";
                builder.Append(extraMessage);
            }

            return builder.ToString();
        }

        /// <summary>
        /// 변환할 텍스트의 구분 처리
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        private List<string> SplitTextContents(string text)
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
                var splitTexts = textLine.Split(
                    [Environment.NewLine], //[Environment.NewLine, " / "],
                    StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                resultTexts.AddRange(splitTexts);
            }

            return resultTexts;
        }

        /// <summary>
        /// 주어진 텍스트 목록의 각 텍스트들을 축약 처리한다.
        /// </summary>
        /// <param name="texts">줄 단위로 분할한 텍스트들의 목록</param>
        /// <returns></returns>
        /// <remarks>
        /// 텍스트의 축약은 앱 설정 파일에서 설정한 목록에 일치하는 문자열을 잘라내는 방식으로 수행한다.
        /// </remarks>
        private List<string> SummarizeTextContents(ICollection<string> texts)
        {
            List<string> summarizedTexts = new(texts.Count);
            StringBuilder stringBuilder = new();
            foreach (var text in texts)
            {
                stringBuilder.Clear();
                stringBuilder.Append(text);

                if (configuration.formMessages != null)
                {
                    int index = Array.FindIndex(configuration.formMessages, formMessage => ContainsFormMessage(text, formMessage));
                    if (index >= 0)
                        stringBuilder.Replace(configuration.formMessages[index], string.Empty);
                }

                summarizedTexts.Add(stringBuilder.ToString());
            }

            return summarizedTexts;

            // - 고정 양식인 문자열은 모두 잘라냄.
            // - 단, 내용이 없는 경우에는 고정 양식 문자열을 내용에 포함(확인하는 사람이 내용이 없음을 알 수 있도록)
            static bool ContainsFormMessage(string originalText, string formMessage)
            {
                if (string.Compare(originalText, formMessage, StringComparison.InvariantCultureIgnoreCase) == 0)
                    return false;

                return originalText.Contains(formMessage);
            }
        }

        private string SliceStaffMessageText(string text)
        {
            StringBuilder stringBuilder = new(text);
            if (configuration.sliceMessages != null)
            {
                while (true)
                {
                    int index = Array.FindIndex(configuration.sliceMessages, sliceMessage => ContainsSliceMessage(stringBuilder.ToString(), sliceMessage));
                    if (index >= 0)
                        stringBuilder.Replace(configuration.sliceMessages[index], string.Empty);
                    else
                        break;
                }
            }

            return stringBuilder.ToString();

            static bool ContainsSliceMessage(string originalText, string sliceMessage)
            {
                if (string.Compare(originalText, sliceMessage, StringComparison.InvariantCultureIgnoreCase) == 0)
                    return false;

                return originalText.Contains(sliceMessage);
            }
        }
    }
}
