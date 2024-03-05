using Model.Entities;

namespace Model.Services;

public interface IReservationAlgorithm
{
    Reservation? TryReserve(Hospital hospital, Doctor doctor);
}
