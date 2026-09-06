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

        /// <summary>
        /// Allows an ambulance driver client to join their vehicle's targeted broadcast channel.
        /// </summary>
        public async Task JoinAmbulanceGroup(string ambulanceId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Ambulance_{ambulanceId}");
        }

        /// <summary>
        /// Allows an ambulance driver client to leave their vehicle's broadcast channel.
        /// </summary>
        public async Task LeaveAmbulanceGroup(string ambulanceId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Ambulance_{ambulanceId}");
        }
    }
}