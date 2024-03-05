using Model.Entities;

namespace Model.Services.ReservationAlgorithms;

public interface IReservationAlgorithm
{
    Reservation? TryReserve(Hospital hospital, Doctor doctor);
}
