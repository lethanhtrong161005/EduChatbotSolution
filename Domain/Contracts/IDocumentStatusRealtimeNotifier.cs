using Domain.Contracts.DTOs;

namespace Domain.Contracts;

public interface IDocumentStatusRealtimeNotifier
{
    Task UpdateStatus(DocumentStatusUpdate documentStatusUpdate);
}
