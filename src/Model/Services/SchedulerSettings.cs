namespace Model.Services;

public static class SchedulerSettings
{
    public const int StartWorkingHour = 10;
    public const int EndWorkingHour = 18;

    public static readonly TimeSpan HeartSurgeryDuration = TimeSpan.FromHours(3);
    public static readonly TimeSpan BrainSurgeryDurationWithCt = TimeSpan.FromHours(2);
    public static readonly TimeSpan BrainSurgeryDurationWithoutCt = TimeSpan.FromHours(3);
}
