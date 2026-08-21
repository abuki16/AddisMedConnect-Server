using AddisMedConnect.Application.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace AddisMedConnect.Api.Hubs;

public class BedHub : Hub<IBedHubClient>
{
    public async Task JoinHospitalGroup(string hospitalId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"hospital-{hospitalId}");
    }

    public async Task LeaveHospitalGroup(string hospitalId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"hospital-{hospitalId}");
    }
}