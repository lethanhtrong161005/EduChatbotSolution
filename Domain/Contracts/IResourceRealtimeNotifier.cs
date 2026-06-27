using Domain.Contracts.DTOs;

namespace Domain.Contracts;

public interface IResourceRealtimeNotifier
{
    Task PushUpdateAsync(ResourceUpdate update, string? callerId = null);
}
