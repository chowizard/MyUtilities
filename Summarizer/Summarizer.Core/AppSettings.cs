namespace Summarizer.Core
{
    public class AppSettings
    {
        public string StaffName { get; init; } = "아무개";

        public string ReservationConfirmMessage { get; init; } = "채널로 예약문자 전송";

        public string[] FormMessages { get; init; } =
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

        public string[] SliceStaffMessages { get; init; } =
        [
            "안녕하세요~",
            "안녕하세요^^",
            "안녕하세요~^^",
            "^^"
        ];

        public string Theme { get; set; } = "System";
    }
}
