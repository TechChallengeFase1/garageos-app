namespace GarageOS.Domain.Utils;

public static class BrasiliaTime
{
    private static readonly TimeZoneInfo Fuso =
        TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");

    public static DateTime Agora => DateTime.UtcNow;
}
