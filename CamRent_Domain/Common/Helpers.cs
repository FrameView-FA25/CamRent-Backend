using System;
using System.Runtime.InteropServices;

namespace CamRent_Domain.Common
{
    // Extension methods must be declared in a top-level static class.
    public static class TimeZoneHelpers
    {
        // Windows: "SE Asia Standard Time"; Linux/macOS: "Asia/Ho_Chi_Minh"
        public static TimeZoneInfo GetVietnamTimeZone()
        {
            try
            {
                return RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                    ? TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time")
                    : TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
            }
            catch
            {
                // fallback to fixed offset if system TZ not found
                return TimeZoneInfo.CreateCustomTimeZone("VN", TimeSpan.FromHours(7), "Vietnam", "Vietnam");
            }
        }

        public static DateTime ToVietnamTime(this DateTime utc)
        {
            if (utc.Kind != DateTimeKind.Utc) utc = DateTime.SpecifyKind(utc, DateTimeKind.Utc);
            return TimeZoneInfo.ConvertTimeFromUtc(utc, GetVietnamTimeZone());
        }

        public static DateTimeOffset ToVietnamTime(this DateTimeOffset utcOffset)
            => utcOffset.ToOffset(TimeSpan.FromHours(7));
    }
}
