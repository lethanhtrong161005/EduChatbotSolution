namespace Domain.Utils;

public static class DateTimeExtensions
{
    public static DateTime RoundToNearestEvenHour(this DateTime dateTime)
    {
        int hour;
        if (dateTime.Minute == 30)
            hour = dateTime.Hour % 2 == 1 ? dateTime.Hour + 1 : dateTime.Hour;
        else
            hour = dateTime.Minute > 30 ? dateTime.Hour + 1 : dateTime.Hour;
        return dateTime.Date.AddHours(hour);
    }
}
