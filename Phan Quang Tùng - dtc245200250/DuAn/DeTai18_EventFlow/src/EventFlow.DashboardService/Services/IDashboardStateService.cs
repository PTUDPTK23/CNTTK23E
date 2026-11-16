using System.Threading.Tasks;
using EventFlow.Shared.Messaging;
using EventFlow.Shared.Models;

namespace EventFlow.DashboardService.Services;

public interface IDashboardStateService
{
    DashboardStatsDto GetCurrentStats();
    Task HandleParticipantRegisteredAsync(ParticipantRegisteredMessage message);
    Task HandleParticipantCancelledAsync(ParticipantCancelledMessage message);
    Task HandleParticipantCheckedInAsync(ParticipantCheckedInMessage message);
    Task InitializeFromEventApiAsync();
}
