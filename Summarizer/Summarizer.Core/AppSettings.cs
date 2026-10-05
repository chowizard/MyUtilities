namespace Summarizer.Core
{
    public class AppSettings
    {
        public string StaffName { get; set; } = "아무개";

        public string ReservationConfirmMessage { get; set; } = "채널로 예약문자 전송";

        public bool NormalizeBirthNumber { get; set; } = false;

        public string[] FormMessages { get; set; } =
        [
            @"regex:상담\s*받을\s*분의\s*성함\s*-\s*",
            @"regex:연락처\s*-\s*",
            @"regex:생년\s*월일\s*-\s*",
            @"regex:상담\s*부위\s*-\s*",
            @"regex:첫\s*수술\s*or\s*재\s*수술\s*\(\s*재\s*수술일?\s*경우\s*마지막\s*수술\s*시기\s*\)\s*-\s*",
            @"regex:상담\s*희망\s*날짜\s*와\s*시간대\s*-\s*",
            @"regex:상담\s*원하는\s*원장님\s*-\s*",
            @"regex:저희\s*병원\s*알게\s*되신\s*경로\s*-\s*",
            @"regex:소개자\s*있으실\s*경우\s*,?\s*소개자\s*성함\s*과\s*연락처\s*뒷\s*번호\s*-\s*"
        ];

        public ReplaceMessage[] ReplaceStaffMessages { get; set; } = [];

        public string Theme { get; set; } = "System";
    }
}
