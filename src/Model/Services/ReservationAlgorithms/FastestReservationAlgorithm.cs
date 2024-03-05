using Model.Entities;

namespace Model.Services.ReservationAlgorithms;

public class FastestReservationAlgorithm : IReservationAlgorithm
{
    public Reservation? TryReserve(Hospital hospital, Doctor doctor)
    {
        //handle multi-concurrent requests
        lock (hospital)
        {
            return doctor.Type == DoctorType.HeartSurgeon
                ? TryReserveForHeartSurgery(hospital, doctor)
                : TryReserveForBrainSurgery(hospital, doctor);
        }
    }

    private Reservation? TryReserveForBrainSurgery(Hospital hospital, Doctor doctor)
    {
        var firstAvailableRoom = hospital
            .Rooms
            .Where(room => room.WithMRI)
            .Select(room => new SuitableRoom(
                room,
                room.Reservations
                    .Select(reservation => (DateTime?)reservation.EndedAt)
                    .OrderByDescending(date => date)
                    .FirstOrDefault()
            ))
            .MinBy(item => item.AvailableAt);

        if (firstAvailableRoom == null)
            return null;

        return TryReserveSuitableRooms(
            firstAvailableRoom,
            doctor,
            room => room.WithCT ?
                SchedulerSettings.BrainSurgeryDurationWithCt :
                SchedulerSettings.BrainSurgeryDurationWithoutCt
        );
    }

    private Reservation? TryReserveForHeartSurgery(Hospital hospital, Doctor doctor)
    {
        var firstAvailableRoom = hospital
            .Rooms
            .Where(room => room.WithECG)
            .Select(room => new SuitableRoom(
                room,
                room.Reservations
                    .Select(reservation => (DateTime?)reservation.EndedAt)
                    .OrderByDescending(date => date)
                    .FirstOrDefault()
            )).MinBy(item => item.AvailableAt ?? default(DateTime));

        if (firstAvailableRoom == null)
            return null;

        return TryReserveSuitableRooms(
            firstAvailableRoom,
            doctor,
            _ => SchedulerSettings.HeartSurgeryDuration);
    }

    private static Reservation? TryReserveSuitableRooms(
        SuitableRoom suitableRooms,
        Doctor doctor,
        Func<OperationRoom, TimeSpan> durationProvider)
    {
        // assume that timezone is Utc for simplicity
        var startedAt = suitableRooms.AvailableAt ??
                        DateTime.UtcNow.AddMinutes(-DateTime.UtcNow.Minute)
                            .AddHours(1);
        var duration = durationProvider(suitableRooms.Room);
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

        if (startedAt > DateTime.UtcNow.AddDays(7))
            //room is busy this week
            return null;

        var reservation = new Reservation
        {
            Id = Guid.NewGuid(),
            StartedAt = startedAt,
            Doctor = doctor,
            Room = suitableRooms.Room,
            EndedAt = startedAt.Add(duration)
        };
        suitableRooms.Room.Reservations.Add(reservation);

        return reservation;
    }

    private record SuitableRoom(OperationRoom Room, DateTime? AvailableAt);
}
