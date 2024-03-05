using Model.Entities;

namespace Model.Services;

public interface ISchedulerService
{
    Reservation? Process(ReservationRequest request);
}
