using Model.Entities;

namespace Model.Services;

public class SchedulerService : ISchedulerService
{
    private const int StartWorkingHour = 10;
    private const int EndWorkingHour = 18;

    private static readonly TimeSpan HeartSurgeryDuration = TimeSpan.FromHours(3);
    private static readonly TimeSpan BrainSurgeryDurationWithCt = TimeSpan.FromHours(2);
    private static readonly TimeSpan BrainSurgeryDurationWithoutCt = TimeSpan.FromHours(3);

    public Reservation? Process(ReservationRequest request)
    {
        if (TryProcessQueue())
        {
            var reservation = TryReserve(request);
            if (reservation != null)
                return reservation;
        }

        request.Number = Data.Queue.Count + 1;
        Data.Queue.Enqueue(request);

        return null;
    }

    private bool TryProcessQueue()
    {
        while (!Data.Queue.IsEmpty)
        {
            if (!Data.Queue.TryPeek(out var request))
                return true;

            //handle multi-concurrent requests
            lock (request)
            {
                var reservation = TryReserve(request);
                if (reservation == null)
                {
                    return false;
                }
                Data.Queue.TryDequeue(out _);
            }
        }

        return true;
    }

    private Reservation? TryReserve(ReservationRequest request)
    {
        var hospital = GetHospital(request);
        var doctor = GetDoctor(request);

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
                BrainSurgeryDurationWithCt :
                BrainSurgeryDurationWithoutCt
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
            _ => HeartSurgeryDuration);
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
            if (startedAt.Add(duration).Hour > EndWorkingHour)
            {
                // try schedule for the next day
                startedAt = startedAt.Date.AddDays(1).AddHours(StartWorkingHour);
                if (startedAt > DateTime.UtcNow.AddDays(7))
                    //room is busy this week
                    continue;
            }

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

    private Hospital GetHospital(ReservationRequest request)
    {
        var hospital = Data.Hospitals.SingleOrDefault(x => x.Id == request.HospitalId);
        if (hospital == null)
        {
            //TODO implement validation exception
            throw new Exception($"Hospital with id {request.HospitalId} wasn't found.");
        }

        return hospital;
    }

    private Doctor GetDoctor(ReservationRequest request)
    {
        var doctor = Data.Doctors.SingleOrDefault(x => x.Id == request.DoctorId);
        if (doctor == null)
        {
            //TODO implement validation exception
            throw new Exception($"Doctor with id {request.DoctorId} wasn't found.");
        }

        return doctor;
    }

    private record SuitableRoom(OperationRoom Room, DateTime? AvailableAt);
}
