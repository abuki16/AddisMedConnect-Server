using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace AddisMedConnect.Api.Hubs
{
    public class EmergencyHub : Hub
    {
        /// <summary>
        /// Allows a client to join a specific hospital's group for targeted broadcasts.
        /// </summary>
        public async Task JoinHospitalGroup(string hospitalId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Hospital_{hospitalId}");
        }

        /// <summary>
        /// Allows a client to leave a hospital's group.
        /// </summary>
        public async Task LeaveHospitalGroup(string hospitalId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Hospital_{hospitalId}");
        }
    }
}