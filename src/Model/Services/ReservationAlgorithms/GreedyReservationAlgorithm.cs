using Model.DTOs;
using Model.Entities;

namespace Model.Services.ReservationAlgorithms;

public class GreedyReservationAlgorithm : IReservationAlgorithm
{
    private readonly INextStartDateTimeCalculator _startDateTimeCalculator;

    public GreedyReservationAlgorithm(INextStartDateTimeCalculator startDateTimeCalculator)
    {
        _startDateTimeCalculator = startDateTimeCalculator;
    }

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

    private Reservation? TryReserveSuitableRooms(
        IEnumerable<SuitableRoom> suitableRooms,
        Doctor doctor,
        Func<OperationRoom, TimeSpan> durationProvider)
    {
        foreach (var suitableRoom in suitableRooms)
        {
            // assume that timezone is Utc for simplicity
            var duration = durationProvider(suitableRoom.Room);
            var startedAt = _startDateTimeCalculator
                .CalculateNextStartedAt(suitableRoom.AvailableAt, duration);

            if (startedAt > DateTime.UtcNow.AddDays(7))
                //room is busy this week
                continue;

            var reservation = new Reservation
            {
                Id = Guid.NewGuid(),
                StartedAt = startedAt,
                Doctor = doctor,
                Room = suitableRoom.Room,
                EndedAt = startedAt.Add(duration)
            };
            suitableRoom.Room.Reservations.Add(reservation);

            return reservation;
        }

        return null;
    }
}
