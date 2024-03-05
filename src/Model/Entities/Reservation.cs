namespace Model.Entities;

public class Reservation
{
    public Guid Id { get; init; }
    public DateTime StartedAt { get; init; }
    public OperationRoom Room { get; init; } = null!;
    public Doctor Doctor { get; init; } = null!;
    public DateTime EndedAt { get; init; }
}
