using AddisMedConnect.Application.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace AddisMedConnect.Infrastructure.Services;

public class BedNotificationService : IBedNotificationService
{
    private readonly IHubContext<Hub<IBedHubClient>, IBedHubClient> _hubContext;

    public BedNotificationService(IHubContext<Hub<IBedHubClient>, IBedHubClient> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task NotifyBedStatusChangedAsync(Guid hospitalId, Guid bedId, string newStatus)
    {
        await _hubContext.Clients.Group($"hospital-{hospitalId}")
            .ReceiveBedStatusUpdate(new { BedId = bedId, Status = newStatus });

        await _hubContext.Clients.All
            .ReceiveHospitalBedCountUpdate(new { HospitalId = hospitalId });
    }
}