namespace BounceAPI.Models;

public class ReservationDetails
{
    public Guid Id { get; set; }

    public int RoomId { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
}
