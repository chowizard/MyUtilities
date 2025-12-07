namespace Summarizer
{
    public class Configuration
    {
        /// <summary>
        /// 사용자 이름
        /// </summary>
        public string staffName = string.Empty;

        /// <summary>
        /// 예약 확인 메시지
        /// </summary>
        public string reservationConfirmMessage = string.Empty;

        /// <summary>
        /// 제출 서식 문자열들
        /// </summary>
        public string[] formMessages = null;

        /// <summary>
        /// 생략 대상 문자열들
        /// </summary>
        public string[] sliceMessages = null;
    }
}
