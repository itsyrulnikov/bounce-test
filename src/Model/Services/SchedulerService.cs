using Model.Entities;
using Model.Services.ReservationAlgorithms;

namespace Model.Services;

public class SchedulerService : ISchedulerService
{
    private readonly IReservationAlgorithm _reservationAlgorithm;

    public SchedulerService(IReservationAlgorithm reservationAlgorithm)
    {
        _reservationAlgorithm = reservationAlgorithm;
    }

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

        return _reservationAlgorithm.TryReserve(hospital, doctor);
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

}
