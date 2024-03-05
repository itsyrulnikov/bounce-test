namespace Model.Entities;

public class Hospital
{
    public int Id { get; set; }
    public IList<OperationRoom> Rooms { get; set; } = new List<OperationRoom>();
}
