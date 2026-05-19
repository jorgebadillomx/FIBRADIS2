using System.Runtime.InteropServices;
using Application.Market;

namespace Infrastructure.Jobs.Market;

public class BmvSchedule : IBmvSchedule
{
    private static readonly TimeOnly _open = new(8, 15);
    private static readonly TimeOnly _close = new(15, 15);
    private static readonly TimeZoneInfo _mexicoTz = TimeZoneInfo.FindSystemTimeZoneById(
        RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? "Central Standard Time"
            : "America/Mexico_City");

    public bool IsTradingHours(DateTimeOffset utcNow)
    {
        var local = TimeZoneInfo.ConvertTimeFromUtc(utcNow.UtcDateTime, _mexicoTz);
        var isWeekday = local.DayOfWeek is >= DayOfWeek.Monday and <= DayOfWeek.Friday;
        var localTime = TimeOnly.FromDateTime(local);
        return isWeekday && localTime >= _open && localTime < _close;
    }
}
