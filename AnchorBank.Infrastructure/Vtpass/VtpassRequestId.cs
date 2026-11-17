namespace AnchorBank.Infrastructure.Vtpass;

public static class VtpassRequestId
{
    public static string New()
    {
        var lagos = TimeZoneInfo.FindSystemTimeZoneById("Africa/Lagos");
        var now = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, lagos);
        return $"{now:yyyyMMddHHmm}{Guid.NewGuid():N}"[..24];
    }
}
