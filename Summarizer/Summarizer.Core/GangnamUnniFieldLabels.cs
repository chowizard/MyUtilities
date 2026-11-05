namespace Summarizer.Core
{
    public class GangnamUnniFieldLabels
    {
        public string[] CustomerName            { get; set; } = ["고객명", "방문자명"];
        public string[] PhoneNumber             { get; set; } = ["연락처", "방문자 전화번호"];
        public string[] ReservationItem         { get; set; } = ["예약 항목"];
        public string[] PreferredDateTime       { get; set; } = ["희망 일시"];
        public string[] ChangePreferredDateTime { get; set; } = ["변경 희망 일시"];
    }
}
