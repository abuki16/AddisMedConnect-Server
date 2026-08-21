using AddisMedConnect.Domain.Enums;

namespace AddisMedConnect.Domain.Entities;

public class Hospital
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string SubCity { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string ContactPhone { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Bed> Beds { get; set; } = new List<Bed>();
    public ICollection<User> Staff { get; set; } = new List<User>();
    public ICollection<EmergencyCase> DestinationCases { get; set; } = new List<EmergencyCase>();
}