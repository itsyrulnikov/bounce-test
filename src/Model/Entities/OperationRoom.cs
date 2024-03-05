namespace Model.Entities;

public class OperationRoom
{
    public int Id { get; init; }
    public IList<Reservation> Reservations { get; init; } = new List<Reservation>();
    public bool WithMRI { get; set; }
    public bool WithCT { get; set; }
    public bool WithECG { get; set; }

    public int Weight => (WithMRI ? 1 : 0) + (WithCT ? 1 : 0) + (WithECG ? 1 : 0);
}
