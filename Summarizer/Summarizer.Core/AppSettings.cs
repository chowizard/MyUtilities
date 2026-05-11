namespace Summarizer.Core
{
    public class AppSettings
    {
        public string StaffName { get; set; } = "아무개";

        public string ReservationConfirmMessage { get; set; } = "채널로 예약문자 전송";

        public bool NormalizeBirthNumber { get; set; } = false;

        public string[] FormMessages { get; set; } =
        [
            "상담받을 분의 성함 / 연락처 - ",
            "생년월일 - ",
            "상담부위 - ",
            "첫수술or 재수술 (재수술일경우 마지막 수술시기 ) - ",
            "상담 희망 날짜와 시간대 - ",
            "상담 원하는 원장님 - ",
            "저희 병원 알게되신 경로 - ",
            "소개자 있으실 경우 소개자 성함과 연락처 뒷번호 - "
        ];

        public ReplaceMessage[] ReplaceStaffMessages { get; set; } = [];

        public string Theme { get; set; } = "System";
    }
}
