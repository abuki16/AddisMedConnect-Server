namespace AddisMedConnect.Domain.Entities;

public class Ambulance
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string VehiclePlateNumber { get; set; } = string.Empty;
    public bool IsAvailable { get; set; } = true;
    public double? CurrentLatitude { get; set; }
    public double? CurrentLongitude { get; set; }
    
    public Guid? AssignedDriverId { get; set; }
    public User? AssignedDriver { get; set; }
}