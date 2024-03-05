using Model.Entities;

namespace BounceAPI.Models;

public class ReservationResponse
{
    private const string ReservedStatus = "Reserved";
    private const string QueuedStatus = "Queued";

    public ReservationResponse(Reservation? reservation, ReservationRequest request)
    {
        if (reservation == null)
        {
            Status = QueuedStatus;
            Request = new RequestsDetails
            {
                Number = request.Number
            };
            return;
        }

        Status = ReservedStatus;
        Reservation = new ReservationDetails
        {
            Id = reservation.Id,
            StartedAt = reservation.StartedAt,
            EndedAt = reservation.EndedAt,
            RoomId = reservation.Room.Id
        };
    }

    public string Status { get; set; }
    public ReservationDetails? Reservation { get; set; }
    public RequestsDetails? Request { get; set; }
}
