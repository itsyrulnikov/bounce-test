using Model.DTOs;
using Model.Entities;

namespace Model.Services;

public class NextStartDateTimeCalculator : INextStartDateTimeCalculator
{
    public DateTime CalculateNextStartedAt(DateTime? availableAt,
        TimeSpan duration)
    {
        // assume that timezone is Utc for simplicity
        var startedAt = availableAt ??
                        DateTime.UtcNow.AddMinutes(-DateTime.UtcNow.Minute)
                            .AddHours(1);
        if (startedAt.Hour < SchedulerSettings.StartWorkingHour)
        {
            startedAt = startedAt
                .AddHours(-startedAt.Hour)
                .AddHours(SchedulerSettings.StartWorkingHour);
        }

        var endedAt = startedAt.Add(duration);
        if (endedAt.Hour is < SchedulerSettings.StartWorkingHour or > SchedulerSettings.EndWorkingHour)
        {
            // try schedule for the next day
            startedAt = startedAt.Date.AddDays(1).AddHours(SchedulerSettings.StartWorkingHour);
        }

        return startedAt;
    }
}
