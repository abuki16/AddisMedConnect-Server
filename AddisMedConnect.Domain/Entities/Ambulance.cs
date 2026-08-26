public class Ambulance
{
    public Guid Id { get; set; }
    public string PlateNumber { get; set; } = string.Empty; // Mandatory
    public string? DriverName { get; set; }                 // Optional
    public string? PhoneNumber { get; set; }                // Optional
    public bool IsAvailable { get; set; } = true;
    
    // Location tracking for proximity dispatch
    public double? CurrentLatitude { get; set; }
    public double? CurrentLongitude { get; set; }
}