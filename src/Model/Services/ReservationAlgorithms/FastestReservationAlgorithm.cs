using Model.Entities;

namespace Model.Services.ReservationAlgorithms;

public class FastestReservationAlgorithm : IReservationAlgorithm
{
    private readonly INextStartDateTimeCalculator _startDateTimeCalculator;

    public FastestReservationAlgorithm(INextStartDateTimeCalculator startDateTimeCalculator)
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

    private Reservation? TryReserveSuitableRooms(
        SuitableRoom suitableRoom,
        Doctor doctor,
        Func<OperationRoom, TimeSpan> durationProvider)
    {

        var duration = durationProvider(suitableRoom.Room);
        var startedAt = _startDateTimeCalculator
            .CalculateNextStartedAt(suitableRoom.AvailableAt, duration);
        if (startedAt > DateTime.UtcNow.AddDays(7))
            //room is busy this week
            return null;

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

    private record SuitableRoom(OperationRoom Room, DateTime? AvailableAt);
}
