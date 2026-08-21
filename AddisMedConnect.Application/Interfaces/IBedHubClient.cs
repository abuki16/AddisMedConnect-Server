namespace AddisMedConnect.Application.Interfaces;

public interface IBedHubClient
{
    Task ReceiveBedStatusUpdate(object data);
    Task ReceiveHospitalBedCountUpdate(object data);
}