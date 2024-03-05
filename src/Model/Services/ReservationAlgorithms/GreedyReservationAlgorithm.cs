using Model.Entities;

namespace Model.Services.ReservationAlgorithms;

public class GreedyReservationAlgorithm : IReservationAlgorithm
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
        var suitableRooms = hospital
            .Rooms
            .Where(room => room.WithMRI)
            .OrderByDescending(room => room.WithCT) // with CT first
            .Select(room => new SuitableRoom(
                room,
                room.Reservations
                    .Select(reservation => (DateTime?)reservation.EndedAt)
                    .OrderByDescending(date => date)
                    .FirstOrDefault()
            ))
            .Where(item => item.AvailableAt == null ||
                           item.AvailableAt <= DateTime.UtcNow.AddDays(7));

        return TryReserveSuitableRooms(
            suitableRooms,
            doctor,
            room => room.WithCT ?
                SchedulerSettings.BrainSurgeryDurationWithCt :
                SchedulerSettings.BrainSurgeryDurationWithoutCt
        );
    }

    private Reservation? TryReserveForHeartSurgery(Hospital hospital, Doctor doctor)
    {
        var suitableRooms = hospital
            .Rooms
            .Where(room => room.WithECG)
            .OrderBy(room => room.Weight)
            .Select(room => new SuitableRoom(
                    room,
                    room.Reservations
                        .Select(reservation => (DateTime?)reservation.EndedAt)
                        .OrderByDescending(date => date)
                        .FirstOrDefault()
            ))
            .Where(item => item.AvailableAt == null ||
                           item.AvailableAt <= DateTime.UtcNow.AddDays(7));

        return TryReserveSuitableRooms(
            suitableRooms,
            doctor,
            _ => SchedulerSettings.HeartSurgeryDuration);
    }

    private static Reservation? TryReserveSuitableRooms(
        IEnumerable<SuitableRoom> suitableRooms,
        Doctor doctor,
        Func<OperationRoom, TimeSpan> durationProvider)
    {
        foreach (var item in suitableRooms)
        {
            // assume that timezone is Utc for simplicity
            var startedAt = item.AvailableAt ??
                            DateTime.UtcNow.AddMinutes(-DateTime.UtcNow.Minute)
                                .AddHours(1);
            var duration = durationProvider(item.Room);
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
                continue;

            var reservation = new Reservation
            {
                Id = Guid.NewGuid(),
                StartedAt = startedAt,
                Doctor = doctor,
                Room = item.Room,
                EndedAt = startedAt.Add(duration)
            };
            item.Room.Reservations.Add(reservation);

            return reservation;
        }

        return null;
    }

    private record SuitableRoom(OperationRoom Room, DateTime? AvailableAt);
}
