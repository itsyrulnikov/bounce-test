namespace Model.Entities;

public class ReservationRequest
{
    public int HospitalId { get; set; }
    public int DoctorId { get; set; }

    public int Number { get; set; }
}
