namespace AddisMedConnect.Application.Interfaces;

public interface IBedNotificationService
{
    Task NotifyBedStatusChangedAsync(Guid hospitalId, Guid bedId, string newStatus);
}