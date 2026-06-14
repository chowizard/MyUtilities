using System.Text;
using System.Text.RegularExpressions;

namespace Summarizer.Core.RecruitmentConverters
{
    public partial class GangnamUnniMessageConverter : IRecruitmentMessageConverter
    {
        private const string NewReservationFirstLine    = "고객이 예약을 신청했어요.";
        private const string ChangeReservationFirstLine = "고객이 일정 변경을 요청했어요.";
        private const string CancelReservationFirstLine = "고객이 예약을 취소했어요.";

        private const string DoctorPrefix     = "(의사) ";
        private const string EventPrefix      = "(이벤트) 눈성형 중점 ";
        private const string ServiceName      = "강남언니";
        private const string ChangeLabel      = "예약 변경";
        private const string CancelLabel      = "예약 취소";

        [GeneratedRegex(@"^\d+\.\s+(.+)$")]
        private static partial Regex DateLineRegex();


        public bool CanConvert(string firstLine)
            => firstLine == NewReservationFirstLine
            || firstLine == ChangeReservationFirstLine
            || firstLine == CancelReservationFirstLine;

        public string Convert(string text)
        {
            var lines = text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (lines.Length == 0)
                return string.Empty;

            return lines[0] switch
            {
                NewReservationFirstLine    => ConvertNewReservation(lines),
                ChangeReservationFirstLine => ConvertChangeReservation(lines),
                CancelReservationFirstLine => ConvertCancelReservation(lines),
                _                          => string.Empty
            };
        }


        private static string ConvertNewReservation(string[] lines)
        {
            var name        = ExtractFieldValue(lines, "고객명") ?? string.Empty;
            var phone       = ExtractFieldValue(lines, "연락처") ?? string.Empty;
            var (doctor, treatments) = ParseReservationItem(ExtractFieldValue(lines, "예약 항목"));
            var dateLines   = ExtractDateLines(lines, "희망 일시");

            var firstLine = BuildFirstLine(name, phone, BuildReservationItemField(doctor, treatments));
            var secondLine = JoinDateLines(dateLines);

            return string.IsNullOrEmpty(secondLine)
                ? firstLine
                : $"{firstLine}\n{secondLine}";
        }

        private static string ConvertChangeReservation(string[] lines)
        {
            var name      = ExtractFieldValue(lines, "고객명") ?? string.Empty;
            var phone     = ExtractFieldValue(lines, "연락처") ?? string.Empty;
            var dateLines = ExtractDateLines(lines, "변경 희망 일시");

            var firstLine  = BuildFirstLine(name, phone, ChangeLabel);
            var secondLine = JoinDateLines(dateLines);

            return string.IsNullOrEmpty(secondLine)
                ? firstLine
                : $"{firstLine}\n{secondLine}";
        }

        private static string ConvertCancelReservation(string[] lines)
        {
            var name  = ExtractFieldValue(lines, "고객명") ?? string.Empty;
            var phone = ExtractFieldValue(lines, "연락처") ?? string.Empty;

            return BuildFirstLine(name, phone, CancelLabel);
        }


        private static string BuildFirstLine(string name, string phone, string? fourthField)
        {
            var builder = new StringBuilder();
            builder.Append(ServiceName);
            builder.Append(" / ");
            builder.Append(name);
            builder.Append(" / ");
            builder.Append(phone);

            if (!string.IsNullOrEmpty(fourthField))
            {
                builder.Append(" / ");
                builder.Append(fourthField);
            }

            return builder.ToString();
        }

        private static string? BuildReservationItemField(string? doctor, string? treatments)
        {
            if (!string.IsNullOrEmpty(doctor) && !string.IsNullOrEmpty(treatments))
                return $"{doctor} | {treatments}";

            if (!string.IsNullOrEmpty(doctor))
                return doctor;

            if (!string.IsNullOrEmpty(treatments))
                return treatments;

            return null;
        }

        private static string? ExtractFieldValue(string[] lines, string fieldLabel)
        {
            var prefix = $"[{fieldLabel}] : ";
            foreach (var line in lines)
            {
                if (line.StartsWith(prefix, StringComparison.Ordinal))
                    return line[prefix.Length..].Trim();
            }
            return null;
        }

        private static (string? doctor, string? treatments) ParseReservationItem(string? fieldValue)
        {
            if (string.IsNullOrEmpty(fieldValue))
                return (null, null);

            string? doctor     = null;
            string? treatments = null;

            var parts = fieldValue.Split(" | ", StringSplitOptions.TrimEntries);
            foreach (var part in parts)
            {
                if (part.StartsWith(DoctorPrefix, StringComparison.Ordinal))
                    doctor = part[DoctorPrefix.Length..].Trim();
                else if (part.StartsWith(EventPrefix, StringComparison.Ordinal))
                    treatments = part[EventPrefix.Length..].Trim();
            }

            return (doctor, treatments);
        }

        private static List<string> ExtractDateLines(string[] lines, string fieldLabel)
        {
            var prefix = $"[{fieldLabel}] :";
            var result = new List<string>();
            bool collecting = false;

            foreach (var line in lines)
            {
                if (!collecting)
                {
                    if (line.StartsWith(prefix, StringComparison.Ordinal))
                        collecting = true;
                    continue;
                }

                var match = DateLineRegex().Match(line);
                if (!match.Success)
                    break;

                result.Add(match.Groups[1].Value.Trim());
            }

            return result;
        }

        private static string JoinDateLines(List<string> dateLines)
        {
            if (dateLines.Count == 0)
                return string.Empty;

            var builder = new StringBuilder();
            for (int index = 0; index < dateLines.Count; ++index)
            {
                if (index > 0)
                    builder.Append(" / ");

                builder.Append(index + 1);
                builder.Append(". ");
                builder.Append(dateLines[index]);
            }

            return builder.ToString();
        }
    }
}
